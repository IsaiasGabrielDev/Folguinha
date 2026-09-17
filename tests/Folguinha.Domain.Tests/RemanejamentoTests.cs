using Folguinha.Domain.Remanejo;
using Folguinha.Domain.Regras;
using static Folguinha.Domain.Tests.Dados;

namespace Folguinha.Domain.Tests;

/// Semana 12–18/10. Bruno (caixa, manhã) entrega atestado para qua 14 e qui 15.
public class RemanejamentoTests
{
    static readonly Turno Manha = new(Guid.NewGuid(), "Manhã", new(8, 0), new(16, 20), TimeSpan.FromHours(1));
    static readonly Turno Tarde = new(Guid.NewGuid(), "Tarde", new(13, 40), new(22, 0), TimeSpan.FromHours(1));
    static readonly Funcao Caixa = new(Guid.NewGuid(), "Caixa");

    readonly Funcionario _bruno = Func("Bruno") with { Funcoes = [Caixa.Id] };
    // 6x1: folga na quinta; trabalha de manhã nos outros dias
    readonly Funcionario _gabi = Func("Gabi") with { Funcoes = [Caixa.Id] };
    // 5x2 com hora extra permitida: folga quinta e domingo; dias de trabalho travados (não dá para mover a folga)
    readonly Funcionario _hugo = Func("Hugo", Regime.CincoPorDois, horaExtra: true, limiteHoraExtraSemanal: 8)
        with { Funcoes = [Caixa.Id], CargaSemanal = new TimeSpan(36, 40, 0) };
    // tarde; folga quinta, mas sai às 22h na quarta
    readonly Funcionario _elisa = Func("Elisa") with { Funcoes = [Caixa.Id] };
    // não é caixa
    readonly Funcionario _ana = Func("Ana");

    readonly Ocorrencia _atestado;
    readonly ContextoValidacao _escala;

    public RemanejamentoTests()
    {
        _atestado = new Ocorrencia(Guid.NewGuid(), _bruno.Id, TipoOcorrencia.Atestado, D(14), D(15));

        Alocacao M(Funcionario f, int d) => Trab(f, D(d), "08:00", "16:20", turno: Manha.Id, funcao: f.Funcoes?.FirstOrDefault());
        Alocacao T(Funcionario f, int d) => Trab(f, D(d), "13:40", "22:00", turno: Tarde.Id, funcao: f.Funcoes?.FirstOrDefault());

        Alocacao[] alocacoes =
        [
            M(_bruno, 12), M(_bruno, 13), M(_bruno, 14), M(_bruno, 15), M(_bruno, 16), M(_bruno, 17), Folga(_bruno, D(18)),
            M(_gabi, 12), M(_gabi, 13), M(_gabi, 14), Folga(_gabi, D(15)), M(_gabi, 16), M(_gabi, 17), M(_gabi, 18),
            .. new[] { 12, 13, 14, 16, 17 }.Select(d => M(_hugo, d) with { Travada = true }),
            Folga(_hugo, D(15)), Folga(_hugo, D(18)),
            T(_elisa, 12), T(_elisa, 13), T(_elisa, 14), Folga(_elisa, D(15)), T(_elisa, 16), T(_elisa, 17), T(_elisa, 18),
            T(_ana, 12), T(_ana, 13), Folga(_ana, D(14)), T(_ana, 15), T(_ana, 16), T(_ana, 17), T(_ana, 18),
        ];

        _escala = Ctx([_bruno, _gabi, _hugo, _elisa, _ana], alocacoes, inicio: D(12), fim: D(18),
            demandas: [.. Enum.GetValues<DayOfWeek>().SelectMany(d => new[]
            {
                new Demanda(Manha.Id, 1, 1, Caixa.Id, d),
                new Demanda(Tarde.Id, 1, 1, DiaSemana: d),
            })],
            turnos: [Manha, Tarde], funcoes: [Caixa]);
    }

    Alocacao Quinta => Remanejamento.TurnosAfetados(_escala, _atestado).Single(a => a.Data == D(15));

    [Fact]
    public void Lista_os_turnos_afetados_pela_ocorrencia()
    {
        var afetados = Remanejamento.TurnosAfetados(_escala, _atestado);

        Assert.Equal([D(14), D(15)], afetados.Select(a => a.Data));
        Assert.All(afetados, a => Assert.Equal(_bruno.Id, a.FuncionarioId));
    }

    [Fact]
    public void Atraso_nao_afeta_turnos()
    {
        var atraso = _atestado with { Tipo = TipoOcorrencia.Atraso };

        Assert.Empty(Remanejamento.TurnosAfetados(_escala, atraso));
    }

