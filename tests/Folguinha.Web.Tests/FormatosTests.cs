using Folguinha.Application;
using Folguinha.Domain;
using Folguinha.Domain.Regras;
using Folguinha.Web.Servicos;

namespace Folguinha.Web.Tests;

public class FormatosTests
{
    static Turno T(string nome) => new(Guid.NewGuid(), nome, new(8, 0), new(16, 0), TimeSpan.FromHours(1));

    [Fact]
    public void Turnos_com_a_mesma_inicial_ganham_siglas_diferentes()
    {
        List<Turno> turnos = [T("Manhã"), T("Meio-dia"), T("Tarde")];

        Assert.Equal("Ma", Formatos.Sigla(turnos, "Manhã"));
        Assert.Equal("Me", Formatos.Sigla(turnos, "Meio-dia"));
        Assert.Equal("T", Formatos.Sigla(turnos, "Tarde"));
    }

    static Escala Rascunho() =>
        new Escala(Guid.NewGuid(), new(2026, 10, 1), new(2026, 10, 31)) with { Rascunho = [] };

    [Fact]
    public void Rascunho_so_diz_validado_quando_as_regras_ja_rodaram()
    {
        Assert.Equal("Rascunho · verificando…", Formatos.Estado(Rascunho(), null));
        Assert.Equal("Rascunho validado", Formatos.Estado(Rascunho(), 0));
    }

    /// A palavra "validado" é a proposta de valor do app; não pode sair com pendência na tela.
    [Fact]
    public void Validado_nao_sai_com_pendencia_sobrando()
    {
        Assert.Equal("Rascunho · 5 pontos a revisar", Formatos.Estado(Rascunho(), 5));
        Assert.Equal("Rascunho · 1 ponto a revisar", Formatos.Estado(Rascunho(), 1));
        Assert.Equal("chip-ambar", Formatos.ClasseEstado(Rascunho(), 5));
        Assert.Equal("chip-verde", Formatos.ClasseEstado(Rascunho(), 0));
    }

    [Fact]
    public void Plural_concorda_com_o_numero()
    {
        Assert.Equal("1 função", Formatos.Plural(1, "função", "funções"));
        Assert.Equal("2 funções", Formatos.Plural(2, "função", "funções"));
        Assert.Equal("0 avisos", Formatos.Plural(0, "aviso", "avisos"));
        Assert.Equal("5 críticos", Formatos.Severidades(5, Severidade.Critico));
        Assert.Equal("1 crítico", Formatos.Severidades(1, Severidade.Critico));
    }
}
