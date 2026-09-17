using Folguinha.Domain;
using Folguinha.Domain.Regras;
using Folguinha.Domain.Remanejo;
using static Folguinha.Domain.Texto;

namespace Folguinha.Application;

public sealed record TurnoAfetado(Guid EscalaId, Alocacao Turno);

public sealed record ResultadoAprovacao(DadosDaUnidade Dados, IReadOnlyList<Violacao> Pendencias, Impacto Impacto);

/// Fluxo de ocorrência (ESPEC §9): registrar → afetados → substitutos → simular → aprovar → nova versão.
public static class Ocorrencias
{
    public static string Nome(TipoOcorrencia tipo) => tipo switch
    {
        TipoOcorrencia.Atestado => "Atestado",
        TipoOcorrencia.FaltaJustificada => "Falta justificada",
        TipoOcorrencia.FaltaInjustificada => "Falta injustificada",
        TipoOcorrencia.Ferias => "Férias",
        TipoOcorrencia.Licenca => "Licença",
        TipoOcorrencia.Acidente => "Acidente",
        TipoOcorrencia.Atraso => "Atraso",
        TipoOcorrencia.SaidaAntecipada => "Saída antecipada",
        TipoOcorrencia.AusenciaParcial => "Ausência parcial",
        TipoOcorrencia.CompromissoAprovado => "Compromisso aprovado",
        TipoOcorrencia.ConvocacaoExtraordinaria => "Convocação extraordinária",
        TipoOcorrencia.TrocaVoluntaria => "Troca voluntária",
        TipoOcorrencia.FolgaExtra => "Folga extra",
        _ => tipo.ToString(),
    };

    public static IReadOnlyList<TurnoAfetado> TurnosAfetados(DadosDaUnidade dados, Ocorrencia o) =>
        [.. dados.Escalas.Where(e => !e.Encerrada && e.Inicio <= o.Fim && e.Fim >= o.Inicio)
            .SelectMany(e => Remanejamento.TurnosAfetados(Escalas.Contexto(dados, e), o).Where(t => e.Cobre(t.Data)).Select(t => new TurnoAfetado(e.Id, t)))
            .OrderBy(t => t.Turno.Data)];

    public static IReadOnlyList<Substituto> Substitutos(DadosDaUnidade dados, Ocorrencia o, TurnoAfetado afetado)
    {
        var escala = dados.Escala(afetado.EscalaId) ?? throw new EscalaException("Escala não encontrada.");
        return Remanejamento.SugerirSubstitutos(Escalas.Contexto(dados, escala), o, afetado.Turno, Escalas.Regras(dados, escala.Inicio));
    }

    /// Resultado sem gravar nada (para mostrar o impacto antes de aprovar).
    public static IReadOnlyList<(Escala Escala, ResultadoRemanejamento Resultado)> Simular(
        DadosDaUnidade dados, Ocorrencia o, IReadOnlyList<Substituto> escolhidos) =>
        [.. dados.Escalas.Where(e => !e.Encerrada && e.Inicio <= o.Fim && e.Fim >= o.Inicio)
            .Select(e => (e, Remanejamento.Aplicar(Escalas.Contexto(dados, e), o,
                [.. escolhidos.Where(s => e.Cobre(s.Turno.Data))], Escalas.Regras(dados, e.Inicio))))];

    /// Grava a ocorrência e as trocas. Escalas já publicadas ganham nova versão (se não houver bloqueio).
    public static ResultadoAprovacao Aprovar(
        DadosDaUnidade dados, Ocorrencia o, IReadOnlyList<Substituto> escolhidos,
        IReadOnlyDictionary<string, string> justificativas, DateTimeOffset agora)
    {
        var pessoa = dados.Funcionario(o.FuncionarioId) ?? throw new EscalaException("Funcionário não encontrado.");
        var simulacoes = Simular(dados, o, escolhidos);
        var novos = dados with { Ocorrencias = [.. dados.Ocorrencias.Where(x => x.Id != o.Id), o] };
        var pendencias = new List<Violacao>();

        foreach (var (escala, resultado) in simulacoes)
        {
            var alocacoes = resultado.Alocacoes.Where(a => escala.Cobre(a.Data)).ToList();
            if (Escalas.Diferencas(escala.Atual, alocacoes).Count == 0) continue;
            novos = novos.ComEscala(novos.Escala(escala.Id)! with { Rascunho = alocacoes });
            if (escala.Versoes.Count == 0) continue;

            // o motivo não revela o tipo de afastamento (LGPD), exceto folga extra
            var motivo = o.Tipo == TipoOcorrencia.FolgaExtra
                ? $"Folga extra de {pessoa.Nome} ({Periodo(o)})"
                : $"Ausência aprovada de {pessoa.Nome} ({Periodo(o)})";
            var publicacao = Escalas.Publicar(novos, escala.Id, motivo, justificativas, agora);
            if (publicacao.Publicado) novos = publicacao.Dados;
            else pendencias.AddRange(publicacao.Pendencias);
        }

        var impacto = simulacoes.Count == 0
            ? new Impacto(1, TimeSpan.Zero, [o.FuncionarioId])
            : new Impacto(simulacoes.Average(s => s.Resultado.Impacto.Cobertura),
                simulacoes.Aggregate(TimeSpan.Zero, (t, s) => t + s.Resultado.Impacto.HorasExtras),
                [.. simulacoes.SelectMany(s => s.Resultado.Impacto.Afetados).Distinct()]);
        return new ResultadoAprovacao(novos, pendencias, impacto);
    }

    public static DadosDaUnidade Remover(DadosDaUnidade dados, Guid ocorrenciaId) =>
        dados with { Ocorrencias = [.. dados.Ocorrencias.Where(o => o.Id != ocorrenciaId)] };

    static string Periodo(Ocorrencia o) =>
        o.Inicio == o.Fim ? Dia(o.Inicio) : $"{Dia(o.Inicio)} a {Dia(o.Fim)}";
}
