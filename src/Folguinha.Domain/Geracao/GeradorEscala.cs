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
    /// Quem trabalhou num feriado folga um dia a mais na semana seguinte (Lei 605/49, art. 9º).
    /// Desligado por padrão: numa equipe justa, todo mundo folgando a mais deixa a semana seguinte
    /// sem cobertura — a alternativa legal é pagar o feriado em dobro.
    public bool CompensarFeriado { get; init; }
    /// Turno principal vira só preferência: cada pessoa passa por todos os turnos ao longo do mês.
    public bool RodizioTurnos { get; init; }
    /// Sábado e domingo só se folgam juntos: o domingo do rodízio leva o sábado (que vira a folga
    /// da semana, e o domingo a mais); fora disso a folga cai de segunda a sexta.
    public bool FolgaCasada { get; init; }
    /// Domingo de folga a cada N semanas (2 = fim de semana sim, outro não). Nulo = o mínimo da lei;
    /// nunca passa dele.
    public int? DomingoACada { get; init; }
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
    readonly List<Alocacao> historico;
    readonly List<Alocacao> geradas = [];
    readonly List<Violacao> avisos = [];
    readonly HashSet<(Guid, DateOnly)> folgas = [];
    readonly Regra[] obrigatorias;
    readonly Feriado[] feriados;
    readonly int semanasDomingo;
    readonly int semanasLei;

    public Execucao(EntradaGeracao entrada)
    {
        e = entrada;
        feriados = e.Feriados.Where(f => f.Considerado).OrderBy(f => f.Data).ToArray();
        doFuncionario = e.Funcionarios.ToDictionary(f => f.Id, f =>
        {
            var reais = e.Historico.Where(a => a.FuncionarioId == f.Id && a.Data < e.Inicio).ToList();
            return reais.Concat(reais.Count == 0 ? f.HistoricoPresumido(e.Inicio, e.Turnos, feriados) : []).OrderBy(a => a.Data).ToList();
        });
        historico = doFuncionario.Values.SelectMany(x => x).ToList();
        obrigatorias = e.Regras.Where(r => r.Definicao.Nivel == NivelRegra.Obrigatoria && r is not CoberturaMinima).ToArray();
        var domingo = e.Regras.OfType<DomingoDeFolga>().FirstOrDefault() ?? new DomingoDeFolga();
        semanasLei = e.Empresa.Ramo == RamoAtividade.Comercio ? domingo.SemanasComercio : domingo.SemanasOutros;
        semanasDomingo = Math.Clamp(e.DomingoACada ?? semanasLei, 2, semanasLei);
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
            [.. historico, .. geradas],
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
        var domingueiros = QuemFolgaODomingo(pessoas, domingo, fixas);

        var ordem = pessoas
            .OrderByDescending(f => domingueiros.Contains(f.Id))
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

            if (domingueiros.Contains(f.Id) && livres.Remove(domingo))
            {
                Folgar(f, domingo);
                falta--;
                if (e.FolgaCasada && livres.Remove(domingo.AddDays(-1))) Folgar(f, domingo.AddDays(-1));
            }

            var posicaoExtra = f.FolgaExtra is { } fe && fe.NaSemana(segunda) > 0 ? fe.Posicao : PosicaoFolgaExtra.Livre;
            if (posicaoExtra == PosicaoFolgaExtra.FimDeSemana)
                foreach (var d in livres.Where(d => d.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday).Take(Math.Max(falta, 0)).ToList())
                {
                    livres.Remove(d);
                    Folgar(f, d);
                    falta--;
                }

            for (; falta > 0 && livres.Count > 0; falta--)
            {
                // nunca mais de 6 dias seguidos: a próxima folga não pode passar de última folga + 7
                var limite = PrazoDaProximaFolga(UltimaFolga(f, dias[0]), minhas);
                var candidatos = livres.Any(d => d <= limite) ? livres.Where(d => d <= limite).ToList() : livres;
                if (posicaoExtra == PosicaoFolgaExtra.JuntoDaFolgaNormal
                    && candidatos.Where(d => minhas.Contains(d.AddDays(-1)) || minhas.Contains(d.AddDays(1))).ToList() is { Count: > 0 } colados)
                    candidatos = colados;
                // folga extra só de segunda a sexta: o fim de semana fica reservado ao descanso
                // obrigatório, que já foi marcado acima — aqui só entra dia útil, se houver
                if (posicaoExtra == PosicaoFolgaExtra.DiaUtil
                    && candidatos.Where(d => d.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday)).ToList() is { Count: > 0 } uteis)
                    candidatos = uteis;
                if (e.FolgaCasada && candidatos.Where(d => !FimDeSemana(d)).ToList() is { Count: > 0 } semFimDeSemana)
                    candidatos = semFimDeSemana;
                if (candidatos.Where(d => !TresFolgasSeguidas(f, d)).ToList() is { Count: > 0 } espalhados)
                    candidatos = espalhados;
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
                   && livres.Where(d => d <= prazo).OrderBy(d => TresFolgasSeguidas(f, d)).ThenBy(d => e.FolgaCasada && FimDeSemana(d))
                       .ThenByDescending(d => Pontuar(f, d, feriado)).FirstOrDefault() is { } extra
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
            // mesma proteção, agora por turno: a sobra do dia não enxerga que as pessoas necessárias
            // têm de estar em turnos específicos, e esvaziaria o turno que cobre a abertura ou o fechamento
            if (f.TurnoPrincipal is { } tp)
            {
                var colegas = pessoas.Count(p => p.TurnoPrincipal == tp && !folgas.Contains((p.Id, d)) && !fixas[p.Id].Contains(d));
                if (colegas - 1 < PessoasNoTurno(d, tp)) pontos -= 50;
            }
            if (e.RodizioFeriados && feriado?.Data == d && TrabalhouUltimoFeriado(f, d) && sobra > 0) pontos += 100;
            // semana seguinte tem folga extra no sábado+domingo: folgar no fim desta evita 7 dias seguidos
            if (f.FolgaExtra is { Posicao: PosicaoFolgaExtra.FimDeSemana } fe
                && fe.NaSemana(segunda.AddDays(7)) > 0 && d.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                pontos += 30;
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

    static bool FimDeSemana(DateOnly d) => d.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

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
        var extras = (f.FolgaExtra?.NaSemana(segunda) ?? 0) + dias.Count(d => FolgaExtraAvulsa(f, d))
            + CompensacaoDeFeriado(f, segunda);
        return Math.Clamp(alvo - jaTrabalhados - extras, 0, dias.Count);
    }

    TimeSpan HorasDoTurnoPrincipal(Funcionario f)
    {
        var t = e.Turnos.FirstOrDefault(t => t.Id == f.TurnoPrincipal) ?? e.Turnos.FirstOrDefault(t => f.PodeTurno(t.Id));
        return t is null ? TimeSpan.FromHours(8) : Alocacao.Trabalho(f.Id, e.Inicio, t.Inicio, t.Fim, t.Intervalo).HorasComputadas;
    }

    /// Quem folga este domingo: todo mundo que a lei obriga e, até a parte justa da equipe (uma em cada
    /// N semanas), quem está há mais domingos trabalhando — um de cada turno por vez. Sem a cota, uma
    /// equipe sem histórico de domingos vencia o prazo toda junta e folgava o mesmo fim de semana.
    HashSet<Guid> QuemFolgaODomingo(List<Funcionario> pessoas, DateOnly domingo, Dictionary<Guid, HashSet<DateOnly>> fixas)
    {
        if (!NoPeriodo(domingo) || !Aberto(domingo)) return [];
        var noTurno = pessoas.GroupBy(f => f.TurnoPrincipal)
            .SelectMany(g => g.OrderBy(f => f.Nome, StringComparer.Ordinal).Select((f, i) => (f.Id, i))).ToDictionary(x => x.Id, x => x.i);
        bool Lei(Funcionario f) => Enumerable.Range(1, semanasLei - 1).All(i => Trabalhou(f, domingo.AddDays(-7 * i)));
        var fila = pessoas.Where(f => !fixas[f.Id].Contains(domingo))
            .OrderByDescending(Lei)
            .ThenByDescending(f => Enumerable.Range(1, semanasLei - 1).TakeWhile(i => Trabalhou(f, domingo.AddDays(-7 * i))).Count())
            .ThenBy(f => noTurno[f.Id]).ThenBy(f => f.Nome, StringComparer.Ordinal).ThenBy(f => f.Id)
            .ToList();
        // sem fim de semana casado nem frequência própria, o domingo livre é escolhido dia a dia (Pontuar)
        var cota = !e.FolgaCasada && e.DomingoACada is null ? 0
            : (int)Math.Ceiling(pessoas.Count / (double)semanasDomingo) - pessoas.Count(f => fixas[f.Id].Contains(domingo));
        return fila.Where((f, i) => Lei(f) || i < cota).Select(f => f.Id).ToHashSet();
    }

    /// Folgar em `d` deixaria três dias de folga seguidos (contando as já marcadas e o histórico).
    bool TresFolgasSeguidas(Funcionario f, DateOnly d)
    {
        bool Folga(DateOnly x) => folgas.Contains((f.Id, x)) || doFuncionario[f.Id].Any(a => a.Data == x && !a.Trabalha);
        return Folga(d.AddDays(-1)) && (Folga(d.AddDays(-2)) || Folga(d.AddDays(1))) || Folga(d.AddDays(1)) && Folga(d.AddDays(2));
    }

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
            else if (FolgaExtraAvulsa(f, dia)) hoje[f.Id] = Alocacao.Folga(f.Id, dia);
            else if (Afastado(f, dia)) hoje[f.Id] = new Alocacao(f.Id, dia, TipoAlocacao.Ocorrencia);
            else if (!Aberto(dia) || folgas.Contains((f.Id, dia)) || !DeveTrabalhar12x36(f, dia, i))
                hoje[f.Id] = Alocacao.Folga(f.Id, dia);
            else trabalhadores.Add(f);
        }

        var vagas = Aberto(dia)
            ? Demanda.Aplicaveis(e.Demandas, dia).OrderBy(d => d.EhFaixa)
                // faixa que poucos turnos alcançam primeiro: senão a faixa larga gasta quem fecharia a loja
                .ThenBy(d => d.EhFaixa ? e.Turnos.Count(t => Demanda.Cobre(t.Inicio, t.Fim, d.Inicio!.Value) || Demanda.Cobre(d.Inicio!.Value, d.Fim!.Value, t.Inicio)) : 0)
                .ThenBy(d => d.FuncaoId is null).ThenBy(d => TurnoIndex(d.TurnoId)).ToList()
            : [];
        foreach (var alvo in new Func<Demanda, int>[] { d => d.Minimo, d => d.Ideal })
        foreach (var vaga in vagas)
            Preencher(dia, vaga, alvo(vaga), hoje, trabalhadores);

        foreach (var f in trabalhadores.ToList())
        {
            var turnos = e.Turnos.Where(t => f.PodeTurno(t.Id))
                .OrderByDescending(t => !e.RodizioTurnos && t.Id == f.TurnoPrincipal)
                .ThenByDescending(t => FaltaNoTurno(t, vagas, hoje))
                .ThenBy(t => e.RodizioTurnos ? VezesNoTurno(f, t.Id, dia) : 0)
                .ThenBy(t => ContarNoTurno(t.Id, hoje)) // sobrou gente e a demanda esta atendida: equilibra os turnos
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
        while (Contar(vaga, hoje) < alvo)
        {
            var escolhido = TurnosQueServem(vaga, hoje)
                .SelectMany(t => trabalhadores.Select(f => (f, t, aloc: Tentar(f, dia, t, vaga.FuncaoId))))
                .Where(x => x.aloc is not null)
                .OrderBy(x => Preferido(x.f, dia, x.t) ? 0 : 1)
                // o turno principal vem antes de guardar especialista: ele é lotação, não
                // otimização. Na ordem inversa, dar uma função a alguém tirava a pessoa do
                // turno dela — quem era da manhã ia parar na tarde e vice-versa.
                .ThenBy(x => e.RodizioTurnos || x.f.TurnoPrincipal is null || x.f.TurnoPrincipal == x.t.Id ? 0 : 1)
                // cobrir turno alheio: tira de quem tem gente sobrando no turno principal, não de quem abre a loja
                .ThenByDescending(x => e.RodizioTurnos || x.f.TurnoPrincipal is not { } tp || tp == x.t.Id ? 0 : SobraNoTurno(tp, dia, hoje, trabalhadores))
                // e de quem tem horário parecido: da tarde para o fechamento dá para voltar no dia
                // seguinte; da manhã para o fechamento, as 11h de descanso prendem a pessoa na tarde
                .ThenBy(x => e.RodizioTurnos ? 0 : DistanciaDoPrincipal(x.f, x.t))
                .ThenBy(x => vaga.FuncaoId is null && x.f.Funcoes is { Count: > 0 } ? 1 : 0) // guarda especialistas
                // o turno de ontem vem antes do rodízio: a pessoa troca de turno na virada da semana,
                // depois da folga, e não de um dia para o outro
                .ThenBy(x => TurnoDeOntem(x.f, dia) is { } ontem && ontem != x.t.Id ? 1 : 0)
                .ThenBy(x => e.RodizioTurnos ? VezesNoTurno(x.f, x.t.Id, dia) : 0)
                .ThenBy(x => HorasNoPeriodo(x.f))
                .ThenBy(x => x.f.Nome, StringComparer.Ordinal).ThenBy(x => x.f.Id)
                .FirstOrDefault();
            if (escolhido.aloc is null) return;
            Atribuir(escolhido.f, escolhido.aloc, hoje, trabalhadores);
        }
    }

    /// Quantos do turno principal `tp` trabalham hoje além do que o turno precisa segurar sozinho.
    int SobraNoTurno(Guid tp, DateOnly dia, Dictionary<Guid, Alocacao> hoje, List<Funcionario> trabalhadores) =>
        e.Funcionarios.Count(p => p.TurnoPrincipal == tp && (trabalhadores.Contains(p) || hoje.GetValueOrDefault(p.Id) is { Trabalha: true }))
        - PessoasNoTurno(dia, tp);

    double DistanciaDoPrincipal(Funcionario f, Turno t) =>
        e.Turnos.FirstOrDefault(p => p.Id == f.TurnoPrincipal) is { } p ? Math.Abs((p.Inicio.ToTimeSpan() - t.Inicio.ToTimeSpan()).TotalHours) : 0;

    /// Turnos capazes de preencher a vaga: o dela, ou — numa faixa — os que cobrem o instante
    /// mais vazio, que é onde a faixa está furada agora.
    IEnumerable<Turno> TurnosQueServem(Demanda vaga, Dictionary<Guid, Alocacao> hoje)
    {
        if (!vaga.EhFaixa) return e.Turnos.Where(t => t.Id == vaga.TurnoId);
        var instante = vaga.MomentoMaisVazio(hoje.Values).Instante;
        return e.Turnos.Where(t => Demanda.Cobre(t.Inicio, t.Fim, instante)).OrderBy(t => TurnoIndex(t.Id));
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

    /// Quanto ainda falta para o ideal que este turno consegue atender — conta as faixas
    /// cujo instante mais vazio ele cobre, para quem sobra ir onde o movimento é maior.
    int FaltaNoTurno(Turno t, List<Demanda> vagas, Dictionary<Guid, Alocacao> hoje) =>
        vagas.Sum(v => v.EhFaixa
            ? Demanda.Cobre(t.Inicio, t.Fim, v.MomentoMaisVazio(hoje.Values).Instante) ? Math.Max(0, v.Ideal - Contar(v, hoje)) : 0
            : v.TurnoId == t.Id && v.FuncaoId is null ? v.Ideal - ContarNoTurno(t.Id, hoje) : 0);

    int ContarNoTurno(Guid turnoId, Dictionary<Guid, Alocacao> hoje) =>
        hoje.Values.Count(a => a.Trabalha && a.TurnoId == turnoId);

    int Contar(Demanda vaga, Dictionary<Guid, Alocacao> hoje) =>
        vaga.EhFaixa
            ? vaga.MomentoMaisVazio(hoje.Values).Presentes
            : hoje.Values.Count(a => a.Trabalha && a.TurnoId == vaga.TurnoId && (vaga.FuncaoId is null || a.FuncaoId == vaga.FuncaoId));

    /// Quanto um turno tem de segurar sozinho no dia: a demanda dele, ou a maior faixa de horário
    /// em que ele é o único turno presente em algum instante (abertura e fechamento, tipicamente).
    int PessoasNoTurno(DateOnly dia, Guid turnoId)
    {
        if (!Aberto(dia)) return 0;
        var doDia = Demanda.Aplicaveis(e.Demandas, dia).ToList();
        var porTurno = doDia.Where(d => !d.EhFaixa && d.TurnoId == turnoId && d.FuncaoId is null).Sum(d => d.Minimo);
        var porFaixa = doDia.Where(d => d.EhFaixa && d.TurnosSozinhos(e.Turnos).Any(t => t.Id == turnoId))
            .Select(d => d.Minimo).DefaultIfEmpty(0).Max();
        return Math.Max(porTurno, porFaixa);
    }

    /// Pessoas necessárias no dia (ou numa função): por turno, o maior entre a demanda geral e a soma das funções.
    int PessoasNecessarias(DateOnly dia, Guid? funcaoId = null)
    {
        if (!Aberto(dia)) return 0;
        if (funcaoId is not null)
            return Demanda.Aplicaveis(e.Demandas, dia).Where(d => d.FuncaoId == funcaoId).Sum(d => d.Minimo);
        return Demanda.PessoasNoDia(e.Demandas, dia, e.Turnos);
    }

    // ---------- Consultas ----------

    Alocacao? Travada(Funcionario f, DateOnly dia) => e.Travadas.FirstOrDefault(a => a.FuncionarioId == f.Id && a.Data == dia);

    bool Afastado(Funcionario f, DateOnly dia) => e.Ocorrencias.Any(o => o.FuncionarioId == f.Id && o.Afasta && o.Cobre(dia));

    bool FolgaExtraAvulsa(Funcionario f, DateOnly dia) =>
        e.Ocorrencias.Any(o => o.FuncionarioId == f.Id && o.Tipo == TipoOcorrencia.FolgaExtra && o.Cobre(dia));

    bool Trabalhou(Funcionario f, DateOnly dia) => doFuncionario[f.Id].Any(a => a.Data == dia && a.Trabalha);

    /// Folgas a mais nesta semana por feriado trabalhado na semana anterior. Feriado na última
    /// semana do período fica sem compensação: ela cai na escala seguinte, quando for gerada.
    /// ponytail: não marca o que já foi compensado — regerar um período que começa logo depois
    /// do feriado compensa de novo; some o histórico da escala anterior para evitar.
    int CompensacaoDeFeriado(Funcionario f, DateOnly segunda) =>
        e.CompensarFeriado
            ? feriados.Count(x => x.Data >= segunda.AddDays(-7) && x.Data < segunda && Trabalhou(f, x.Data))
            : 0;

    /// Vezes que a pessoa fez esse turno nas últimas 4 semanas — quem fez menos entra primeiro.
    int VezesNoTurno(Funcionario f, Guid turnoId, DateOnly dia) =>
        doFuncionario[f.Id].Count(a => a.Trabalha && a.TurnoId == turnoId && a.Data >= dia.AddDays(-28) && a.Data < dia);

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

    int TurnoIndex(Guid? turnoId) => e.Turnos.ToList().FindIndex(t => t.Id == turnoId);

    static DateOnly InicioDaSemana(DateOnly d) => d.AddDays(-(((int)d.DayOfWeek + 6) % 7));
}

