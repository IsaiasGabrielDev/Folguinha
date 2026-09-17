namespace Folguinha.Domain;

public enum TipoAlocacao { Trabalho, Folga, Ocorrencia }

/// O que um funcionário faz num dia da escala. Horários só fazem sentido em `Trabalho`.
public sealed record Alocacao(
    Guid FuncionarioId,
    DateOnly Data,
    TipoAlocacao Tipo,
    TimeOnly Inicio = default,
    TimeOnly Fim = default,
    TimeSpan Intervalo = default,
    Guid? TurnoId = null,
    Guid? FuncaoId = null,
    bool Travada = false)
{
    static readonly TimeSpan HoraNoturnaReduzida = new(0, 52, 30);

    public static Alocacao Trabalho(Guid funcionarioId, DateOnly data, TimeOnly inicio, TimeOnly fim, TimeSpan intervalo,
        Guid? turnoId = null, Guid? funcaoId = null) =>
        new(funcionarioId, data, TipoAlocacao.Trabalho, inicio, fim, intervalo, turnoId, funcaoId);

    public static Alocacao Folga(Guid funcionarioId, DateOnly data) => new(funcionarioId, data, TipoAlocacao.Folga);

    public bool Trabalha => Tipo == TipoAlocacao.Trabalho;

    public DateTime InicioEm => Data.ToDateTime(Inicio);

    public DateTime FimEm => Fim > Inicio ? Data.ToDateTime(Fim) : Data.AddDays(1).ToDateTime(Fim);

    /// Tempo de relógio trabalhado (presença menos intervalo).
    public TimeSpan Trabalhado => FimEm - InicioEm - Intervalo;

    /// Minutos entre 22h e 5h (CLT art. 73).
    public TimeSpan Noturno
    {
        get
        {
            var total = TimeSpan.Zero;
            for (var d = Data.AddDays(-1); d <= Data.AddDays(1); d = d.AddDays(1))
            {
                var ini = d.ToDateTime(new TimeOnly(22, 0));
                var fim = ini.AddHours(7);
                var sobreposto = Min(fim, FimEm) - Max(ini, InicioEm);
                if (sobreposto > TimeSpan.Zero) total += sobreposto;
            }
            return total;
        }
    }

    /// Horas para fins de limite de jornada: a hora noturna vale 52min30s (CLT art. 73 §1º).
    /// Simplificação: o intervalo é descontado como hora diurna.
    public TimeSpan HorasComputadas =>
        Trabalhado + TimeSpan.FromMinutes(Math.Round(Noturno.TotalMinutes * 60 / HoraNoturnaReduzida.TotalMinutes)) - Noturno;

    static DateTime Min(DateTime a, DateTime b) => a < b ? a : b;
    static DateTime Max(DateTime a, DateTime b) => a > b ? a : b;
}
