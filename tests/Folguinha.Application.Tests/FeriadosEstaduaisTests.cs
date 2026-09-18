using Folguinha.Application;
using Folguinha.Domain;

namespace Folguinha.Application.Tests;

public class FeriadosEstaduaisTests
{
    static DadosDaUnidade ComNacionais(params (int dia, int mes, string nome)[] nacionais) => new()
    {
        Feriados = [.. nacionais.Select(n => new Feriado(new DateOnly(2026, n.mes, n.dia), n.nome,
            AbrangenciaFeriado.Nacional, Origem: OrigemFeriado.Api))],
    };

    [Fact]
    public void Traz_o_feriado_do_estado_para_cada_ano_pedido()
    {
        var d = Feriados.AplicarEstaduais(new DadosDaUnidade(), "SP", [2026, 2027]);

        Assert.Equal("SP", d.Uf);
        Assert.Equal(
            [new DateOnly(2026, 7, 9), new DateOnly(2027, 7, 9)],
            d.Feriados.Select(f => f.Data));
        Assert.All(d.Feriados, f => Assert.Equal(AbrangenciaFeriado.Estadual, f.Abrangencia));
        // manual para o gerente poder editar e apagar, ao contrário dos nacionais da API
        Assert.All(d.Feriados, f => Assert.Equal(OrigemFeriado.Manual, f.Origem));
    }

    [Fact]
    public void O_que_as_fontes_divergem_entra_desligado()
    {
        var d = Feriados.AplicarEstaduais(new DadosDaUnidade(), "PR", [2026]);

        // as duas fontes discordam em todo feriado do Paraná: nenhum vale sem o gerente confirmar
        Assert.NotEmpty(d.Feriados);
        Assert.All(d.Feriados, f => Assert.False(f.Considerado));
    }

    [Fact]
    public void Nao_pisa_em_data_que_ja_tem_feriado()
    {
        // 21/04 do DF é a Fundação de Brasília, mas Tiradentes já ocupa o dia
        var comTiradentes = ComNacionais((21, 4, "Tiradentes"));

        var d = Feriados.AplicarEstaduais(comTiradentes, "DF", [2026]);

        var em21 = Assert.Single(d.Feriados, f => f.Data == new DateOnly(2026, 4, 21));
        Assert.Equal("Tiradentes", em21.Nome);
        Assert.Contains(d.Feriados, f => f.Data == new DateOnly(2026, 11, 30)); // o outro entrou
    }

    [Fact]
    public void Chamar_duas_vezes_nao_duplica()
    {
        var uma = Feriados.AplicarEstaduais(new DadosDaUnidade(), "BA", [2026]);
        var duas = Feriados.AplicarEstaduais(uma, "BA", [2026]);

        Assert.Equal(uma.Feriados.Count, duas.Feriados.Count);
    }

    [Fact]
    public void Estado_sem_feriado_estadual_nao_muda_nada()
    {
        var d = Feriados.AplicarEstaduais(new DadosDaUnidade(), "MT", [2026]);

        Assert.Empty(d.Feriados);
    }

    [Fact]
    public void Todo_estado_da_lista_tem_data_valida()
    {
        foreach (var (uf, _) in FeriadosEstaduais.Estados)
            foreach (var f in FeriadosEstaduais.De(uf))
            {
                Assert.InRange(f.Mes, 1, 12);
                Assert.InRange(f.Dia, 1, DateTime.DaysInMonth(2026, f.Mes));
                Assert.False(string.IsNullOrWhiteSpace(f.Nome));
            }
    }
}
