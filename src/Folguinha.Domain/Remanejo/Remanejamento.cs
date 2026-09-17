using Folguinha.Domain.Regras;
using static Folguinha.Domain.Texto;

namespace Folguinha.Domain.Remanejo;

/// Uma opção de cobertura para um turno afetado (ESPEC §9.2).
/// `Mudancas` substitui alocações do substituto: o turno coberto e, se for o caso, a folga movida.
public sealed record Substituto(
    Funcionario Funcionario,
    Alocacao Turno,
    bool Elegivel,
    TimeSpan HorasExtras,
    DateOnly? FolgaMovidaPara,
    IReadOnlyList<Alocacao> Mudancas,
    IReadOnlyList<Violacao> Problemas,
    string Resumo);

/// `Cobertura`: fração da demanda mínima atendida nos dias da ocorrência.
public sealed record Impacto(double Cobertura, TimeSpan HorasExtras, IReadOnlyList<Guid> Afetados);

public sealed record ResultadoRemanejamento(IReadOnlyList<Alocacao> Alocacoes, IReadOnlyList<Violacao> Violacoes, Impacto Impacto)
{
    public bool PodePublicar => Violacoes.All(v => v.Severidade != Severidade.Bloqueio);
}

/// Ocorrência → turnos afetados → substitutos → simulação (ESPEC §9). A escala atual vem num ContextoValidacao.
public static class Remanejamento
{
    public static IReadOnlyList<Alocacao> TurnosAfetados(ContextoValidacao escala, Ocorrencia o) =>
        o.Afasta
            ? escala.Alocacoes.Where(a => a.FuncionarioId == o.FuncionarioId && a.Trabalha && o.Cobre(a.Data)).OrderBy(a => a.Data).ToList()
            : [];

    /// Quem está de folga no dia e pode assumir o turno, do melhor para o pior; inelegíveis vêm no fim com o motivo.
    public static IReadOnlyList<Substituto> SugerirSubstitutos(
        ContextoValidacao escala, Ocorrencia o, Alocacao turno, IReadOnlyList<Regra>? regras = null)
    {
        var obrigatorias = (regras ?? CatalogoRegras.Padrao())
            .Where(r => r.Definicao.Nivel == NivelRegra.Obrigatoria && r is not CoberturaMinima).ToList();
        var comAusencia = ComOcorrencia(escala, o);

        return escala.Funcionarios
            .Where(f => f.Id != o.FuncionarioId
                        && (turno.FuncaoId is not { } fn || f.TemFuncao(fn))
                        && (turno.TurnoId is not { } t || f.PodeTurno(t))
                        && Alocacao(comAusencia, f, turno.Data) is { Tipo: TipoAlocacao.Folga, Travada: false })
            .Select(f => Avaliar(comAusencia, f, turno, obrigatorias))
            .OrderByDescending(s => s.Elegivel)
            .ThenBy(s => s.HorasExtras)
            .ThenBy(s => s.FolgaMovidaPara is null ? 0 : 1)
            .ThenBy(s => s.Funcionario.TurnoPrincipal == turno.TurnoId ? 0 : 1)
            .ThenBy(s => HorasNoPeriodo(comAusencia, s.Funcionario))
            .ThenBy(s => s.Funcionario.Nome, StringComparer.Ordinal)
            .ToList();
    }

    /// Simula (ou aplica) a ocorrência com os substitutos escolhidos. Tudo que muda fica travado.
    public static ResultadoRemanejamento Aplicar(
        ContextoValidacao escala, Ocorrencia o, IReadOnlyList<Substituto> escolhidos, IReadOnlyList<Regra>? regras = null)
    {
        var novo = escolhidos.Aggregate(ComOcorrencia(escala, o), (ctx, s) => Trocar(ctx, s.Mudancas));
        var impacto = new Impacto(
            Cobertura(novo, o),
            escolhidos.Aggregate(TimeSpan.Zero, (t, s) => t + s.HorasExtras),
            [.. new[] { o.FuncionarioId }.Concat(escolhidos.Select(s => s.Funcionario.Id)).Distinct()]);
        return new ResultadoRemanejamento(novo.Alocacoes, [.. Validador.Validar(novo, regras ?? CatalogoRegras.Padrao())], impacto);
    }

    sealed record Variante(ContextoValidacao Escala, IReadOnlyList<Alocacao> Mudancas, List<Violacao> Problemas, TimeSpan Extras, DateOnly? Folga);

