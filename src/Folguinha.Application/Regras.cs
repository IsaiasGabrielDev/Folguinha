using Folguinha.Domain.Regras;

namespace Folguinha.Application;

/// Mudança de nível de uma regra, versionada e com vigência (ESPEC §7.2).
public sealed record RegraConfigurada(
    string RegraId,
    NivelRegra Nivel,
    DateOnly VigenteDesde,
    int Versao,
    string Responsavel,
    DateTimeOffset AlteradaEm,
    string? Motivo = null);

public static class ConfiguracaoDeRegras
{
    /// Catálogo com a configuração vigente na data (a mais recente com início até a data).
    public static IReadOnlyList<Regra> Aplicar(IEnumerable<RegraConfigurada> configuradas, DateOnly data) =>
        [.. CatalogoRegras.Padrao().Select(r =>
            configuradas.Where(c => c.RegraId == r.Definicao.Id && c.VigenteDesde <= data)
                .MaxBy(c => (c.VigenteDesde, c.Versao)) is { } c
                ? r.ComDefinicao(r.Definicao with { Nivel = c.Nivel, Versao = c.Versao, VigenteDesde = c.VigenteDesde, Responsavel = c.Responsavel })
                : r)];

    /// Regras legais (clt.*) só podem ficar iguais ou mais rígidas que o padrão.
    public static bool PodeUsar(Regra padrao, NivelRegra nivel) =>
        !padrao.Definicao.Id.StartsWith("clt.", StringComparison.Ordinal) || nivel <= padrao.Definicao.Nivel;

    public static DadosDaUnidade Configurar(
        DadosDaUnidade dados, string regraId, NivelRegra nivel, DateOnly vigenteDesde, DateTimeOffset agora, string? motivo = null)
    {
        var padrao = CatalogoRegras.Padrao().FirstOrDefault(r => r.Definicao.Id == regraId)
            ?? throw new EscalaException("Regra desconhecida.");
        if (!PodeUsar(padrao, nivel))
            throw new EscalaException("Regras da CLT não podem ficar mais brandas que o padrão legal.");

        var versao = dados.RegrasConfiguradas.Where(c => c.RegraId == regraId).Select(c => c.Versao).DefaultIfEmpty(padrao.Definicao.Versao).Max() + 1;
        return dados with
        {
            RegrasConfiguradas = [.. dados.RegrasConfiguradas, new RegraConfigurada(regraId, nivel, vigenteDesde, versao, dados.NomeGestor, agora, motivo)],
        };
    }
}
