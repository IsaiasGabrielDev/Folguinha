using System.Text.Json;
using System.Text.Json.Serialization;
using Folguinha.Domain;

namespace Folguinha.Application;

/// Tudo que o beta guarda no aparelho do gerente (uma unidade).
public sealed record DadosDaUnidade
{
    public const int FormatoAtual = 1;

    public int VersaoFormato { get; init; } = FormatoAtual;
    public Empresa? Empresa { get; init; }
    public IReadOnlyList<Funcionario> Funcionarios { get; init; } = [];
    public IReadOnlyList<Turno> Turnos { get; init; } = [];
    public IReadOnlyList<Funcao> Funcoes { get; init; } = [];
    public IReadOnlyList<Demanda> Demandas { get; init; } = [];
    public IReadOnlyList<Feriado> Feriados { get; init; } = [];
    public IReadOnlyList<Ocorrencia> Ocorrencias { get; init; } = [];
    public IReadOnlyList<PeriodoFuncionamento> Funcionamento { get; init; } = [];
    public IReadOnlyList<Alocacao> Alocacoes { get; init; } = [];
}

/// Chave/valor local (IndexedDB no navegador).
public interface IArmazenamento
{
    ValueTask<string?> Ler(string chave);
    ValueTask Gravar(string chave, string valor);
    ValueTask Remover(string chave);
}

public sealed class RepositorioDados(IArmazenamento armazenamento)
{
    const string Chave = "dados";

    public async ValueTask<DadosDaUnidade> Carregar() =>
        await armazenamento.Ler(Chave) is { } json ? DeBackup(json) ?? new() : new();

    public ValueTask Salvar(DadosDaUnidade dados) => armazenamento.Gravar(Chave, ParaBackup(dados));

    public ValueTask Apagar() => armazenamento.Remover(Chave);

    public static string ParaBackup(DadosDaUnidade dados) => JsonSerializer.Serialize(dados, DadosJson.Default.DadosDaUnidade);

    /// Nulo se o conteúdo não for um backup do Folguinha em formato conhecido.
    public static DadosDaUnidade? DeBackup(string json)
    {
        try
        {
            var dados = JsonSerializer.Deserialize(json, DadosJson.Default.DadosDaUnidade);
            return dados is { VersaoFormato: DadosDaUnidade.FormatoAtual } ? dados : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}

// Gerado em compilação: o publish do Blazor remove o JSON por reflexão.
[JsonSourceGenerationOptions(UseStringEnumConverter = true)]
[JsonSerializable(typeof(DadosDaUnidade))]
internal sealed partial class DadosJson : JsonSerializerContext;
