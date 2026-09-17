using System.IO.Compression;
using System.Xml.Linq;
using Folguinha.Infrastructure.Planilhas;

namespace Folguinha.Infrastructure.Tests;

public class PlanilhaXlsxTests
{
    static readonly XNamespace S = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

    static byte[] Exemplo() => PlanilhaXlsx.Gerar(
    [
        new Aba("Carla Mendes")
        {
            Linhas =
            [
                [new("Data", EstiloCelula.Cabecalho), new("Entrada", EstiloCelula.Cabecalho), new("Total", EstiloCelula.Cabecalho)],
                [new("qui 01/10"), new("12:00", EstiloCelula.Tarde), new(7.33)],
                [new("sáb 03/10"), new("Folga", EstiloCelula.Folga), new(null)],
            ],
            Larguras = [14, 10, 8],
            Filtro = true,
            CongelarCabecalho = true,
        },
        new Aba("Consolidado <geral> & filtros: [out/26]") { Linhas = [[new("R&D \"teste\" <ok>")]] },
    ]);

    static Dictionary<string, XDocument> Abrir(byte[] xlsx)
    {
        using var zip = new ZipArchive(new MemoryStream(xlsx));
        return zip.Entries.ToDictionary(e => e.FullName, e => { using var s = e.Open(); return XDocument.Load(s); });
    }

    [Fact]
    public void Gera_um_pacote_com_as_partes_obrigatorias_e_xml_valido()
    {
        var partes = Abrir(Exemplo());

        Assert.Contains("[Content_Types].xml", partes.Keys);
        Assert.Contains("_rels/.rels", partes.Keys);
        Assert.Contains("xl/workbook.xml", partes.Keys);
        Assert.Contains("xl/_rels/workbook.xml.rels", partes.Keys);
        Assert.Contains("xl/styles.xml", partes.Keys);
        Assert.Contains("xl/worksheets/sheet1.xml", partes.Keys);
        Assert.Contains("xl/worksheets/sheet2.xml", partes.Keys);
    }

    [Fact]
    public void Nomes_de_aba_ficam_validos_para_o_excel()
    {
        var nomes = Abrir(Exemplo())["xl/workbook.xml"].Descendants(S + "sheet").Select(x => (string)x.Attribute("name")!).ToList();

        Assert.Equal("Carla Mendes", nomes[0]);
        Assert.True(nomes[1].Length <= 31);
        Assert.DoesNotContain(nomes[1], c => "[]:*?/\\".Contains(c));
    }

    [Fact]
    public void Nomes_de_aba_repetidos_viram_unicos()
    {
        var xlsx = PlanilhaXlsx.Gerar([new Aba("Ana"), new Aba("Ana"), new Aba("ana")]);

        var nomes = Abrir(xlsx)["xl/workbook.xml"].Descendants(S + "sheet").Select(x => ((string)x.Attribute("name")!).ToLowerInvariant());

        Assert.Equal(3, nomes.Distinct().Count());
    }

    [Fact]
    public void Escreve_texto_numero_e_celula_vazia()
    {
        var aba = Abrir(Exemplo())["xl/worksheets/sheet1.xml"];
        var linha2 = aba.Descendants(S + "row").ElementAt(1).Elements(S + "c").ToList();

        Assert.Equal("A2", (string)linha2[0].Attribute("r")!);
        Assert.Equal("inlineStr", (string)linha2[0].Attribute("t")!);
        Assert.Equal("qui 01/10", linha2[0].Descendants(S + "t").Single().Value);
        Assert.Equal("7.33", linha2[2].Element(S + "v")!.Value);
        Assert.Null(linha2[2].Attribute("t"));
        Assert.DoesNotContain(aba.Descendants(S + "row").ElementAt(2).Elements(S + "c"), c => (string)c.Attribute("r")! == "C3");
    }

    [Fact]
    public void Caracteres_especiais_sao_preservados()
    {
        var aba = Abrir(Exemplo())["xl/worksheets/sheet2.xml"];

        Assert.Equal("R&D \"teste\" <ok>", aba.Descendants(S + "t").Single().Value);
    }

    [Fact]
    public void Filtro_congelamento_e_larguras()
    {
        var partes = Abrir(Exemplo());
        var aba = partes["xl/worksheets/sheet1.xml"];

        Assert.Equal("A1:C3", (string)aba.Descendants(S + "autoFilter").Single().Attribute("ref")!);
        Assert.Equal("frozen", (string)aba.Descendants(S + "pane").Single().Attribute("state")!);
        Assert.Equal(3, aba.Descendants(S + "col").Count());
        Assert.Contains(partes["xl/workbook.xml"].Descendants(S + "definedName"),
            d => (string)d.Attribute("name")! == "_xlnm._FilterDatabase");
    }

    [Fact]
    public void Estilos_referenciados_existem()
    {
        var partes = Abrir(Exemplo());
        var estilos = partes["xl/styles.xml"].Descendants(S + "cellXfs").Single().Elements(S + "xf").Count();

        var usados = partes["xl/worksheets/sheet1.xml"].Descendants(S + "c")
            .Select(c => (int?)c.Attribute("s") ?? 0);

        Assert.All(usados, s => Assert.InRange(s, 0, estilos - 1));
        Assert.Equal(Enum.GetValues<EstiloCelula>().Length, estilos);
    }
}
