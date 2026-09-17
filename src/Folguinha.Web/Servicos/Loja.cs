using Folguinha.Application;

namespace Folguinha.Web.Servicos;

/// Estado do app no navegador: carrega uma vez, salva a cada alteração e avisa as telas.
public sealed class Loja(RepositorioDados repositorio)
{
    public DadosDaUnidade Dados { get; private set; } = new();
    public bool Carregado { get; private set; }
    /// Mensagem curta para o usuário (sucesso ou erro), mostrada pelo layout.
    public (string Texto, bool Erro)? Mensagem { get; private set; }

    public event Action? Mudou;

    public static DateOnly Hoje => DateOnly.FromDateTime(DateTime.Now);

    public async Task Carregar()
    {
        if (Carregado) return;
        Dados = await repositorio.Carregar();
        Carregado = true;
        Mudou?.Invoke();
    }

    /// Aplica uma alteração e salva. Erros de regra de negócio viram mensagem; devolve se deu certo.
    public async Task<bool> Alterar(Func<DadosDaUnidade, DadosDaUnidade> alteracao, string? sucesso = null)
    {
        try
        {
            Dados = alteracao(Dados);
            await repositorio.Salvar(Dados);
            Avisar(sucesso);
            return true;
        }
        catch (EscalaException e)
        {
            Avisar(e.Message, erro: true);
            return false;
        }
    }

    public async Task Substituir(DadosDaUnidade dados, string? sucesso = null)
    {
        Dados = dados;
        await repositorio.Salvar(Dados);
        Avisar(sucesso);
    }

    public async Task Apagar()
    {
        await repositorio.Apagar();
        Dados = new();
        Avisar("Dados apagados deste aparelho.");
    }

    public void Avisar(string? texto, bool erro = false)
    {
        Mensagem = texto is null ? null : (texto, erro);
        Mudou?.Invoke();
    }

    public void LimparMensagem()
    {
        Mensagem = null;
        Mudou?.Invoke();
    }
}
