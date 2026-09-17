using Folguinha.Domain.Regras;
using static Folguinha.Domain.Texto;

namespace Folguinha.Domain.Geracao;

public sealed record EntradaGeracao
{
    public required Empresa Empresa { get; init; }
    public required DateOnly Inicio { get; init; }
    public required DateOnly Fim { get; init; }
    public required IReadOnlyList<Funcionario> Funcionarios { get; init; }
    public required IReadOnlyList<Turno> Turnos { get; init; }
    public IReadOnlyList<Demanda> Demandas { get; init; } = [];
    public IReadOnlyList<Funcao> Funcoes { get; init; } = [];
    public IReadOnlyList<Feriado> Feriados { get; init; } = [];
    public IReadOnlyList<Ocorrencia> Ocorrencias { get; init; } = [];
    /// Vazio = aberto todos os dias.
    public IReadOnlyList<PeriodoFuncionamento> Funcionamento { get; init; } = [];
    public IReadOnlyList<DateOnly> DatasFechadas { get; init; } = [];
    /// Escalas anteriores (base para descanso, domingos e rodízio de feriados).
    public IReadOnlyList<Alocacao> Historico { get; init; } = [];
    /// Alocações que o gerador não pode mudar (ajustes manuais, dias já publicados).
    public IReadOnlyList<Alocacao> Travadas { get; init; } = [];
    public IReadOnlyList<Regra> Regras { get; init; } = CatalogoRegras.Padrao();
    public bool RodizioFeriados { get; init; } = true;
}

public sealed record ResultadoGeracao(IReadOnlyList<Alocacao> Alocacoes, IReadOnlyList<Violacao> Violacoes)
{
    public bool PodePublicar => Violacoes.All(v => v.Severidade != Severidade.Bloqueio);
}

/// Heurística determinística (ESPEC §8): primeiro as folgas de cada semana, depois os turnos de cada dia.
public static class GeradorEscala
{
    public const string RegraRodizioFeriado = "ger.rodizio-feriado";
    public const string RegraSemTurno = "ger.sem-turno";

    public static ResultadoGeracao Gerar(EntradaGeracao entrada) => new Execucao(entrada).Rodar();
}

sealed class Execucao
{
    readonly EntradaGeracao e;
    readonly Dictionary<Guid, List<Alocacao>> doFuncionario;
    readonly List<Alocacao> geradas = [];
    readonly List<Violacao> avisos = [];
    readonly HashSet<(Guid, DateOnly)> folgas = [];
    readonly Regra[] obrigatorias;
    readonly Feriado[] feriados;
    readonly int semanasDomingo;

    public Execucao(EntradaGeracao entrada)
    {
        e = entrada;
        doFuncionario = e.Funcionarios.ToDictionary(f => f.Id,
            f => e.Historico.Where(a => a.FuncionarioId == f.Id && a.Data < e.Inicio).OrderBy(a => a.Data).ToList());
        obrigatorias = e.Regras.Where(r => r.Definicao.Nivel == NivelRegra.Obrigatoria && r is not CoberturaMinima).ToArray();
        feriados = e.Feriados.Where(f => f.Considerado).OrderBy(f => f.Data).ToArray();
        var domingo = e.Regras.OfType<DomingoDeFolga>().FirstOrDefault() ?? new DomingoDeFolga();
        semanasDomingo = e.Empresa.Ramo == RamoAtividade.Comercio ? domingo.SemanasComercio : domingo.SemanasOutros;
    }

    public ResultadoGeracao Rodar()
    {
        for (var segunda = InicioDaSemana(e.Inicio); segunda <= e.Fim; segunda = segunda.AddDays(7))
        {
            var dias = Enumerable.Range(0, 7).Select(i => segunda.AddDays(i)).Where(NoPeriodo).ToList();
            PlanejarFolgas(segunda, dias);
            foreach (var dia in dias) Escalar(dia);
        }

        var ctx = new ContextoValidacao(e.Empresa, e.Inicio, e.Fim, e.Funcionarios,
            [.. e.Historico.Where(a => a.Data < e.Inicio), .. geradas],
            e.Feriados, e.Ocorrencias, e.Demandas, e.Turnos, e.Funcoes)
        {
            DiasFechados = Dias().Where(d => !Aberto(d)).ToHashSet(),
        };
        return new ResultadoGeracao(geradas, [.. Validador.Validar(ctx, e.Regras), .. avisos]);
    }

