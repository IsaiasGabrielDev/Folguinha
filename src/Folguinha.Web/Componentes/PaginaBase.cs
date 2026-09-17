using Folguinha.Application;
using Folguinha.Web.Servicos;
using Microsoft.AspNetCore.Components;

namespace Folguinha.Web.Componentes;

/// Base das telas: acesso aos dados e redesenho quando a loja muda.
public abstract class PaginaBase : ComponentBase, IDisposable
{
    [Inject] protected Loja Loja { get; set; } = default!;
    [Inject] protected NavigationManager Nav { get; set; } = default!;

    protected DadosDaUnidade Dados => Loja.Dados;

    protected override void OnInitialized() => Loja.Mudou += Redesenhar;

    void Redesenhar() => InvokeAsync(StateHasChanged);

    /// Dá um quadro para a tela mostrar "carregando" antes de um cálculo pesado.
    protected static Task Respirar() => Task.Delay(30);

    public virtual void Dispose()
    {
        Loja.Mudou -= Redesenhar;
        GC.SuppressFinalize(this);
    }
}
