using Folguinha.Domain.Regras;
using static Folguinha.Domain.Tests.Dados;

namespace Folguinha.Domain.Tests;

public class HoraNoturnaTests
{
    [Fact]
    public void Sete_horas_noturnas_valem_oito()
    {
        var f = Func();

        Assert.Equal(TimeSpan.FromHours(8), Trab(f, D(1), "22:00", "05:00", intervaloMin: 0).HorasComputadas);
    }

    [Fact]
    public void Jornada_diurna_desconta_so_o_intervalo()
    {
        var f = Func();

        Assert.Equal(new TimeSpan(7, 20, 0), Trab(f, D(1), "08:00", "16:20").HorasComputadas);
    }
}

public class JornadaDiariaTests
{
    readonly JornadaDiaria _regra = new();

    [Fact]
    public void Bloqueia_mais_de_10h_mesmo_com_hora_extra()
    {
        var f = Func("Bruno", horaExtra: true);

        var v = Assert.Single(_regra.Validar(Ctx([f], [Trab(f, D(5), "08:00", "20:00")])));

        Assert.Equal(Severidade.Bloqueio, v.Severidade);
        Assert.Contains("11h", v.Mensagem);
        Assert.Contains("10h", v.Mensagem);
    }

    [Fact]
    public void Bloqueia_hora_extra_de_quem_nao_tem_permissao()
    {
        var f = Func("Bruno");

        var v = Assert.Single(_regra.Validar(Ctx([f], [Trab(f, D(5), "08:00", "18:00")])));

        Assert.Contains("hora extra", v.Mensagem);
    }

    [Fact]
    public void Aceita_hora_extra_permitida_ate_10h()
    {
        var f = Func(horaExtra: true);

        Assert.Empty(_regra.Validar(Ctx([f], [Trab(f, D(5), "08:00", "18:00")])));
    }

    [Fact]
    public void Aceita_compensacao_do_5x2_de_44h()
    {
        var f = Func(regime: Regime.CincoPorDois);

        Assert.Empty(_regra.Validar(Ctx([f], [Trab(f, D(5), "08:00", "17:48")])));
    }

    [Fact]
    public void Aceita_12h_no_12x36()
    {
        var f = Func(regime: Regime.DozePorTrintaESeis, cargaSemanal: 42);

        Assert.Empty(_regra.Validar(Ctx([f], [Trab(f, D(5), "07:00", "20:00")])));
    }
}

public class JornadaSemanalTests
{
    readonly JornadaSemanal _regra = new();

    [Fact]
    public void Aceita_seis_dias_de_7h20()
    {
        var f = Func();

        Assert.Empty(_regra.Validar(Ctx([f], Semana(f, D(5), "08:00", "16:20"))));
    }

    [Fact]
    public void Aponta_semana_acima_da_carga_sem_hora_extra()
    {
        var f = Func("Elisa");

        var v = Assert.Single(_regra.Validar(Ctx([f], Semana(f, D(5), "08:00", "17:00"))));

        Assert.Contains("48h", v.Mensagem);
        Assert.Contains("44h", v.Mensagem);
        Assert.Equal(D(11), v.Data);
    }

    [Fact]
    public void Aceita_hora_extra_semanal_dentro_do_limite()
    {
        var f = Func(horaExtra: true, limiteHoraExtraSemanal: 4);

        Assert.Empty(_regra.Validar(Ctx([f], Semana(f, D(5), "08:00", "17:00"))));
    }

    [Fact]
    public void Tempo_parcial_de_30h_nao_admite_hora_extra()
    {
        var f = Func(cargaSemanal: 30, contrato: TipoContrato.TempoParcial);
        var als = Enumerable.Range(0, 4).Select(i => Trab(f, D(5 + i), "08:00", "16:00", 0)).ToArray();

        var v = Assert.Single(_regra.Validar(Ctx([f], als)));
        Assert.Contains("30h", v.Mensagem);
    }

    [Fact]
    public void Tempo_parcial_de_24h_admite_ate_6h_extras()
    {
        var f = Func(cargaSemanal: 24, contrato: TipoContrato.TempoParcial);
        var als = Enumerable.Range(0, 5).Select(i => Trab(f, D(5 + i), "08:00", "14:00", 0)).ToArray();

        Assert.Empty(_regra.Validar(Ctx([f], als)));
    }

    [Fact]
    public void Tempo_parcial_de_24h_aponta_mais_de_30h()
    {
        var f = Func(cargaSemanal: 24, contrato: TipoContrato.TempoParcial);
        var als = Enumerable.Range(0, 5).Select(i => Trab(f, D(5 + i), "08:00", "14:15", 0)).ToArray();

        Assert.Single(_regra.Validar(Ctx([f], als)));
    }
}

public class IntrajornadaTests
{
    readonly Intrajornada _regra = new();

    [Theory]
    [InlineData("08:00", "16:30", 30)]   // 8h de trabalho, 30 min de intervalo
    [InlineData("08:00", "19:00", 180)]  // intervalo acima de 2h
    [InlineData("08:00", "13:00", 0)]    // 5h sem pausa
    public void Aponta_intervalo_fora_da_lei(string inicio, string fim, int intervalo)
    {
        var f = Func();

        Assert.Single(_regra.Validar(Ctx([f], [Trab(f, D(5), inicio, fim, intervalo)])));
    }

    [Theory]
    [InlineData("08:00", "17:00", 60)]
    [InlineData("08:00", "13:15", 15)]
    [InlineData("08:00", "12:00", 0)]
    public void Aceita_intervalo_correto(string inicio, string fim, int intervalo)
    {
        var f = Func();

        Assert.Empty(_regra.Validar(Ctx([f], [Trab(f, D(5), inicio, fim, intervalo)])));
    }
}

public class MenorTrabalhoNoturnoTests
{
    readonly MenorTrabalhoNoturno _regra = new();

    [Fact]
    public void Bloqueia_menor_depois_das_22h()
    {
        var f = Func("Gabi", menor: true);

        Assert.Single(_regra.Validar(Ctx([f], [Trab(f, D(5), "16:30", "22:30")])));
    }

    [Fact]
    public void Aceita_menor_ate_22h()
    {
        var f = Func(menor: true);

        Assert.Empty(_regra.Validar(Ctx([f], [Trab(f, D(5), "14:00", "22:00")])));
    }

    [Fact]
    public void Adulto_pode_trabalhar_a_noite()
    {
        var f = Func();

        Assert.Empty(_regra.Validar(Ctx([f], [Trab(f, D(5), "22:00", "05:00")])));
    }
}