    // ---------- Folgas da semana ----------

    void PlanejarFolgas(DateOnly segunda, List<DateOnly> dias)
    {
        var pessoas = e.Funcionarios.Where(f => f.Regime != Regime.DozePorTrintaESeis).ToList();
        var fixas = pessoas.ToDictionary(f => f.Id, f => dias.Where(d => NaoTrabalhaDeQualquerJeito(f, d)).ToHashSet());
        var folgasNoDia = dias.ToDictionary(d => d, d => pessoas.Count(f => fixas[f.Id].Contains(d)));
        var domingo = segunda.AddDays(6);
        var feriado = feriados.FirstOrDefault(x => dias.Contains(x.Data));

        var ordem = pessoas
            .OrderByDescending(f => PrecisaFolgarDomingo(f, domingo))
            // quem tem prazo de folga dentro da semana escolhe antes, para não sobrar só um dia lotado
            .ThenBy(f => UltimaFolga(f, dias[0]).AddDays(7) is var prazo && prazo <= dias[^1] ? prazo : DateOnly.MaxValue)
            .ThenByDescending(f => feriado is not null && TrabalhouUltimoFeriado(f, feriado.Data))
            .ThenByDescending(f => DomingosRecentes(f, domingo))
            .ThenBy(f => UltimaFolga(f, dias[0]))
            .ThenBy(f => f.Nome, StringComparer.Ordinal).ThenBy(f => f.Id);

        var posicao = 0;
        foreach (var f in ordem)
        {
            var minhas = fixas[f.Id];
            var livres = dias.Where(d => !minhas.Contains(d)).ToList();
            var falta = dias.Count - AlvoDeDias(f, segunda, dias) - minhas.Count;

            if (PrecisaFolgarDomingo(f, domingo) && livres.Remove(domingo))
            {
                Folgar(f, domingo);
                falta--;
            }

            for (; falta > 0 && livres.Count > 0; falta--)
            {
                // nunca mais de 6 dias seguidos: a próxima folga não pode passar de última folga + 7
                var limite = PrazoDaProximaFolga(UltimaFolga(f, dias[0]), minhas);
                var candidatos = livres.Any(d => d <= limite) ? livres.Where(d => d <= limite) : livres;
                var escolhido = candidatos
                    .OrderByDescending(d => Pontuar(f, d, feriado))
                    .ThenBy(d => (d.DayNumber + posicao) % 7)
                    .First();
                livres.Remove(escolhido);
                Folgar(f, escolhido);
            }

            // a folga de domingo obrigatória pode ter deixado um trecho de 7+ dias: folga extra (a lei vem antes da carga)
            while (PrazoDaProximaFolga(UltimaFolga(f, dias[0]), minhas) is var prazo
                   && dias.Max() >= prazo
                   && livres.Where(d => d <= prazo).OrderByDescending(d => Pontuar(f, d, feriado)).FirstOrDefault() is { } extra
                   && extra != default)
            {
                livres.Remove(extra);
                Folgar(f, extra);
            }
            posicao++;

            void Folgar(Funcionario f, DateOnly d)
            {
                minhas.Add(d);
                folgasNoDia[d]++;
                folgas.Add((f.Id, d));
            }
        }

        double Pontuar(Funcionario f, DateOnly d, Feriado? feriado)
        {
            var sobra = pessoas.Count - PessoasNecessarias(d) - folgasNoDia[d];
            var pontos = (double)sobra;
            // quem trabalhou domingos recentes folga neste, para não ser obrigado na semana seguinte
            if (d.DayOfWeek == DayOfWeek.Sunday && sobra > 0 && DomingosRecentes(f, d) >= Math.Max(1, semanasDomingo - 2)) pontos += 20;
            foreach (var fn in f.Funcoes ?? [])
            {
                var colegas = pessoas.Count(p => p.TemFuncao(fn) && !folgas.Contains((p.Id, d)) && !fixas[p.Id].Contains(d));
                if (colegas - 1 < PessoasNecessarias(d, fn)) pontos -= 50;
            }
            if (e.RodizioFeriados && feriado?.Data == d && TrabalhouUltimoFeriado(f, d) && sobra > 0) pontos += 100;
            return pontos;
        }
    }

