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

public sealed record Funcionario(
    Guid Id,
    string Nome,
    Regime Regime,
    TimeSpan CargaSemanal,
    TipoContrato Contrato = TipoContrato.Clt,
    bool MenorDeIdade = false,
    bool PermiteHoraExtra = false,
    TimeSpan LimiteHoraExtraSemanal = default);

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
        or TipoOcorrencia.Licenca or TipoOcorrencia.Acidente or TipoOcorrencia.CompromissoAprovado;

    public bool Cobre(DateOnly dia) => dia >= Inicio && dia <= Fim;
}
