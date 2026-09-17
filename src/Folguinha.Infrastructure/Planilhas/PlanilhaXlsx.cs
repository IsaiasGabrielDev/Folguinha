using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace Folguinha.Infrastructure.Planilhas;

/// A ordem define o índice no styles.xml.
public enum EstiloCelula { Normal, Cabecalho, Negrito, Manha, Tarde, Noite, Folga, Ocorrencia, Alerta }

/// `Valor`: texto, número ou nulo (célula vazia).
public readonly record struct Celula(object? Valor, EstiloCelula Estilo = EstiloCelula.Normal);

public sealed class Aba(string nome)
{
    public string Nome { get; } = nome;
    public IReadOnlyList<IReadOnlyList<Celula>> Linhas { get; init; } = [];
    public IReadOnlyList<double>? Larguras { get; init; }
    /// Filtro automático na primeira linha.
    public bool Filtro { get; init; }
    public bool CongelarCabecalho { get; init; }
}

/// Gera .xlsx (SpreadsheetML) sem dependências externas, para rodar leve no navegador.
public static class PlanilhaXlsx
{
    const string Ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    const string NsRel = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

    public static byte[] Gerar(IReadOnlyList<Aba> abas)
    {
        var nomes = NomesUnicos(abas);
        using var memoria = new MemoryStream();
        using (var zip = new ZipArchive(memoria, ZipArchiveMode.Create, leaveOpen: true))
        {
            Parte(zip, "[Content_Types].xml", w =>
            {
                w.WriteStartElement("Types", "http://schemas.openxmlformats.org/package/2006/content-types");
                Default(w, "rels", "application/vnd.openxmlformats-package.relationships+xml");
                Default(w, "xml", "application/xml");
                Override(w, "/xl/workbook.xml", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml");
                Override(w, "/xl/styles.xml", "application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml");
                for (var i = 1; i <= abas.Count; i++)
                    Override(w, $"/xl/worksheets/sheet{i}.xml", "application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml");
            });

            Parte(zip, "_rels/.rels", w =>
            {
                w.WriteStartElement("Relationships", "http://schemas.openxmlformats.org/package/2006/relationships");
                Relacao(w, "rId1", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument", "xl/workbook.xml");
            });

            Parte(zip, "xl/workbook.xml", w =>
            {
                w.WriteStartElement("workbook", Ns);
                w.WriteAttributeString("xmlns", "r", null, NsRel);
                w.WriteStartElement("sheets", Ns);
                for (var i = 0; i < abas.Count; i++)
                {
                    w.WriteStartElement("sheet", Ns);
                    w.WriteAttributeString("name", nomes[i]);
                    w.WriteAttributeString("sheetId", (i + 1).ToString(CultureInfo.InvariantCulture));
                    w.WriteAttributeString("id", NsRel, $"rId{i + 1}");
                    w.WriteEndElement();
                }
                w.WriteEndElement();

                var comFiltro = abas.Select((a, i) => (a, i)).Where(x => x.a.Filtro && x.a.Linhas.Count > 0).ToList();
                if (comFiltro.Count > 0)
                {
                    w.WriteStartElement("definedNames", Ns);
                    foreach (var (a, i) in comFiltro)
                    {
                        w.WriteStartElement("definedName", Ns);
                        w.WriteAttributeString("name", "_xlnm._FilterDatabase");
                        w.WriteAttributeString("localSheetId", i.ToString(CultureInfo.InvariantCulture));
                        w.WriteAttributeString("hidden", "1");
                        w.WriteString($"'{nomes[i].Replace("'", "''")}'!{Intervalo(a, absoluto: true)}");
                        w.WriteEndElement();
                    }
                    w.WriteEndElement();
                }
            });

            Parte(zip, "xl/_rels/workbook.xml.rels", w =>
            {
                w.WriteStartElement("Relationships", "http://schemas.openxmlformats.org/package/2006/relationships");
                for (var i = 1; i <= abas.Count; i++)
                    Relacao(w, $"rId{i}", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet", $"worksheets/sheet{i}.xml");
                Relacao(w, $"rId{abas.Count + 1}", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles", "styles.xml");
            });

            Parte(zip, "xl/styles.xml", Estilos);

            for (var i = 0; i < abas.Count; i++)
            {
                var aba = abas[i];
                Parte(zip, $"xl/worksheets/sheet{i + 1}.xml", w => Planilha(w, aba));
            }
        }
        return memoria.ToArray();
    }

    static void Planilha(XmlWriter w, Aba aba)
    {
        w.WriteStartElement("worksheet", Ns);

        if (aba.CongelarCabecalho && aba.Linhas.Count > 0)
        {
            w.WriteStartElement("sheetViews", Ns);
            w.WriteStartElement("sheetView", Ns);
            w.WriteAttributeString("workbookViewId", "0");
            w.WriteStartElement("pane", Ns);
            w.WriteAttributeString("ySplit", "1");
            w.WriteAttributeString("topLeftCell", "A2");
            w.WriteAttributeString("activePane", "bottomLeft");
            w.WriteAttributeString("state", "frozen");
            w.WriteEndElement();
            w.WriteEndElement();
            w.WriteEndElement();
        }

        if (aba.Larguras is { Count: > 0 } larguras)
        {
            w.WriteStartElement("cols", Ns);
            for (var c = 0; c < larguras.Count; c++)
            {
                w.WriteStartElement("col", Ns);
                w.WriteAttributeString("min", (c + 1).ToString(CultureInfo.InvariantCulture));
                w.WriteAttributeString("max", (c + 1).ToString(CultureInfo.InvariantCulture));
                w.WriteAttributeString("width", larguras[c].ToString(CultureInfo.InvariantCulture));
                w.WriteAttributeString("customWidth", "1");
                w.WriteEndElement();
            }
            w.WriteEndElement();
        }

        w.WriteStartElement("sheetData", Ns);
        for (var l = 0; l < aba.Linhas.Count; l++)
        {
            w.WriteStartElement("row", Ns);
            w.WriteAttributeString("r", (l + 1).ToString(CultureInfo.InvariantCulture));
            for (var c = 0; c < aba.Linhas[l].Count; c++)
            {
                var celula = aba.Linhas[l][c];
                if (celula.Valor is null && celula.Estilo == EstiloCelula.Normal) continue;

                w.WriteStartElement("c", Ns);
                w.WriteAttributeString("r", $"{Coluna(c)}{l + 1}");
                if (celula.Estilo != EstiloCelula.Normal)
                    w.WriteAttributeString("s", ((int)celula.Estilo).ToString(CultureInfo.InvariantCulture));
                switch (celula.Valor)
                {
                    case null:
                        break;
                    case string texto:
                        w.WriteAttributeString("t", "inlineStr");
                        w.WriteStartElement("is", Ns);
                        w.WriteStartElement("t", Ns);
                        w.WriteAttributeString("xml", "space", null, "preserve");
                        w.WriteString(SemCaracteresInvalidos(texto));
                        w.WriteEndElement();
                        w.WriteEndElement();
                        break;
                    case IConvertible numero and (int or long or double or decimal or float):
                        w.WriteElementString("v", Ns, numero.ToString(CultureInfo.InvariantCulture));
                        break;
                    default:
                        throw new ArgumentException($"Tipo de célula não suportado: {celula.Valor.GetType().Name}");
                }
                w.WriteEndElement();
            }
            w.WriteEndElement();
        }
        w.WriteEndElement();

        if (aba.Filtro && aba.Linhas.Count > 0)
        {
            w.WriteStartElement("autoFilter", Ns);
            w.WriteAttributeString("ref", Intervalo(aba, absoluto: false));
            w.WriteEndElement();
        }
    }

    static void Estilos(XmlWriter w)
    {
        // fills 0 e 1 são reservados pelo Excel
        string[] cores = ["F4EEE6", "FFF1CC", "DDF3EE", "E4E4FA", "EFEAE3", "FCE1E4", "FDEBDB"];
        w.WriteStartElement("styleSheet", Ns);

        w.WriteStartElement("fonts", Ns);
        w.WriteAttributeString("count", "2");
        Fonte(w, negrito: false);
        Fonte(w, negrito: true);
        w.WriteEndElement();

        w.WriteStartElement("fills", Ns);
        w.WriteAttributeString("count", (cores.Length + 2).ToString(CultureInfo.InvariantCulture));
        Preenchimento(w, "none", null);
        Preenchimento(w, "gray125", null);
        foreach (var cor in cores) Preenchimento(w, "solid", cor);
        w.WriteEndElement();

        w.WriteStartElement("borders", Ns);
        w.WriteAttributeString("count", "1");
        w.WriteStartElement("border", Ns);
        foreach (var lado in new[] { "left", "right", "top", "bottom", "diagonal" }) w.WriteElementString(lado, Ns, "");
        w.WriteEndElement();
        w.WriteEndElement();

        w.WriteStartElement("cellStyleXfs", Ns);
        w.WriteAttributeString("count", "1");
        Formato(w, 0, 0);
        w.WriteEndElement();

        // mesma ordem de EstiloCelula: (fonte, fill)
        (int fonte, int fill)[] xfs = [(0, 0), (1, 2), (1, 0), (0, 3), (0, 4), (0, 5), (0, 6), (0, 7), (0, 8)];
        w.WriteStartElement("cellXfs", Ns);
        w.WriteAttributeString("count", xfs.Length.ToString(CultureInfo.InvariantCulture));
        foreach (var (fonte, fill) in xfs) Formato(w, fonte, fill, xfId: true);
        w.WriteEndElement();

        w.WriteStartElement("cellStyles", Ns);
        w.WriteAttributeString("count", "1");
        w.WriteStartElement("cellStyle", Ns);
        w.WriteAttributeString("name", "Normal");
        w.WriteAttributeString("xfId", "0");
        w.WriteAttributeString("builtinId", "0");
        w.WriteEndElement();
        w.WriteEndElement();

        static void Fonte(XmlWriter w, bool negrito)
        {
            w.WriteStartElement("font", Ns);
            if (negrito) w.WriteElementString("b", Ns, "");
            w.WriteStartElement("sz", Ns); w.WriteAttributeString("val", "11"); w.WriteEndElement();
            w.WriteStartElement("name", Ns); w.WriteAttributeString("val", "Calibri"); w.WriteEndElement();
            w.WriteEndElement();
        }

        static void Preenchimento(XmlWriter w, string padrao, string? cor)
        {
            w.WriteStartElement("fill", Ns);
            w.WriteStartElement("patternFill", Ns);
            w.WriteAttributeString("patternType", padrao);
            if (cor is not null)
            {
                w.WriteStartElement("fgColor", Ns); w.WriteAttributeString("rgb", "FF" + cor); w.WriteEndElement();
                w.WriteStartElement("bgColor", Ns); w.WriteAttributeString("indexed", "64"); w.WriteEndElement();
            }
            w.WriteEndElement();
            w.WriteEndElement();
        }

        static void Formato(XmlWriter w, int fonte, int fill, bool xfId = false)
        {
            w.WriteStartElement("xf", Ns);
            w.WriteAttributeString("numFmtId", "0");
            w.WriteAttributeString("fontId", fonte.ToString(CultureInfo.InvariantCulture));
            w.WriteAttributeString("fillId", fill.ToString(CultureInfo.InvariantCulture));
            w.WriteAttributeString("borderId", "0");
            if (xfId)
            {
                w.WriteAttributeString("xfId", "0");
                if (fonte != 0) w.WriteAttributeString("applyFont", "1");
                if (fill != 0) w.WriteAttributeString("applyFill", "1");
            }
            w.WriteEndElement();
        }
    }

    static void Parte(ZipArchive zip, string caminho, Action<XmlWriter> escrever)
    {
        using var stream = zip.CreateEntry(caminho, CompressionLevel.Optimal).Open();
        using var w = XmlWriter.Create(stream, new XmlWriterSettings { Encoding = new UTF8Encoding(false) });
        w.WriteStartDocument(standalone: true);
        escrever(w);
        w.WriteEndDocument();
    }

    static void Default(XmlWriter w, string extensao, string tipo)
    {
        w.WriteStartElement("Default", "http://schemas.openxmlformats.org/package/2006/content-types");
        w.WriteAttributeString("Extension", extensao);
        w.WriteAttributeString("ContentType", tipo);
        w.WriteEndElement();
    }

    static void Override(XmlWriter w, string parte, string tipo)
    {
        w.WriteStartElement("Override", "http://schemas.openxmlformats.org/package/2006/content-types");
        w.WriteAttributeString("PartName", parte);
        w.WriteAttributeString("ContentType", tipo);
        w.WriteEndElement();
    }

    static void Relacao(XmlWriter w, string id, string tipo, string alvo)
    {
        w.WriteStartElement("Relationship", "http://schemas.openxmlformats.org/package/2006/relationships");
        w.WriteAttributeString("Id", id);
        w.WriteAttributeString("Type", tipo);
        w.WriteAttributeString("Target", alvo);
        w.WriteEndElement();
    }

    static string Intervalo(Aba aba, bool absoluto)
    {
        var ultimaColuna = Coluna(Math.Max(1, aba.Linhas.Max(l => l.Count)) - 1);
        var ultimaLinha = aba.Linhas.Count;
        return absoluto ? $"$A$1:${ultimaColuna}${ultimaLinha}" : $"A1:{ultimaColuna}{ultimaLinha}";
    }

    static string Coluna(int indice)
    {
        var nome = "";
        for (var n = indice + 1; n > 0; n = (n - 1) / 26) nome = (char)('A' + (n - 1) % 26) + nome;
        return nome;
    }

    /// Excel: até 31 caracteres, sem []:*?/\ e sem repetir (ignorando maiúsculas).
    static List<string> NomesUnicos(IReadOnlyList<Aba> abas)
    {
        var usados = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var nomes = new List<string>();
        foreach (var aba in abas)
        {
            var baseNome = new string(aba.Nome.Where(c => !"[]:*?/\\".Contains(c)).ToArray()).Trim().Trim('\'');
            if (baseNome.Length == 0) baseNome = "Planilha";
            baseNome = baseNome[..Math.Min(31, baseNome.Length)];
            var nome = baseNome;
            for (var i = 2; !usados.Add(nome); i++)
            {
                var sufixo = $" ({i})";
                nome = baseNome[..Math.Min(baseNome.Length, 31 - sufixo.Length)] + sufixo;
            }
            nomes.Add(nome);
        }
        return nomes;
    }

    static string SemCaracteresInvalidos(string texto) =>
        texto.Any(c => !XmlConvert.IsXmlChar(c)) ? new string(texto.Where(XmlConvert.IsXmlChar).ToArray()) : texto;
}