public static class SituacaoInicialExtensoes
{
    /// Sem histórico no app: reconstrói os dias desde a última folga informada no cadastro.
    public static IReadOnlyCollection<Alocacao> HistoricoPresumido(
        this Funcionario f, DateOnly inicio, IReadOnlyList<Turno> turnos, IEnumerable<Feriado> feriados)
    {
        if (f.Situacao is not { } s || s.UltimaFolga >= inicio) return [];
        var turno = turnos.FirstOrDefault(t => t.Id == f.TurnoPrincipal) ?? turnos.FirstOrDefault(t => f.PodeTurno(t.Id));
        Alocacao Trabalho(DateOnly d) => turno is null
            ? Alocacao.Trabalho(f.Id, d, new(8, 0), new(16, 0), TimeSpan.FromHours(1))
            : Alocacao.Trabalho(f.Id, d, turno.Inicio, turno.Fim, turno.Intervalo, turno.Id);

        var dias = new Dictionary<DateOnly, Alocacao>();
        if (f.Regime == Regime.DozePorTrintaESeis)
        {
            // plantão no dia anterior à folga informada mais recente
            var ontem = inicio.AddDays(-1);
            var plantao = s.UltimaFolga == ontem ? ontem.AddDays(-1) : ontem;
            dias[plantao] = Trabalho(plantao);
        }
        else
        {
            dias[s.UltimaFolga] = Alocacao.Folga(f.Id, s.UltimaFolga);
            for (var d = s.UltimaFolga.AddDays(1); d < inicio; d = d.AddDays(1)) dias[d] = Trabalho(d);
            if (s.UltimoDomingoDeFolga is { } dom && dom <= s.UltimaFolga)
                for (var d = dom; d < s.UltimaFolga; d = d.AddDays(7))
                    dias.TryAdd(d, d == dom ? Alocacao.Folga(f.Id, d) : Trabalho(d));
        }

        if (s.TrabalhouUltimoFeriado is { } trabalhou
            && feriados.Where(x => x.Considerado && x.Data < inicio).MaxBy(x => x.Data) is { } feriado)
            dias.TryAdd(feriado.Data, trabalhou ? Trabalho(feriado.Data) : Alocacao.Folga(f.Id, feriado.Data));

        return dias.Values;
    }
}
