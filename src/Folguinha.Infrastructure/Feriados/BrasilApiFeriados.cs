using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Folguinha.Application;
using Folguinha.Domain;

namespace Folguinha.Infrastructure.Feriados;

/// Feriados nacionais da BrasilAPI (https://brasilapi.com.br/docs#tag/Feriados-Nacionais), sem chave, com CORS.
public sealed class BrasilApiFeriados(HttpClient http) : IFeriadoProvider
{
    public const string Endereco = "https://brasilapi.com.br/api/feriados/v1/";

    public async Task<IReadOnlyList<Feriado>> Buscar(int ano, CancellationToken cancelamento = default)
    {
        var itens = await http.GetFromJsonAsync($"{Endereco}{ano}", FeriadosJson.Default.ItemBrasilApiArray, cancelamento) ?? [];
        return [.. itens.Select(Converter)];
    }

    /// Carnaval vem como "national", mas por lei é ponto facultativo: entra desligado.
    static Feriado Converter(ItemBrasilApi item)
    {
        var data = DateOnly.ParseExact(item.Date, "yyyy-MM-dd");
        var facultativo = item.Name.Contains("Carnaval", StringComparison.OrdinalIgnoreCase)
            || item.Type.Equals("optional", StringComparison.OrdinalIgnoreCase);
        return new Feriado(data, item.Name,
            facultativo ? AbrangenciaFeriado.PontoFacultativo : AbrangenciaFeriado.Nacional,
            Considerado: !facultativo,
            Origem: OrigemFeriado.Api);
    }
}

internal sealed record ItemBrasilApi(
    [property: JsonPropertyName("date")] string Date,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("type")] string Type);

[JsonSerializable(typeof(ItemBrasilApi[]))]
internal sealed partial class FeriadosJson : JsonSerializerContext;
