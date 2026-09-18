using Folguinha.Domain;

namespace Folguinha.Application;

public static class FolgasExtras
{
    /// Espalha a folga extra pela equipe: com "a cada 2 semanas", metade folga numa semana e metade
    /// na outra. Sem revezar, todo mundo folga junto e a loja fica vazia no fim de semana. A ordem
    /// da lista é preservada — o gerador usa o índice para alternar plantões no 12x36.
    public static DadosDaUnidade Revezar(DadosDaUnidade dados, FolgaExtraPeriodica modelo, Guid? ancora = null)
    {
        var grupos = Math.Max(1, modelo.ACadaSemanas);
        // quem serve de âncora fica na semana escolhida; o resto se espalha pelas seguintes,
        // intercalado por turno para nenhum turno ficar inteiro na mesma semana
        var semana = dados.Funcionarios
            .OrderBy(f => f.Id == ancora ? 0 : 1)
            .ThenBy(f => f.TurnoPrincipal)
            .ThenBy(f => f.Nome, StringComparer.Ordinal)
            .Select((f, i) => (f.Id, Semana: i % grupos))
            .ToDictionary(t => t.Id, t => t.Semana);

        return dados with
        {
            Funcionarios = [.. dados.Funcionarios.Select(f => f with
            {
                FolgaExtra = modelo with { APartirDe = modelo.APartirDe.AddDays(7 * semana[f.Id]) },
            })],
        };
    }

    /// Quem tem folga extra periódica caindo toda na mesma semana. Mais de uma pessoa aqui
    /// significa que elas folgam sempre juntas — e num fim de semana a loja fica vazia.
    public static IReadOnlyList<Funcionario> NaMesmaSemana(DadosDaUnidade dados)
    {
        var comExtra = dados.Funcionarios.Where(f => f.FolgaExtra is { ACadaSemanas: > 1 }).ToList();
        return comExtra.Count > 1 && comExtra.Select(f => f.FolgaExtra!.APartirDe).Distinct().Count() == 1
            ? comExtra
            : [];
    }
}
