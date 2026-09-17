using System.IO.Compression;
using System.Xml.Linq;
using Folguinha.Application;
using Folguinha.Domain;
using Folguinha.Infrastructure.Planilhas;

namespace Folguinha.Infrastructure.Tests;

public class ExportacaoEscalaTests
{
    static readonly XNamespace S = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    static readonly DateTimeOffset Agora = new(2026, 9, 30, 18, 0, 0, TimeSpan.FromHours(-3));

    readonly DadosDaUnidade _dados;
    readonly Funcionario _bruno;

    public ExportacaoEscalaTests()
    {
        var manha = new Turno(Guid.NewGuid(), "Manhã", new(8, 0), new(16, 20), TimeSpan.FromHours(1));
        var tarde = new Turno(Guid.NewGuid(), "Tarde", new(13, 40), new(22, 0), TimeSpan.FromHours(1));
        var caixa = new Funcao(Guid.NewGuid(), "Caixa");
        string[] nomes = ["Ana", "Bruno", "Carla", "Diego"];
        var equipe = nomes.Select((n, i) => new Funcionario(Guid.NewGuid(), n, Regime.SeisPorUm, TimeSpan.FromHours(44))
        {
            Matricula = $"00{i + 1}",
            Setor = "Frente de loja",
            Cargo = "Operador",
            Funcoes = i < 2 ? [caixa.Id] : null,
        }).ToList();
        _bruno = equipe[1];
        _dados = new DadosDaUnidade
        {
            NomeGestor = "Isaias",
            NomeUnidade = "Centro",
            Empresa = new Empresa(Guid.NewGuid(), "Loja Exemplo", RamoAtividade.Comercio),
            Funcionarios = equipe,
            Turnos = [manha, tarde],
            Funcoes = [caixa],
            Feriados = [new Feriado(new(2026, 10, 12), "Nossa Senhora Aparecida", AbrangenciaFeriado.Nacional)],
            Demandas = [.. Enum.GetValues<DayOfWeek>().SelectMany(d => new[] { new Demanda(manha.Id, 1, 1, DiaSemana: d), new Demanda(tarde.Id, 1, 1, DiaSemana: d) })],
        };
    }

    (DadosDaUnidade Dados, Escala Escala) Publicada()
    {
        var (dados, escala, violacoes) = Escalas.Gerar(_dados, new(2026, 10, 1), new(2026, 10, 31));
        var justificativas = violacoes.Where(Escalas.PrecisaJustificativa).ToDictionary(Escalas.Chave, _ => "Feriado com escala combinada");
        var r = Escalas.Publicar(dados, escala.Id, "", justificativas, Agora);
        Assert.True(r.Publicado);
        return (r.Dados, r.Dados.Escala(escala.Id)!);
    }

    static Dictionary<string, List<string>> TextosPorAba(byte[] xlsx)
    {
        using var zip = new ZipArchive(new MemoryStream(xlsx));
        XDocument Ler(string nome) { using var s = zip.GetEntry(nome)!.Open(); return XDocument.Load(s); }
        var nomes = Ler("xl/workbook.xml").Descendants(S + "sheet").Select(x => (string)x.Attribute("name")!).ToList();
        return nomes.Select((n, i) => (n, textos: Ler($"xl/worksheets/sheet{i + 1}.xml").Descendants(S + "c")
                .Select(c => c.Descendants(S + "t").FirstOrDefault()?.Value ?? c.Element(S + "v")?.Value ?? "").ToList()))
            .ToDictionary(x => x.n, x => x.textos);
    }

    [Fact]
    public void Gera_calendario_lista_uma_aba_por_pessoa_e_validacao()
    {
        var (dados, escala) = Publicada();

        var abas = TextosPorAba(ExportacaoEscala.Gerar(dados, escala, new OpcoesExportacao()));

        Assert.Equal([ExportacaoEscala.AbaCalendario, ExportacaoEscala.AbaLista, "Ana", "Bruno", "Carla", "Diego", ExportacaoEscala.AbaValidacao], abas.Keys);
    }

    [Fact]
    public void Aba_individual_tem_os_campos_da_especificacao()
    {
        var (dados, escala) = Publicada();

        var bruno = TextosPorAba(ExportacaoEscala.Gerar(dados, escala, new OpcoesExportacao(Consolidada: false, Relatorio: false)))["Bruno"];

        foreach (var esperado in new[] { "Bruno", "002", "Operador · Caixa", "Loja Exemplo · Centro · Frente de loja", "01/10/2026 a 31/10/2026",
                     "Início intervalo", "Fim intervalo", "Total da semana", "Total no período", "Horas extras previstas",
                     "Domingos trabalhados", "Feriados trabalhados", "12/10/2026" })
            Assert.Contains(esperado, bruno);
        Assert.Contains(bruno, t => t.StartsWith("Versão 1, publicada em 30/09/2026 18:00 por Isaias", StringComparison.Ordinal));
        Assert.Contains(bruno, t => t.Contains("não comprova a jornada realizada", StringComparison.Ordinal));
    }

