using System.Text.Json;
using System.Text.Json.Serialization;
using Folguinha.Domain;

namespace Folguinha.Application;

/// Tudo que o beta guarda no aparelho do gerente (uma unidade).
public sealed record DadosDaUnidade
{
    public const int FormatoAtual = 1;

    public int VersaoFormato { get; init; } = FormatoAtual;
    /// Quem gera, aprova e publica (autor das versões).
    public string NomeGestor { get; init; } = "Gerente";
    /// Leu o aviso de que o app não substitui DP/contador/jurídico (ESPEC §1).
    public bool AceitouAviso { get; init; }
    public Empresa? Empresa { get; init; }
    public string NomeUnidade { get; init; } = "";
    public IReadOnlyList<Funcionario> Funcionarios { get; init; } = [];
    public IReadOnlyList<Turno> Turnos { get; init; } = [];
    public IReadOnlyList<Funcao> Funcoes { get; init; } = [];
    public IReadOnlyList<Demanda> Demandas { get; init; } = [];
    public IReadOnlyList<PeriodoFuncionamento> Funcionamento { get; init; } = [];
    public IReadOnlyList<DateOnly> DatasFechadas { get; init; } = [];
    public IReadOnlyList<Feriado> Feriados { get; init; } = [];
    public IReadOnlyList<int> AnosDeFeriadosCarregados { get; init; } = [];
    public bool RodizioFeriados { get; init; } = true;
    public IReadOnlyList<Ocorrencia> Ocorrencias { get; init; } = [];
    public IReadOnlyList<RegraConfigurada> RegrasConfiguradas { get; init; } = [];
    public IReadOnlyList<Escala> Escalas { get; init; } = [];

    public Funcionario? Funcionario(Guid id) => Funcionarios.FirstOrDefault(f => f.Id == id);

    public Escala? Escala(Guid id) => Escalas.FirstOrDefault(e => e.Id == id);

    public Escala? EscalaDoDia(DateOnly dia) => Escalas.FirstOrDefault(e => e.Cobre(dia));

    public DadosDaUnidade ComEscala(Escala escala) => this with
    {
        Escalas = [.. Escalas.Where(e => e.Id != escala.Id), escala],
    };
}

/// Chave/valor local (IndexedDB no navegador).
public interface IArmazenamento
{
    ValueTask<string?> Ler(string chave);
    ValueTask Gravar(string chave, string valor);
    ValueTask Remover(string chave);
}

/// `chave` separa conjuntos de dados no mesmo aparelho (o diagnóstico usa uma chave própria).
public sealed class RepositorioDados(IArmazenamento armazenamento, string chave = "dados")
{
    readonly string Chave = chave;

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
// Propriedades só de leitura (calculadas) não vão para o arquivo.
[JsonSourceGenerationOptions(UseStringEnumConverter = true, IgnoreReadOnlyProperties = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(DadosDaUnidade))]
internal sealed partial class DadosJson : JsonSerializerContext;