    /// Data máxima da próxima folga para não passar de 6 dias seguidos, considerando folgas já marcadas.
    static DateOnly PrazoDaProximaFolga(DateOnly ultimaFolga, IEnumerable<DateOnly> marcadas)
    {
        var atual = ultimaFolga;
        foreach (var m in marcadas.Where(m => m > ultimaFolga).Order())
        {
            if (m > atual.AddDays(7)) break;
            atual = m;
        }
        return atual.AddDays(7);
    }

    bool NaoTrabalhaDeQualquerJeito(Funcionario f, DateOnly d) =>
        !Aberto(d)
        || Travada(f, d) is { Trabalha: false }
        || Travada(f, d) is null && Afastado(f, d)
        || (f.Disponibilidades ?? []).Any(x => x.ValeEm(d) && x.Tipo != TipoRestricao.Preferencia && x.Inicio is null);

    int AlvoDeDias(Funcionario f, DateOnly segunda, List<DateOnly> dias)
    {
        var cota = f.Regime switch
        {
            Regime.SeisPorUm => 6,
            Regime.CincoPorDois => 5,
            _ => Math.Clamp((int)Math.Ceiling(f.CargaSemanal / HorasDoTurnoPrincipal(f)), 1, 6),
        };
        var antes = Enumerable.Range(0, 7).Select(i => segunda.AddDays(i))
            .Where(d => d < e.Inicio && doFuncionario[f.Id].Any(a => a.Data == d)).ToList();
        var considerados = dias.Count + antes.Count;
        var alvo = considerados >= 7 ? cota : (int)Math.Round(cota * considerados / 7.0, MidpointRounding.AwayFromZero);
        var jaTrabalhados = antes.Count(d => doFuncionario[f.Id].Any(a => a.Data == d && a.Trabalha));
        return Math.Clamp(alvo - jaTrabalhados, 0, dias.Count);
    }

    TimeSpan HorasDoTurnoPrincipal(Funcionario f)
    {
        var t = e.Turnos.FirstOrDefault(t => t.Id == f.TurnoPrincipal) ?? e.Turnos.FirstOrDefault(t => f.PodeTurno(t.Id));
        return t is null ? TimeSpan.FromHours(8) : Alocacao.Trabalho(f.Id, e.Inicio, t.Inicio, t.Fim, t.Intervalo).HorasComputadas;
    }

    bool PrecisaFolgarDomingo(Funcionario f, DateOnly domingo) =>
        NoPeriodo(domingo) && Enumerable.Range(1, semanasDomingo - 1).All(i => Trabalhou(f, domingo.AddDays(-7 * i)));

    int DomingosRecentes(Funcionario f, DateOnly domingo) =>
        Enumerable.Range(1, semanasDomingo - 1).Count(i => Trabalhou(f, domingo.AddDays(-7 * i)));

    DateOnly UltimaFolga(Funcionario f, DateOnly primeiroDia)
    {
        var d = primeiroDia.AddDays(-1);
        while (Trabalhou(f, d)) d = d.AddDays(-1);
        return d;
    }

    // ---------- Turnos do dia ----------