    [Fact]
    public void Intervalo_sugerido_fica_no_meio_da_jornada()
    {
        var (dados, escala) = Publicada();

        var ana = TextosPorAba(ExportacaoEscala.Gerar(dados, escala, new OpcoesExportacao(Consolidada: false, Relatorio: false)))["Ana"];

        // manhã 08:00–16:20 com 1h → 7h20 de trabalho → intervalo começa 3h45 depois (arredondado)
        var linha = ana.IndexOf("08:00");
        if (linha >= 0) Assert.Equal(["08:00", "11:45", "12:45", "16:20"], ana.Skip(linha).Take(4));
        else Assert.Contains("13:40", ana);
    }

    [Fact]
    public void Ausencia_nao_revela_o_motivo()
    {
        var (dados, escala) = Publicada();
        var dia = escala.Atual.First(a => a.FuncionarioId == _bruno.Id && a.Trabalha).Data;
        var atestado = new Ocorrencia(Guid.NewGuid(), _bruno.Id, TipoOcorrencia.Atestado, dia, dia, "consulta");
        var aprovado = Ocorrencias.Aprovar(dados, atestado, [], new Dictionary<string, string>(), Agora);
        var aprovada = aprovado.Dados;
        var comVersao = aprovada.Escala(escala.Id)!;
        if (comVersao.Rascunho is not null)
        {
            var j = Escalas.Validar(aprovada, comVersao).Where(Escalas.PrecisaJustificativa).ToDictionary(Escalas.Chave, _ => "ok");
            aprovada = Escalas.Publicar(aprovada, escala.Id, "Ausência", j, Agora).Dados;
        }

        var todas = TextosPorAba(ExportacaoEscala.Gerar(aprovada, aprovada.Escala(escala.Id)!, new OpcoesExportacao()))
            .SelectMany(a => a.Value).ToList();

        Assert.Contains(todas, t => t.StartsWith("Ausência aprovada", StringComparison.Ordinal));
        Assert.DoesNotContain(todas, t => t.Contains("Atestado", StringComparison.OrdinalIgnoreCase) || t.Contains("consulta", StringComparison.Ordinal));
    }

    [Fact]
    public void Relatorio_traz_regras_alertas_aceitos_e_responsaveis()
    {
        var (dados, escala) = Publicada();

        var validacao = TextosPorAba(ExportacaoEscala.Gerar(dados, escala, new OpcoesExportacao(false, false, true)))[ExportacaoEscala.AbaValidacao];

        foreach (var esperado in new[] { "Regras verificadas", "Descanso entre jornadas", "CLT art. 66", "Alertas aceitos com justificativa",
                     "Feriado com escala combinada", "Responsável pela aprovação", "Isaias", "30/09/2026 18:00" })
            Assert.Contains(esperado, validacao);
    }

    [Fact]
    public void Rascunho_e_identificado_como_nao_publicado()
    {
        var (dados, escala, _) = Escalas.Gerar(_dados, new(2026, 10, 1), new(2026, 10, 31));

        var abas = TextosPorAba(ExportacaoEscala.Gerar(dados, escala, new OpcoesExportacao()));

        Assert.Contains("Rascunho — ainda não publicada", abas["Ana"]);
        Assert.Contains("— (não publicada)", abas[ExportacaoEscala.AbaValidacao]);
        Assert.EndsWith("-rascunho.xlsx", ExportacaoEscala.NomeArquivo(dados, escala, null));
    }

    [Fact]
    public void Versao_antiga_pode_ser_exportada()
    {
        var (dados, escala) = Publicada();
        var ajustado = Escalas.Ajustar(dados, escala.Id, Alocacao.Folga(_bruno.Id, new(2026, 10, 20)));
        var j = ajustado.Violacoes.Where(Escalas.PrecisaJustificativa).ToDictionary(Escalas.Chave, _ => "ok");
        var v2 = Escalas.Publicar(ajustado.Dados, escala.Id, "Troca", j, Agora.AddDays(1)).Dados;
        var final = v2.Escala(escala.Id)!;

        var v1 = TextosPorAba(ExportacaoEscala.Gerar(v2, final, new OpcoesExportacao(false, false, true), final.Versoes[0]));

        Assert.Contains(v1[ExportacaoEscala.AbaValidacao], t => t.StartsWith("Versão 1", StringComparison.Ordinal));
        Assert.Equal("escala-loja-exemplo-2026-10-01-a-2026-10-31-v1.xlsx", ExportacaoEscala.NomeArquivo(v2, final, final.Versoes[0]));
    }

    [Fact]
    public void Exige_ao_menos_uma_planilha()
    {
        var (dados, escala) = Publicada();

        Assert.Throws<EscalaException>(() => ExportacaoEscala.Gerar(dados, escala, new OpcoesExportacao(false, false, false)));
    }
}
