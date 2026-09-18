namespace Folguinha.Domain;

// TenantId/UnidadeId não aparecem aqui: são colunas de persistência (Infrastructure),
// as regras e o gerador trabalham sempre dentro de uma empresa/unidade.

public enum RamoAtividade { Comercio, Outro }

public enum Regime { SeisPorUm, CincoPorDois, DozePorTrintaESeis, Personalizado }

public enum TipoContrato { Clt, TempoParcial, Aprendiz, Intermitente }

public sealed record Empresa(Guid Id, string Nome, RamoAtividade Ramo);

public sealed record Funcao(Guid Id, string Nome);

/// Turno com `Fim` menor ou igual a `Inicio` termina no dia seguinte.
public sealed record Turno(Guid Id, string Nome, TimeOnly Inicio, TimeOnly Fim, TimeSpan Intervalo);

/// Listas nulas = sem restrição (qualquer função/turno, disponível todos os dias).
public sealed record Funcionario(
    Guid Id,
    string Nome,
    Regime Regime,
    TimeSpan CargaSemanal,
    TipoContrato Contrato = TipoContrato.Clt,
    bool MenorDeIdade = false,
    bool PermiteHoraExtra = false,
    TimeSpan LimiteHoraExtraSemanal = default)
{
    public string? Matricula { get; init; }
    public string? Cargo { get; init; }
    public string? Setor { get; init; }
    public IReadOnlyList<Guid>? Funcoes { get; init; }
    public IReadOnlyList<Guid>? Turnos { get; init; }
    public Guid? TurnoPrincipal { get; init; }
    public IReadOnlyList<Disponibilidade>? Disponibilidades { get; init; }
    /// Como a pessoa chega na primeira escala (usado enquanto não há histórico no app).
    public SituacaoInicial? Situacao { get; init; }
    public FolgaExtraPeriodica? FolgaExtra { get; init; }

    public bool TemFuncao(Guid funcaoId) => Funcoes?.Contains(funcaoId) == true;

    public bool PodeTurno(Guid turnoId) => Turnos is null || Turnos.Contains(turnoId);
}

/// Informado no cadastro: a escala nova continua a sequência que a pessoa já vinha fazendo.
/// Para 12x36, `UltimaFolga` é o último dia sem plantão.
public sealed record SituacaoInicial(
    DateOnly UltimaFolga,
    DateOnly? UltimoDomingoDeFolga = null,
    bool? TrabalhouUltimoFeriado = null);

/// Onde cai a folga extra: qualquer dia, sábado + domingo, colada na folga normal da semana,
/// ou só de segunda a sexta — caso comum em quem já folga o fim de semana pelo descanso obrigatório.
public enum PosicaoFolgaExtra { Livre, FimDeSemana, JuntoDaFolgaNormal, DiaUtil }

/// Folgas além do regime, ex.: 6x1 com uma folga a mais a cada 2 semanas (contadas a partir da semana de `APartirDe`).
public sealed record FolgaExtraPeriodica(
    int ACadaSemanas,
    DateOnly APartirDe,
    int Quantidade = 1,
    PosicaoFolgaExtra Posicao = PosicaoFolgaExtra.Livre)
{
    public int NaSemana(DateOnly segunda)
    {
        var inicio = APartirDe.AddDays(-(((int)APartirDe.DayOfWeek + 6) % 7));
        var semanas = (segunda.DayNumber - inicio.DayNumber) / 7;
        return semanas >= 0 && semanas % ACadaSemanas == 0 ? Quantidade : 0;
    }
}

public enum TipoRestricao { Contratual, Permanente, Temporaria, Preferencia }

/// No `Dia`, a pessoa só trabalha dentro de [Inicio, Fim]; sem horários = não trabalha nesse dia.
/// `Preferencia` o gerador tenta respeitar; os demais tipos são obrigatórios (ESPEC §5.1).
public sealed record Disponibilidade(
    DayOfWeek Dia,
    TimeOnly? Inicio,
    TimeOnly? Fim,
    TipoRestricao Tipo = TipoRestricao.Contratual,
    DateOnly? ValidaAte = null)
{
    public bool ValeEm(DateOnly dia) => dia.DayOfWeek == Dia && (ValidaAte is null || dia <= ValidaAte);

    public bool Comporta(Turno t) =>
        Inicio is { } ini && Fim is { } fim && t.Inicio >= ini && t.Fim > t.Inicio && t.Fim <= fim;
}

/// Horário de abertura da unidade; vários períodos no mesmo dia são permitidos.
public sealed record PeriodoFuncionamento(DayOfWeek Dia, TimeOnly Abre, TimeOnly Fecha);