    void Escalar(DateOnly dia)
    {
        var hoje = new Dictionary<Guid, Alocacao>();
        var trabalhadores = new List<Funcionario>();

        foreach (var (f, i) in e.Funcionarios.Select((f, i) => (f, i)))
        {
            if (Travada(f, dia) is { } t) hoje[f.Id] = t;
            else if (Afastado(f, dia)) hoje[f.Id] = new Alocacao(f.Id, dia, TipoAlocacao.Ocorrencia);
            else if (!Aberto(dia) || folgas.Contains((f.Id, dia)) || !DeveTrabalhar12x36(f, dia, i))
                hoje[f.Id] = Alocacao.Folga(f.Id, dia);
            else trabalhadores.Add(f);
        }

        var vagas = Aberto(dia)
            ? Demanda.Aplicaveis(e.Demandas, dia).OrderBy(d => d.FuncaoId is null).ThenBy(d => TurnoIndex(d.TurnoId)).ToList()
            : [];
        foreach (var alvo in new Func<Demanda, int>[] { d => d.Minimo, d => d.Ideal })
        foreach (var vaga in vagas)
            Preencher(dia, vaga, alvo(vaga), hoje, trabalhadores);

        foreach (var f in trabalhadores.ToList())
        {
            var turnos = e.Turnos.Where(t => f.PodeTurno(t.Id))
                .OrderByDescending(t => t.Id == f.TurnoPrincipal)
                .ThenByDescending(t => vagas.Where(v => v.TurnoId == t.Id && v.FuncaoId is null).Sum(v => v.Ideal) - Contar(t.Id, null, hoje))
                .ThenBy(t => TurnoIndex(t.Id));
            var aloc = turnos.Select(t => Tentar(f, dia, t, null)).FirstOrDefault(a => a is not null);
            if (aloc is not null) Atribuir(f, aloc, hoje, trabalhadores);
            else
            {
                trabalhadores.Remove(f);
                hoje[f.Id] = Alocacao.Folga(f.Id, dia);
                avisos.Add(new Violacao(GeradorEscala.RegraSemTurno, Severidade.Atencao,
                    $"Nenhum turno de {Dia(dia)} serve para {f.Nome} sem quebrar regras; ficou de folga.",
                    $"Revise os turnos permitidos e a disponibilidade de {f.Nome}.", f.Id, dia));
            }
        }

        foreach (var a in hoje.Values)
        {
            geradas.Add(a);
            if (!doFuncionario[a.FuncionarioId].Contains(a)) doFuncionario[a.FuncionarioId].Add(a);
        }

        if (e.RodizioFeriados && feriados.FirstOrDefault(x => x.Data == dia) is not null)
            foreach (var f in e.Funcionarios.Where(f => f.Regime != Regime.DozePorTrintaESeis && hoje[f.Id].Trabalha))
                if (UltimoFeriado(dia) is { } ultimo && Trabalhou(f, ultimo.Data))
                    avisos.Add(new Violacao(GeradorEscala.RegraRodizioFeriado, Severidade.Informativo,
                        $"{f.Nome} trabalhou no último feriado ({Dia(ultimo.Data)}) e não pôde folgar em {Dia(dia)}.",
                        "Priorize a folga de " + f.Nome + " no próximo feriado.", f.Id, dia));
    }

    void Preencher(DateOnly dia, Demanda vaga, int alvo, Dictionary<Guid, Alocacao> hoje, List<Funcionario> trabalhadores)
    {
        var turno = e.Turnos.FirstOrDefault(t => t.Id == vaga.TurnoId);
        if (turno is null) return;

        while (Contar(vaga.TurnoId, vaga.FuncaoId, hoje) < alvo)
        {
            var escolhido = trabalhadores
                .Select(f => (f, aloc: Tentar(f, dia, turno, vaga.FuncaoId)))
                .Where(x => x.aloc is not null)
                .OrderBy(x => Preferido(x.f, dia, turno) ? 0 : 1)
                .ThenBy(x => vaga.FuncaoId is null && x.f.Funcoes is { Count: > 0 } ? 1 : 0) // guarda especialistas
                .ThenBy(x => x.f.TurnoPrincipal is null || x.f.TurnoPrincipal == turno.Id ? 0 : 1)
                .ThenBy(x => TurnoDeOntem(x.f, dia) is { } ontem && ontem != turno.Id ? 1 : 0)
                .ThenBy(x => HorasNoPeriodo(x.f))
                .ThenBy(x => x.f.Nome, StringComparer.Ordinal).ThenBy(x => x.f.Id)
                .FirstOrDefault();
            if (escolhido.aloc is null) return;
            Atribuir(escolhido.f, escolhido.aloc, hoje, trabalhadores);
        }
    }

    void Atribuir(Funcionario f, Alocacao a, Dictionary<Guid, Alocacao> hoje, List<Funcionario> trabalhadores)
    {
        hoje[f.Id] = a;
        trabalhadores.Remove(f);
    }

    /// Alocação válida para o funcionário nesse turno, ou null se quebraria alguma regra obrigatória.
    Alocacao? Tentar(Funcionario f, DateOnly dia, Turno turno, Guid? funcaoId)
    {
        if (!f.PodeTurno(turno.Id) || funcaoId is { } fn && !f.TemFuncao(fn)) return null;
        if ((f.Disponibilidades ?? []).Any(x => x.ValeEm(dia) && x.Tipo != TipoRestricao.Preferencia && !x.Comporta(turno)))
            return null;

        var aloc = Alocacao.Trabalho(f.Id, dia, turno.Inicio, turno.Fim, turno.Intervalo, turno.Id,
            funcaoId ?? f.Funcoes?.FirstOrDefault());
        var anteriores = doFuncionario[f.Id];

        if (f.Regime == Regime.DozePorTrintaESeis
            && anteriores.LastOrDefault(a => a.Trabalha) is { } ultimo
            && aloc.InicioEm - ultimo.FimEm < TimeSpan.FromHours(36))
            return null;

        var fimDaSemana = dia.AddDays((7 - (int)dia.DayOfWeek) % 7);
        var recentes = anteriores.Where(a => a.Data >= dia.AddDays(-7 * 8));
        var ctx = new ContextoValidacao(e.Empresa, dia, fimDaSemana, [f], [.. recentes, aloc],
            e.Feriados, e.Ocorrencias, [], e.Turnos, e.Funcoes);
        return obrigatorias.Any(r => r.Validar(ctx).Any()) ? null : aloc;
    }