    static Substituto Avaliar(ContextoValidacao escala, Funcionario f, Alocacao turno, List<Regra> obrigatorias)
    {
        var cobrir = turno with { FuncionarioId = f.Id, Travada = true };
        var melhor = Simular([cobrir], null);

        if (melhor.Problemas.Count > 0 || melhor.Extras > TimeSpan.Zero)
        {
            // devolve a folga em outro dia da mesma semana, sem piorar a cobertura desse dia
            var segunda = Segunda(turno.Data);
            var movida = escala.Alocacoes
                .Where(a => a.FuncionarioId == f.Id && a.Trabalha && !a.Travada && a.Data != turno.Data
                            && a.Data >= segunda && a.Data <= segunda.AddDays(6) && escala.NoPeriodo(a.Data))
                .OrderBy(a => Math.Abs(a.Data.DayNumber - turno.Data.DayNumber)).ThenBy(a => a.Data)
                .Select(a => Simular([cobrir, global::Folguinha.Domain.Alocacao.Folga(f.Id, a.Data) with { Travada = true }], a.Data))
                .FirstOrDefault(v => v.Problemas.Count == 0 && !PioraCobertura(escala, v.Escala, v.Folga!.Value));
            if (movida is not null && (melhor.Problemas.Count > 0 || movida.Extras < melhor.Extras)) melhor = movida;
        }

        var elegivel = melhor.Problemas.Count == 0;
        var resumo = elegivel
            ? string.Join(" · ", new[]
            {
                escala.Funcoes.FirstOrDefault(x => x.Id == turno.FuncaoId)?.Nome,
                $"{Horas(HorasNaSemana(melhor.Escala, f, turno.Data))} de {Horas(f.CargaSemanal)} na semana",
                melhor.Extras > TimeSpan.Zero ? $"gera {Horas(melhor.Extras)} extras" : "sem hora extra",
                melhor.Folga is { } d ? $"folga passa para {Dia(d)}" : null,
            }.OfType<string>())
            : melhor.Problemas[0].Mensagem;
        return new Substituto(f, turno, elegivel, melhor.Extras, melhor.Folga, melhor.Mudancas, melhor.Problemas, resumo);

        Variante Simular(IReadOnlyList<Alocacao> mudancas, DateOnly? folga)
        {
            var ctx = Trocar(escala, mudancas);
            var problemas = Validador.Validar(ctx with { Funcionarios = [f] }, obrigatorias).ToList();
            return new Variante(ctx, mudancas, problemas, HorasExtras(escala, ctx, f, turno.Data), folga);
        }
    }

    static ContextoValidacao ComOcorrencia(ContextoValidacao escala, Ocorrencia o)
    {
        if (escala.Ocorrencias.Contains(o)) return escala;
        var tipo = o.Tipo == TipoOcorrencia.FolgaExtra ? TipoAlocacao.Folga : TipoAlocacao.Ocorrencia;
        var ausencias = TurnosAfetados(escala, o).Select(a => new Alocacao(a.FuncionarioId, a.Data, tipo, Travada: true)).ToList();
        return Trocar(escala, ausencias) with { Ocorrencias = [.. escala.Ocorrencias, o] };
    }

    static ContextoValidacao Trocar(ContextoValidacao escala, IReadOnlyList<Alocacao> mudancas) => escala with
    {
        Alocacoes = [.. escala.Alocacoes.Where(a => !mudancas.Any(m => m.FuncionarioId == a.FuncionarioId && m.Data == a.Data)), .. mudancas],
    };

    static Alocacao? Alocacao(ContextoValidacao escala, Funcionario f, DateOnly dia) =>
        escala.Alocacoes.FirstOrDefault(a => a.FuncionarioId == f.Id && a.Data == dia);

    static bool PioraCobertura(ContextoValidacao antes, ContextoValidacao depois, DateOnly dia)
    {
        int Faltas(ContextoValidacao c) => new CoberturaMinima().Validar(c with { Inicio = dia, Fim = dia }).Count();
        return Faltas(depois) > Faltas(antes);
    }

    static double Cobertura(ContextoValidacao escala, Ocorrencia o)
    {
        int exigido = 0, atendido = 0;
        for (var dia = o.Inicio; dia <= o.Fim; dia = dia.AddDays(1))
        {
            if (!escala.NoPeriodo(dia) || escala.DiasFechados.Contains(dia)) continue;
            foreach (var d in Demanda.Aplicaveis(escala.Demandas, dia))
            {
                var escalados = escala.Alocacoes.Count(a => a.Trabalha && a.Data == dia && a.TurnoId == d.TurnoId
                                                            && (d.FuncaoId is null || a.FuncaoId == d.FuncaoId));
                exigido += d.Minimo;
                atendido += Math.Min(escalados, d.Minimo);
            }
        }
        return exigido == 0 ? 1.0 : (double)atendido / exigido;
    }

    static DateOnly Segunda(DateOnly d) => d.AddDays(-(((int)d.DayOfWeek + 6) % 7));

    static TimeSpan HorasNaSemana(ContextoValidacao escala, Funcionario f, DateOnly dia) =>
        escala.TrabalhoDe(f).Where(a => a.Data >= Segunda(dia) && a.Data <= Segunda(dia).AddDays(6))
            .Aggregate(TimeSpan.Zero, (t, a) => t + a.HorasComputadas);

    /// Horas extras que a mudança acrescenta na semana (acima da carga contratual).
    static TimeSpan HorasExtras(ContextoValidacao antes, ContextoValidacao depois, Funcionario f, DateOnly dia)
    {
        TimeSpan Excesso(ContextoValidacao c)
        {
            var x = HorasNaSemana(c, f, dia) - f.CargaSemanal;
            return x > TimeSpan.Zero ? x : TimeSpan.Zero;
        }
        var extra = Excesso(depois) - Excesso(antes);
        return extra > TimeSpan.Zero ? extra : TimeSpan.Zero;
    }

    static double HorasNoPeriodo(ContextoValidacao escala, Funcionario f) =>
        escala.TrabalhoDe(f).Where(a => escala.NoPeriodo(a.Data)).Sum(a => a.HorasComputadas.TotalHours);
}
