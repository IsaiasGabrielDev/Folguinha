using Folguinha.Application;
using Folguinha.Domain;
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

    [Fact]
    public void Rascunho_so_diz_validado_quando_as_regras_ja_rodaram()
    {
        var rascunho = new Escala(Guid.NewGuid(), new(2026, 10, 1), new(2026, 10, 31)) with { Rascunho = [] };

        Assert.Equal("Rascunho · verificando…", Formatos.Estado(rascunho, null));
        Assert.Equal("Rascunho validado", Formatos.Estado(rascunho, true));
        Assert.Equal("Rascunho", Formatos.Estado(rascunho, false));
    }
}
