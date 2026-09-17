using System.Net;
using System.Text;
using Folguinha.Domain;
using Folguinha.Infrastructure.Feriados;

namespace Folguinha.Infrastructure.Tests;

public class BrasilApiFeriadosTests
{
    sealed class RespostaFixa(HttpStatusCode status, string corpo) : HttpMessageHandler
    {
        public Uri? Pedido { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Pedido = request.RequestUri;
            return Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(corpo, Encoding.UTF8, "application/json") });
        }
    }

    const string Exemplo = """
        [{"date":"2026-01-01","name":"Confraternização mundial","type":"national","weekday":"quinta-feira"},
         {"date":"2026-02-16","name":"Carnaval","type":"national","weekday":"segunda-feira"},
         {"date":"2026-10-12","name":"Nossa Senhora Aparecida","type":"national","weekday":"segunda-feira"}]
        """;

    [Fact]
    public async Task Converte_os_feriados_da_api()
    {
        var resposta = new RespostaFixa(HttpStatusCode.OK, Exemplo);
        var api = new BrasilApiFeriados(new HttpClient(resposta));

        var feriados = await api.Buscar(2026);

        Assert.Equal("https://brasilapi.com.br/api/feriados/v1/2026", resposta.Pedido?.ToString());
        Assert.Equal(3, feriados.Count);
        var aparecida = feriados.Single(f => f.Data == new DateOnly(2026, 10, 12));
        Assert.Equal(new Feriado(new(2026, 10, 12), "Nossa Senhora Aparecida", AbrangenciaFeriado.Nacional, true, OrigemFeriado.Api), aparecida);
    }

    [Fact]
    public async Task Carnaval_entra_como_ponto_facultativo_desligado()
    {
        var api = new BrasilApiFeriados(new HttpClient(new RespostaFixa(HttpStatusCode.OK, Exemplo)));

        var carnaval = (await api.Buscar(2026)).Single(f => f.Nome == "Carnaval");

        Assert.Equal(AbrangenciaFeriado.PontoFacultativo, carnaval.Abrangencia);
        Assert.False(carnaval.Considerado);
    }

    [Fact]
    public async Task Erro_do_servidor_vira_excecao_de_rede()
    {
        var api = new BrasilApiFeriados(new HttpClient(new RespostaFixa(HttpStatusCode.InternalServerError, "{}")));

        await Assert.ThrowsAsync<HttpRequestException>(() => api.Buscar(2026));
    }
}
