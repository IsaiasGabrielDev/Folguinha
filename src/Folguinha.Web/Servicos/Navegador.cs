using Folguinha.Application;
using Microsoft.JSInterop;

namespace Folguinha.Web.Servicos;

/// Ponte para wwwroot/js/folguinha.js: IndexedDB, persistência e entrega de arquivos.
public sealed class Navegador(IJSRuntime js) : IArmazenamento, IAsyncDisposable
{
    Task<IJSObjectReference>? _modulo;

    Task<IJSObjectReference> Modulo() =>
        _modulo ??= js.InvokeAsync<IJSObjectReference>("import", "./js/folguinha.js").AsTask();

    public async ValueTask<string?> Ler(string chave) => await (await Modulo()).InvokeAsync<string?>("ler", chave);

    public async ValueTask Gravar(string chave, string valor) => await (await Modulo()).InvokeVoidAsync("gravar", chave, valor);

    public async ValueTask Remover(string chave) => await (await Modulo()).InvokeVoidAsync("remover", chave);

    /// "persistente", "temporario" ou "indisponivel".
    public async ValueTask<string> PedirPersistencia() => await (await Modulo()).InvokeAsync<string>("pedirPersistencia");

    public async ValueTask<int> EspacoUsadoKb() => await (await Modulo()).InvokeAsync<int>("espacoUsado");

    public async ValueTask<bool> Instalado() => await (await Modulo()).InvokeAsync<bool>("instalado");

    /// "compartilhado", "baixado" ou "cancelado".
    public async ValueTask<string> EntregarArquivo(string nome, string tipo, byte[] conteudo)
    {
        using var stream = new DotNetStreamReference(new MemoryStream(conteudo));
        return await (await Modulo()).InvokeAsync<string>("entregarArquivo", nome, tipo, stream);
    }

    public const string TipoXlsx = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    public async ValueTask DisposeAsync()
    {
        if (_modulo is { IsCompletedSuccessfully: true }) await (await _modulo).DisposeAsync();
    }
}
