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

/// Onde cai a folga extra: qualquer dia, sábado + domingo, ou colada na folga normal da semana.
public enum PosicaoFolgaExtra { Livre, FimDeSemana, JuntoDaFolgaNormal }

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
public sealed record Demanda(
    Guid TurnoId,
    int Minimo,
    int Ideal,
    Guid? FuncaoId = null,
    DayOfWeek? DiaSemana = null,
    DateOnly? Data = null)
{
    public bool ValeEm(DateOnly dia) => Data is { } d ? d == dia : DiaSemana == dia.DayOfWeek;

    /// Demandas do dia; a de data específica substitui a do dia da semana para o mesmo turno/função.
    public static IEnumerable<Demanda> Aplicaveis(IEnumerable<Demanda> demandas, DateOnly dia)
    {
        var doDia = demandas.Where(d => d.ValeEm(dia)).ToList();
        return doDia.Where(d => d.Data is not null
            || !doDia.Any(x => x.Data is not null && x.TurnoId == d.TurnoId && x.FuncaoId == d.FuncaoId));
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
