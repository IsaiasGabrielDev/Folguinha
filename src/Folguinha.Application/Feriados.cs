using Folguinha.Domain;

namespace Folguinha.Application;

public interface IFeriadoProvider
{
    Task<IReadOnlyList<Feriado>> Buscar(int ano, CancellationToken cancelamento = default);
}

/// Feriados: nacionais vindos da API + manuais. O usuário liga/desliga e renomeia; data só muda nos manuais.
public static class Feriados
{
    /// Junta a lista nova da API com a atual, preservando o que o usuário decidiu.
    public static IReadOnlyList<Feriado> Mesclar(IReadOnlyList<Feriado> atuais, IReadOnlyList<Feriado> daApi, int ano)
    {
        var editados = atuais.Where(f => f.Origem == OrigemFeriado.Api && f.Data.Year == ano).ToDictionary(f => f.Data);
        var daApiAjustados = daApi.Select(f => editados.TryGetValue(f.Data, out var e)
            ? f with { Nome = e.Nome, Considerado = e.Considerado, Abrangencia = e.Abrangencia }
            : f);
        return [.. atuais.Where(f => f.Origem == OrigemFeriado.Manual || f.Data.Year != ano)
            .Concat(daApiAjustados)
            .OrderBy(f => f.Data)];
    }

    /// Nulo em `Erro` quando deu certo.
    public static async Task<(DadosDaUnidade Dados, string? Erro)> Atualizar(
        DadosDaUnidade dados, IFeriadoProvider provedor, int ano, CancellationToken cancelamento = default)
    {
        try
        {
            var daApi = await provedor.Buscar(ano, cancelamento);
            return (dados with
            {
                Feriados = Mesclar(dados.Feriados, daApi, ano),
                AnosDeFeriadosCarregados = [.. dados.AnosDeFeriadosCarregados.Append(ano).Distinct().Order()],
            }, null);
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException or FormatException or System.Text.Json.JsonException)
        {
            return (dados, "Não foi possível buscar os feriados agora. Verifique a internet ou cadastre manualmente.");
        }
    }

    /// Adiciona (original nulo) ou substitui um feriado.
    public static DadosDaUnidade Salvar(DadosDaUnidade dados, Feriado? original, Feriado novo)
    {
        if (original is { Origem: OrigemFeriado.Api } && novo.Data != original.Data)
            throw new EscalaException("A data de um feriado nacional não pode ser alterada; desligue-o e cadastre outro.");
        if (dados.Feriados.Any(f => f != original && f.Data == novo.Data))
            throw new EscalaException("Já existe um feriado nessa data.");
        return dados with { Feriados = [.. dados.Feriados.Where(f => f != original).Append(novo).OrderBy(f => f.Data)] };
    }

    public static DadosDaUnidade Remover(DadosDaUnidade dados, Feriado feriado) =>
        feriado.Origem == OrigemFeriado.Api
            ? throw new EscalaException("Feriados nacionais não são apagados; desligue-o para tratá-lo como dia normal.")
            : dados with { Feriados = [.. dados.Feriados.Where(f => f != feriado)] };
}
