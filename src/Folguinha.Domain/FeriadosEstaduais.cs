namespace Folguinha.Domain;

/// Feriado estadual de data fixa. `Consenso = false` entra **desligado**: as fontes públicas
/// divergem entre si e vários desses dias são ponto facultativo, não feriado. Quem confirma
/// é o gerente com o DP — igual o Carnaval, que também chega desligado.
public sealed record FeriadoEstadual(int Dia, int Mes, string Nome, bool Consenso = true);

/// Tabela embutida em vez de API: feriado estadual muda por lei, quase nunca, e o app precisa
/// funcionar offline. As APIs abertas ou cobrem 5 dos 27 estados e pararam em 2020, ou pedem token.
/// Datas móveis (N. S. da Penha no ES, Carnaval no RJ) ficam de fora — o Carnaval nacional já cobre.
public static class FeriadosEstaduais
{
    public static readonly IReadOnlyList<(string Uf, string Nome)> Estados =
    [
        ("AC", "Acre"), ("AL", "Alagoas"), ("AP", "Amapá"), ("AM", "Amazonas"), ("BA", "Bahia"),
        ("CE", "Ceará"), ("DF", "Distrito Federal"), ("ES", "Espírito Santo"), ("GO", "Goiás"),
        ("MA", "Maranhão"), ("MT", "Mato Grosso"), ("MS", "Mato Grosso do Sul"), ("MG", "Minas Gerais"),
        ("PA", "Pará"), ("PB", "Paraíba"), ("PR", "Paraná"), ("PE", "Pernambuco"), ("PI", "Piauí"),
        ("RJ", "Rio de Janeiro"), ("RN", "Rio Grande do Norte"), ("RS", "Rio Grande do Sul"),
        ("RO", "Rondônia"), ("RR", "Roraima"), ("SC", "Santa Catarina"), ("SP", "São Paulo"),
        ("SE", "Sergipe"), ("TO", "Tocantins"),
    ];

    public static IReadOnlyList<FeriadoEstadual> De(string? uf) =>
        uf is not null && PorUf.TryGetValue(uf, out var fs) ? fs : [];

    public static string NomeDoEstado(string? uf) =>
        Estados.FirstOrDefault(e => e.Uf == uf).Nome ?? uf ?? "";

    static readonly Dictionary<string, FeriadoEstadual[]> PorUf = new()
    {
        ["AC"] = [new(23, 1, "Dia do Evangélico"), new(15, 6, "Aniversário do Acre"),
                  new(5, 9, "Dia da Amazônia"), new(17, 11, "Assinatura do Tratado de Petrópolis"),
                  new(8, 3, "Dia Internacional da Mulher", false)],
        ["AL"] = [new(24, 6, "São João"), new(29, 6, "São Pedro"), new(16, 9, "Emancipação política de Alagoas")],
        ["AP"] = [new(19, 3, "Dia de São José"), new(13, 9, "Criação do Território Federal", false),
                  new(25, 7, "São Tiago", false), new(5, 10, "Criação do estado", false)],
        ["AM"] = [new(5, 9, "Elevação do Amazonas a província"), new(8, 12, "Nossa Senhora da Conceição")],
        ["BA"] = [new(2, 7, "Independência da Bahia")],
        ["CE"] = [new(19, 3, "Dia de São José"), new(25, 3, "Abolição da escravidão no Ceará")],
        ["DF"] = [new(21, 4, "Fundação de Brasília"), new(30, 11, "Dia do Evangélico")],
        ["ES"] = [new(8, 9, "Nossa Senhora da Vitória", false), new(28, 10, "Dia do Servidor Público", false),
                  new(30, 11, "Dia do Evangélico", false)],
        ["GO"] = [new(26, 7, "Sant'Ana"), new(24, 10, "Pedra fundamental de Goiânia"),
                  new(24, 5, "Nossa Senhora Auxiliadora", false)],
        ["MA"] = [new(28, 7, "Adesão do Maranhão à Independência"), new(8, 12, "Nossa Senhora da Conceição", false)],
        ["MT"] = [],
        ["MS"] = [new(11, 10, "Criação do estado")],
        ["MG"] = [new(21, 4, "Data Magna (execução de Tiradentes)")],
        ["PA"] = [new(15, 8, "Adesão do Grão-Pará à Independência"), new(8, 12, "Nossa Senhora da Conceição", false)],
        ["PB"] = [new(5, 8, "Fundação do Estado")],
        ["PR"] = [new(19, 12, "Emancipação política do Paraná", false), new(15, 11, "Nossa Senhora do Rocio", false)],
        ["PE"] = [new(6, 3, "Revolução Pernambucana de 1817"), new(24, 6, "São João")],
        ["PI"] = [new(19, 10, "Dia do Piauí"), new(13, 3, "Batalha do Jenipapo", false)],
        ["RJ"] = [new(23, 4, "Dia de São Jorge")],
        ["RN"] = [new(3, 10, "Mártires de Cunhaú e Uruaçu"), new(7, 8, "Dia do Rio Grande do Norte", false),
                  new(29, 11, "Dia do Evangélico", false)],
        ["RS"] = [new(20, 9, "Revolução Farroupilha")],
        ["RO"] = [new(4, 1, "Criação do estado"), new(18, 6, "Dia do Evangélico")],
        ["RR"] = [new(5, 10, "Criação do estado")],
        ["SC"] = [new(11, 8, "Dia de Santa Catarina"), new(25, 11, "Santa Catarina de Alexandria")],
        ["SP"] = [new(9, 7, "Revolução Constitucionalista de 1932")],
        ["SE"] = [new(8, 7, "Emancipação política de Sergipe")],
        ["TO"] = [new(5, 10, "Criação do estado"), new(8, 9, "Nossa Senhora da Natividade"),
                  new(18, 3, "Autonomia do Estado", false)],
    };
}