/// Mínimo de pessoas num turno (opcionalmente numa função). Vale para um dia da semana
/// ou para uma data específica; a data específica substitui o dia da semana.
///
/// Com `TurnoId` nulo é uma **faixa de horário** da unidade: exige `Minimo` pessoas presentes
/// em todo instante entre `Inicio` e `Fim`, somando quem estiver em qualquer turno que cubra
/// aquele instante (inclusive a sobreposição entre manhã e tarde). É como se diz "no pico
/// preciso de 4, no horário morto 2 bastam" sem inventar um turno para cada faixa.
public sealed record Demanda(
    Guid? TurnoId,
    int Minimo,
    int Ideal,
    Guid? FuncaoId = null,
    DayOfWeek? DiaSemana = null,
    DateOnly? Data = null,
    TimeOnly? Inicio = null,
    TimeOnly? Fim = null)
{
    public bool EhFaixa => TurnoId is null && Inicio is not null && Fim is not null;

    public bool ValeEm(DateOnly dia) => Data is { } d ? d == dia : DiaSemana == dia.DayOfWeek;

    /// Demandas do dia; a de data específica substitui a do dia da semana para o mesmo turno/função/faixa.
    public static IEnumerable<Demanda> Aplicaveis(IEnumerable<Demanda> demandas, DateOnly dia)
    {
        var doDia = demandas.Where(d => d.ValeEm(dia)).ToList();
        return doDia.Where(d => d.Data is not null
            || !doDia.Any(x => x.Data is not null && x.TurnoId == d.TurnoId && x.FuncaoId == d.FuncaoId && x.Inicio == d.Inicio));
    }

    /// Verdadeiro se quem entra às `inicio` e sai às `fim` está presente no instante `h`
    /// (turno que vira o dia tem `fim` menor que `inicio`).
    public static bool Cobre(TimeOnly inicio, TimeOnly fim, TimeOnly h) =>
        fim > inicio ? inicio <= h && fim > h : inicio <= h || fim > h;

    /// Momento mais vazio da faixa e quantas pessoas há nele. As alocações devem ser as de um
    /// único dia; a lotação só muda quando alguém entra ou sai, então basta olhar esses instantes.
    /// ponytail: ignora faixa que atravessa a meia-noite; parta em duas se a loja virar o dia.
    public (int Presentes, TimeOnly Instante) MomentoMaisVazio(IEnumerable<Alocacao> doDia)
    {
        if (Inicio is not { } ini || Fim is not { } fim) return (0, default);
        var presentes = doDia.Where(a => a.Trabalha && (FuncaoId is null || a.FuncaoId == FuncaoId)).ToList();
        return presentes
            .SelectMany(a => new[] { a.Inicio, a.Fim })
            .Where(h => h > ini && h < fim)
            .Append(ini)
            .Distinct()
            .Select(h => (Presentes: presentes.Count(a => Cobre(a.Inicio, a.Fim, h)), Instante: h))
            .MinBy(x => x.Presentes);
    }

    /// Quantas pessoas o dia inteiro precisa: por turno, o maior entre a demanda geral e a soma
    /// das funções; numa faixa, cada turno que fica sozinho em algum instante precisa segurar
    /// o mínimo sozinho, então eles somam.
    public static int PessoasNoDia(IEnumerable<Demanda> demandas, DateOnly dia, IReadOnlyList<Turno> turnos)
    {
        var doDia = Aplicaveis(demandas, dia).ToList();
        var porTurno = doDia.Where(d => !d.EhFaixa).GroupBy(d => d.TurnoId).Sum(g =>
            Math.Max(g.Where(d => d.FuncaoId is null).Sum(d => d.Minimo), g.Where(d => d.FuncaoId is not null).Sum(d => d.Minimo)));
        var porFaixa = turnos.Sum(t => doDia
            .Where(d => d.EhFaixa && d.TurnosSozinhos(turnos).Any(x => x.Id == t.Id))
            .Select(d => d.Minimo).DefaultIfEmpty(0).Max());
        return Math.Max(porTurno, porFaixa);
    }

    /// Turnos que ficam sozinhos em algum instante da faixa — cada um precisa segurar o mínimo
    /// por conta própria. Onde os turnos se sobrepõem, eles somam e ninguém precisa segurar sozinho.
    public IEnumerable<Turno> TurnosSozinhos(IReadOnlyList<Turno> turnos)
    {
        if (Inicio is not { } ini || Fim is not { } fim) yield break;
        var instantes = turnos.SelectMany(t => new[] { t.Inicio, t.Fim }).Where(h => h > ini && h < fim).Append(ini).Distinct().ToList();
        foreach (var t in turnos)
            if (instantes.Any(h => Cobre(t.Inicio, t.Fim, h) && turnos.Count(o => Cobre(o.Inicio, o.Fim, h)) == 1))
                yield return t;
    }
}

public enum AbrangenciaFeriado { Nacional, Estadual, Municipal, PontoFacultativo }

public enum OrigemFeriado { Api, Manual }

/// `Considerado = false` faz o dia ser tratado como dia normal.
public sealed record Feriado(
    DateOnly Data,
    string Nome,
    AbrangenciaFeriado Abrangencia,
    bool Considerado = true,
    OrigemFeriado Origem = OrigemFeriado.Api);

public enum TipoOcorrencia
{
    Atestado, FaltaJustificada, FaltaInjustificada, Ferias, Licenca, Acidente, Atraso,
    SaidaAntecipada, AusenciaParcial, CompromissoAprovado, ConvocacaoExtraordinaria, TrocaVoluntaria,
    /// Folga concedida além do regime (vira folga na escala e não substitui as folgas normais).
    FolgaExtra,
}

/// Guarda só o motivo administrativo (`Tipo`/`Observacao`); nunca diagnóstico (LGPD).
public sealed record Ocorrencia(
    Guid Id,
    Guid FuncionarioId,
    TipoOcorrencia Tipo,
    DateOnly Inicio,
    DateOnly Fim,
    string? Observacao = null)
{
    /// Tipos em que a pessoa não pode estar escalada no período.
    public bool Afasta => Tipo is TipoOcorrencia.Atestado or TipoOcorrencia.FaltaJustificada or TipoOcorrencia.Ferias
        or TipoOcorrencia.Licenca or TipoOcorrencia.Acidente or TipoOcorrencia.CompromissoAprovado or TipoOcorrencia.FolgaExtra;

    public bool Cobre(DateOnly dia) => dia >= Inicio && dia <= Fim;
}
