using Folguinha.Domain.Regras;
using static Folguinha.Domain.Tests.Dados;

namespace Folguinha.Domain.Tests;

public class InterjornadaTests
{
    readonly Interjornada _regra = new();

    [Fact]
    public void Aponta_descanso_menor_que_11h_com_mensagem_explicativa()
    {
        var diego = Func("Diego");
        var ctx = Ctx([diego], [Trab(diego, D(16), "14:40", "23:00"), Trab(diego, D(17), "08:00", "16:20")]);

        var v = Assert.Single(_regra.Validar(ctx));

        Assert.Equal(Severidade.Bloqueio, v.Severidade);
        Assert.Equal(diego.Id, v.FuncionarioId);
        Assert.Equal(D(17), v.Data);
        Assert.Equal("Diego terminaria às 23:00 de sex 16/10 e começaria às 08:00 de sáb 17/10: descanso de 9h (mínimo 11h).", v.Mensagem);
        Assert.NotNull(v.Sugestao);
    }

    [Fact]
    public void Aceita_descanso_de_exatamente_11h()
    {
        var f = Func();
        var ctx = Ctx([f], [Trab(f, D(16), "13:00", "21:00"), Trab(f, D(17), "08:00", "16:00")]);

        Assert.Empty(_regra.Validar(ctx));
    }

    [Fact]
    public void Considera_turno_que_vira_a_meia_noite()
    {
        var f = Func();
        var ctx = Ctx([f], [Trab(f, D(16), "22:00", "05:00"), Trab(f, D(17), "14:00", "22:00")]);

        var v = Assert.Single(_regra.Validar(ctx));
        Assert.Contains("descanso de 9h", v.Mensagem);
    }

    [Fact]
    public void Nao_aponta_conflito_que_esta_todo_antes_do_periodo()
    {
        var f = Func();
        var ctx = Ctx([f], [Trab(f, D(30, 9), "14:40", "23:00"), Trab(f, D(1), "08:00", "16:20")], inicio: D(2));

        Assert.Empty(_regra.Validar(ctx));
    }
}

public class DiasConsecutivosTests
{
    readonly DiasConsecutivos _regra = new();

    [Fact]
    public void Aponta_o_setimo_dia_seguido_de_trabalho()
    {
        var f = Func("Ana");
        var dias = Enumerable.Range(1, 7).Select(d => Trab(f, D(d), "08:00", "15:20")).ToArray();

        var v = Assert.Single(_regra.Validar(Ctx([f], dias)));

        Assert.Equal(D(7), v.Data);
        Assert.Contains("7 dias seguidos", v.Mensagem);
    }

    [Fact]
    public void Aceita_seis_dias_seguidos()
    {
        var f = Func();

        Assert.Empty(_regra.Validar(Ctx([f], Semana(f, D(1), "08:00", "15:20"))));
    }

    [Fact]
    public void Folga_zera_a_contagem()
    {
        var f = Func();
        var als = Semana(f, D(1), "08:00", "15:20")
            .Append(Folga(f, D(7)))
            .Concat(Semana(f, D(8), "08:00", "15:20"))
            .ToArray();

        Assert.Empty(_regra.Validar(Ctx([f], als)));
    }
}

public class DomingoDeFolgaTests
{
    readonly DomingoDeFolga _regra = new();

    static Alocacao[] Domingos(Funcionario f, params int[] dias) =>
        dias.Select(d => Trab(f, D(d), "10:00", "18:00")).ToArray();

    [Fact]
    public void Comercio_exige_um_domingo_de_folga_a_cada_3_semanas()
    {
        var carla = Func("Carla");

        var v = Assert.Single(_regra.Validar(Ctx([carla], Domingos(carla, 4, 11, 18))));

        Assert.Equal(D(18), v.Data);
        Assert.Contains("3 domingos seguidos", v.Mensagem);
    }

    [Fact]
    public void Comercio_aceita_folga_em_um_dos_3_domingos()
    {
        var f = Func();
        var als = Domingos(f, 4, 18, 25).Append(Folga(f, D(11))).ToArray();

        Assert.Empty(_regra.Validar(Ctx([f], als)));
    }

    [Fact]
    public void Outras_atividades_aceitam_ate_6_domingos_seguidos()
    {
        var f = Func();

        Assert.Empty(_regra.Validar(Ctx([f], Domingos(f, 4, 11, 18, 25), empresa: Industria)));
    }

    [Fact]
    public void Outras_atividades_apontam_o_setimo_domingo_seguido()
    {
        var f = Func();
        var als = Domingos(f, 4, 11, 18, 25)
            .Concat([Trab(f, D(1, 11), "10:00", "18:00"), Trab(f, D(8, 11), "10:00", "18:00"), Trab(f, D(15, 11), "10:00", "18:00")])
            .ToArray();

        var v = Assert.Single(_regra.Validar(Ctx([f], als, fim: D(30, 11), empresa: Industria)));
        Assert.Equal(D(15, 11), v.Data);
    }

    [Fact]
    public void Escala_12x36_nao_entra_na_regra()
    {
        var f = Func(regime: Regime.DozePorTrintaESeis);

        Assert.Empty(_regra.Validar(Ctx([f], Domingos(f, 4, 11, 18))));
    }
}