    bool DeveTrabalhar12x36(Funcionario f, DateOnly dia, int indice)
    {
        if (f.Regime != Regime.DozePorTrintaESeis) return true;
        var ultimo = doFuncionario[f.Id].LastOrDefault(a => a.Trabalha);
        // sem histórico: metade começa no primeiro dia, metade no segundo
        return ultimo is null ? (dia.DayNumber - e.Inicio.DayNumber) % 2 == indice % 2 : ultimo.Data <= dia.AddDays(-2);
    }

    bool Preferido(Funcionario f, DateOnly dia, Turno t) =>
        (f.Disponibilidades ?? []).Where(x => x.ValeEm(dia) && x.Tipo == TipoRestricao.Preferencia).All(x => x.Comporta(t));

    int Contar(Guid turnoId, Guid? funcaoId, Dictionary<Guid, Alocacao> hoje) =>
        hoje.Values.Count(a => a.Trabalha && a.TurnoId == turnoId && (funcaoId is null || a.FuncaoId == funcaoId));

    /// Pessoas necessárias no dia (ou numa função): por turno, o maior entre a demanda geral e a soma das funções.
    int PessoasNecessarias(DateOnly dia, Guid? funcaoId = null)
    {
        if (!Aberto(dia)) return 0;
        var doDia = Demanda.Aplicaveis(e.Demandas, dia).ToList();
        if (funcaoId is not null) return doDia.Where(d => d.FuncaoId == funcaoId).Sum(d => d.Minimo);
        return doDia.GroupBy(d => d.TurnoId).Sum(g =>
            Math.Max(g.Where(d => d.FuncaoId is null).Sum(d => d.Minimo), g.Where(d => d.FuncaoId is not null).Sum(d => d.Minimo)));
    }

    // ---------- Consultas ----------

    Alocacao? Travada(Funcionario f, DateOnly dia) => e.Travadas.FirstOrDefault(a => a.FuncionarioId == f.Id && a.Data == dia);

    bool Afastado(Funcionario f, DateOnly dia) => e.Ocorrencias.Any(o => o.FuncionarioId == f.Id && o.Afasta && o.Cobre(dia));

    bool Trabalhou(Funcionario f, DateOnly dia) => doFuncionario[f.Id].Any(a => a.Data == dia && a.Trabalha);

    Guid? TurnoDeOntem(Funcionario f, DateOnly dia) =>
        doFuncionario[f.Id].LastOrDefault(a => a.Data == dia.AddDays(-1) && a.Trabalha)?.TurnoId;

    double HorasNoPeriodo(Funcionario f) =>
        doFuncionario[f.Id].Where(a => a.Trabalha && a.Data >= e.Inicio).Sum(a => a.HorasComputadas.TotalHours);

    Feriado? UltimoFeriado(DateOnly antesDe) => feriados.LastOrDefault(x => x.Data < antesDe);

    bool TrabalhouUltimoFeriado(Funcionario f, DateOnly feriado) =>
        UltimoFeriado(feriado) is { } ultimo && Trabalhou(f, ultimo.Data);

    bool Aberto(DateOnly dia) =>
        !e.DatasFechadas.Contains(dia) && (e.Funcionamento.Count == 0 || e.Funcionamento.Any(p => p.Dia == dia.DayOfWeek));

    bool NoPeriodo(DateOnly dia) => dia >= e.Inicio && dia <= e.Fim;

    IEnumerable<DateOnly> Dias()
    {
        for (var d = e.Inicio; d <= e.Fim; d = d.AddDays(1)) yield return d;
    }

    int TurnoIndex(Guid turnoId) => e.Turnos.ToList().FindIndex(t => t.Id == turnoId);

    static DateOnly InicioDaSemana(DateOnly d) => d.AddDays(-(((int)d.DayOfWeek + 6) % 7));
}