    [Fact]
    public void Sugere_so_quem_tem_a_funcao_e_esta_de_folga_no_dia()
    {
        var nomes = Remanejamento.SugerirSubstitutos(_escala, _atestado, Quinta).Select(s => s.Funcionario.Nome);

        Assert.Equal(["Gabi", "Hugo", "Elisa"], nomes);
    }

    [Fact]
    public void Move_a_folga_quando_cobrir_quebraria_o_descanso_semanal()
    {
        var gabi = Remanejamento.SugerirSubstitutos(_escala, _atestado, Quinta).Single(s => s.Funcionario.Nome == "Gabi");

        Assert.True(gabi.Elegivel);
        Assert.Equal(D(14), gabi.FolgaMovidaPara); // Hugo cobre o caixa da quarta
        Assert.Equal(TimeSpan.Zero, gabi.HorasExtras);
        Assert.Contains("folga passa para qua 14/10", gabi.Resumo);
    }

    [Fact]
    public void Nao_move_a_folga_para_dia_que_ficaria_sem_cobertura()
    {
        // sem o Hugo, a Gabi é a única caixa da quarta
        var semHugo = _escala with
        {
            Funcionarios = [.. _escala.Funcionarios.Where(f => f.Id != _hugo.Id)],
            Alocacoes = [.. _escala.Alocacoes.Where(a => a.FuncionarioId != _hugo.Id)],
        };

        var gabi = Remanejamento.SugerirSubstitutos(semHugo, _atestado, Quinta).Single(s => s.Funcionario.Nome == "Gabi");

        Assert.Equal(D(16), gabi.FolgaMovidaPara);
    }

    [Fact]
    public void Informa_as_horas_extras_quando_nao_da_para_mover_a_folga()
    {
        var hugo = Remanejamento.SugerirSubstitutos(_escala, _atestado, Quinta).Single(s => s.Funcionario.Nome == "Hugo");

        Assert.True(hugo.Elegivel);
        Assert.Null(hugo.FolgaMovidaPara);
        Assert.Equal(new TimeSpan(7, 20, 0), hugo.HorasExtras);
        Assert.Contains("gera 7h20 extras", hugo.Resumo);
    }

    [Fact]
    public void Mostra_o_motivo_de_quem_nao_pode_cobrir()
    {
        var elisa = Remanejamento.SugerirSubstitutos(_escala, _atestado, Quinta).Single(s => s.Funcionario.Nome == "Elisa");

        Assert.False(elisa.Elegivel);
        Assert.Contains("descanso de 10h", elisa.Resumo);
    }

    [Fact]
    public void Aplicar_gera_nova_escala_travada_com_impacto()
    {
        var gabi = Remanejamento.SugerirSubstitutos(_escala, _atestado, Quinta).First();

        var r = Remanejamento.Aplicar(_escala, _atestado, [gabi]);

        Assert.All(r.Alocacoes.Where(a => a.FuncionarioId == _bruno.Id && a.Data >= D(14) && a.Data <= D(15)),
            a => Assert.Equal(TipoAlocacao.Ocorrencia, a.Tipo));
        var gabiQuinta = r.Alocacoes.Single(a => a.FuncionarioId == _gabi.Id && a.Data == D(15));
        Assert.True(gabiQuinta.Trabalha && gabiQuinta.Travada);
        Assert.Equal(Caixa.Id, gabiQuinta.FuncaoId);
        Assert.Equal(TipoAlocacao.Folga, r.Alocacoes.Single(a => a.FuncionarioId == _gabi.Id && a.Data == D(14)).Tipo);

        Assert.Equal(1.0, r.Impacto.Cobertura);
        Assert.Equal(TimeSpan.Zero, r.Impacto.HorasExtras);
        Assert.Equal(new[] { _bruno.Id, _gabi.Id }.Order(), r.Impacto.Afetados.Order());
        Assert.DoesNotContain(r.Violacoes, v => v.Severidade is Severidade.Bloqueio or Severidade.Critico);
    }

    [Fact]
    public void Sem_substituto_aponta_a_falta_de_cobertura()
    {
        var r = Remanejamento.Aplicar(_escala, _atestado, []);

        Assert.True(r.Impacto.Cobertura < 1.0);
        Assert.Contains(r.Violacoes, v => v.Severidade == Severidade.Critico && v.Data == D(15));
        Assert.Equal([_bruno.Id], r.Impacto.Afetados);
    }
}
