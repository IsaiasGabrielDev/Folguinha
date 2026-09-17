using Folguinha.Domain.Regras;
using static Folguinha.Domain.Tests.Dados;

namespace Folguinha.Domain.Tests;

public class AfastamentoTests
{
    readonly EscaladoDuranteAfastamento _regra = new();

    [Fact]
    public void Bloqueia_escala_durante_atestado_sem_expor_o_motivo()
    {
        var bruno = Func("Bruno");
        var atestado = new Ocorrencia(Guid.NewGuid(), bruno.Id, TipoOcorrencia.Atestado, D(14), D(15));

        var v = Assert.Single(_regra.Validar(Ctx([bruno], [Trab(bruno, D(15), "08:00", "16:20")], ocorrencias: [atestado])));

        Assert.Equal(Severidade.Bloqueio, v.Severidade);
        Assert.Equal("Bruno tem ausência aprovada em qui 15/10 e está na escala.", v.Mensagem);
        Assert.DoesNotContain("atestado", v.Mensagem, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Ignora_ocorrencia_fora_do_dia()
    {
        var f = Func();
        var ferias = new Ocorrencia(Guid.NewGuid(), f.Id, TipoOcorrencia.Ferias, D(20), D(29));

        Assert.Empty(_regra.Validar(Ctx([f], [Trab(f, D(15), "08:00", "16:20")], ocorrencias: [ferias])));
    }

    [Fact]
    public void Atraso_nao_e_afastamento()
    {
        var f = Func();
        var atraso = new Ocorrencia(Guid.NewGuid(), f.Id, TipoOcorrencia.Atraso, D(15), D(15));

        Assert.Empty(_regra.Validar(Ctx([f], [Trab(f, D(15), "08:00", "16:20")], ocorrencias: [atraso])));
    }
}

public class FeriadoTrabalhadoTests
{
    readonly FeriadoTrabalhado _regra = new();
    static readonly Feriado Aparecida = new(D(12), "Nossa Senhora Aparecida", AbrangenciaFeriado.Nacional);

    [Fact]
    public void Alerta_sobre_compensacao_do_feriado()
    {
        var carla = Func("Carla");

        var v = Assert.Single(_regra.Validar(Ctx([carla], [Trab(carla, D(12), "08:00", "16:20")], feriados: [Aparecida])));

        Assert.Equal(Severidade.Atencao, v.Severidade);
        Assert.Contains("Nossa Senhora Aparecida", v.Mensagem);
    }

    [Fact]
    public void Feriado_ignorado_e_dia_normal()
    {
        var f = Func();
        var ignorado = Aparecida with { Considerado = false };

        Assert.Empty(_regra.Validar(Ctx([f], [Trab(f, D(12), "08:00", "16:20")], feriados: [ignorado])));
    }

    [Fact]
    public void Escala_12x36_ja_compensa_feriados()
    {
        var f = Func(regime: Regime.DozePorTrintaESeis);

        Assert.Empty(_regra.Validar(Ctx([f], [Trab(f, D(12), "07:00", "19:00")], feriados: [Aparecida])));
    }
}

public class CoberturaTests
{
    readonly CoberturaMinima _regra = new();
    static readonly Turno Tarde = new(Guid.NewGuid(), "Tarde", new(14, 0), new(22, 20), TimeSpan.FromHours(1));
    static readonly Funcao Caixa = new(Guid.NewGuid(), "Caixa");

    [Fact]
    public void Aponta_falta_de_pessoas_na_funcao()
    {
        var gabi = Func("Gabi");
        var demanda = new Demanda(Tarde.Id, Minimo: 2, Ideal: 2, FuncaoId: Caixa.Id, DiaSemana: DayOfWeek.Saturday);

        var v = Assert.Single(_regra.Validar(Ctx([gabi],
            [Trab(gabi, D(17), "14:00", "22:20", turno: Tarde.Id, funcao: Caixa.Id)],
            inicio: D(17), fim: D(17), demandas: [demanda], turnos: [Tarde], funcoes: [Caixa])));

        Assert.Equal(Severidade.Critico, v.Severidade);
        Assert.Equal("sáb 17/10 · Tarde: falta 1 pessoa na função Caixa.", v.Mensagem);
        Assert.Null(v.FuncionarioId);
    }

    [Fact]
    public void Demanda_por_data_substitui_a_do_dia_da_semana()
    {
        var f = Func();
        var sabado = new Demanda(Tarde.Id, Minimo: 3, Ideal: 3, DiaSemana: DayOfWeek.Saturday);
        var dia17 = new Demanda(Tarde.Id, Minimo: 1, Ideal: 1, Data: D(17));

        Assert.Empty(_regra.Validar(Ctx([f],
            [Trab(f, D(17), "14:00", "22:20", turno: Tarde.Id)],
            inicio: D(17), fim: D(17), demandas: [sabado, dia17], turnos: [Tarde])));
    }

    [Fact]
    public void Conta_todos_os_dias_do_periodo()
    {
        var f = Func();
        var sabado = new Demanda(Tarde.Id, Minimo: 1, Ideal: 1, DiaSemana: DayOfWeek.Saturday);

        var vs = _regra.Validar(Ctx([f], [], demandas: [sabado], turnos: [Tarde])).ToList();

        Assert.Equal(5, vs.Count); // 5 sábados em outubro/2026
    }

    [Fact]
    public void Mensagem_no_plural_sem_funcao()
    {
        var sabado = new Demanda(Tarde.Id, Minimo: 2, Ideal: 3, DiaSemana: DayOfWeek.Saturday);

        var v = Assert.Single(_regra.Validar(Ctx([], [], inicio: D(17), fim: D(17), demandas: [sabado], turnos: [Tarde])));

        Assert.Equal("sáb 17/10 · Tarde: faltam 2 pessoas.", v.Mensagem);
    }
}

public class PreferenciaTests
{
    readonly PreferenciaDoFuncionario _regra = new();

    [Fact]
    public void Informa_quando_a_preferencia_nao_foi_atendida()
    {
        var f = Func("Carla") with { Disponibilidades = [new Disponibilidade(DayOfWeek.Saturday, new(8, 0), new(14, 0), TipoRestricao.Preferencia)] };

        var v = Assert.Single(_regra.Validar(Ctx([f], [Trab(f, D(17), "13:40", "22:00")])));

        Assert.Equal(Severidade.Informativo, v.Severidade);
        Assert.Equal("Carla prefere outro horário em sáb 17/10 (13:40–22:00).", v.Mensagem);
    }

    [Fact]
    public void Nada_a_informar_quando_atendida()
    {
        var f = Func() with { Disponibilidades = [new Disponibilidade(DayOfWeek.Saturday, new(8, 0), new(14, 0), TipoRestricao.Preferencia)] };

        Assert.Empty(_regra.Validar(Ctx([f], [Trab(f, D(17), "08:00", "13:00", 0)])));
    }
}

public class ValidadorTests
{
    [Fact]
    public void Nivel_configurado_define_a_severidade()
    {
        var f = Func();
        var regra = new Interjornada { Definicao = Interjornada.Padrao with { Nivel = NivelRegra.Alerta } };
        var ctx = Ctx([f], [Trab(f, D(16), "14:40", "23:00"), Trab(f, D(17), "08:00", "16:20")]);

        Assert.Equal(Severidade.Atencao, Assert.Single(Validador.Validar(ctx, [regra])).Severidade);
    }

    [Fact]
    public void Regra_fora_de_vigencia_nao_e_aplicada()
    {
        var f = Func();
        var antiga = new Interjornada { Definicao = Interjornada.Padrao with { VigenteAte = D(30, 9) } };
        var ctx = Ctx([f], [Trab(f, D(16), "14:40", "23:00"), Trab(f, D(17), "08:00", "16:20")]);

        Assert.Empty(Validador.Validar(ctx, [antiga]));
    }

    [Fact]
    public void Catalogo_padrao_traz_fundamento_em_todas_as_regras()
    {
        var regras = CatalogoRegras.Padrao();

        Assert.All(regras, r => Assert.False(string.IsNullOrWhiteSpace(r.Definicao.Fundamento)));
        Assert.Equal(regras.Count, regras.Select(r => r.Definicao.Id).Distinct().Count());
    }

    [Fact]
    public void Escala_correta_de_6x1_nao_gera_violacoes()
    {
        var f = Func();
        var als = Semana(f, D(5), "08:00", "16:20").Append(Folga(f, D(11))).ToArray();

        Assert.Empty(Validador.Validar(Ctx([f], als, inicio: D(5), fim: D(11)), CatalogoRegras.Padrao()));
    }
}
