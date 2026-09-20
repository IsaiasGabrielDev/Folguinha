using Folguinha.Domain.Geracao;
using Folguinha.Domain.Regras;
using static Folguinha.Domain.Tests.Dados;

namespace Folguinha.Domain.Tests;

public class GeradorTests
{
    static readonly Turno Manha = new(Guid.NewGuid(), "Manhã", new(8, 0), new(16, 20), TimeSpan.FromHours(1));
    static readonly Turno Tarde = new(Guid.NewGuid(), "Tarde", new(13, 40), new(22, 0), TimeSpan.FromHours(1));
    static readonly Turno Plantao = new(Guid.NewGuid(), "Plantão", new(7, 0), new(19, 0), TimeSpan.FromHours(1));
    static readonly Funcao Caixa = new(Guid.NewGuid(), "Caixa");
    static readonly Feriado Independencia = new(D(7, 9), "Independência", AbrangenciaFeriado.Nacional);
    static readonly Feriado Aparecida = new(D(12), "Nossa Senhora Aparecida", AbrangenciaFeriado.Nacional);

    static readonly string[] Nomes = ["Ana", "Bruno", "Carla", "Diego", "Elisa", "Felipe", "Gabi", "Hugo"];

    /// Varejo: 2 pessoas por turno de seg a sex, 1 por turno no fim de semana.
    static Demanda[] DemandaVarejo() =>
    [
        .. new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday }
            .SelectMany(d => new[] { new Demanda(Manha.Id, 2, 2, DiaSemana: d), new Demanda(Tarde.Id, 2, 2, DiaSemana: d) }),
        .. new[] { DayOfWeek.Saturday, DayOfWeek.Sunday }
            .SelectMany(d => new[] { new Demanda(Manha.Id, 1, 1, DiaSemana: d), new Demanda(Tarde.Id, 1, 1, DiaSemana: d) }),
    ];

    static EntradaGeracao Varejo(Funcionario[] equipe) => new()
    {
        Empresa = Comercio,
        Inicio = D(1),
        Fim = D(31),
        Funcionarios = equipe,
        Turnos = [Manha, Tarde],
        Demandas = DemandaVarejo(),
        Feriados = [Independencia, Aparecida],
    };

    static int DiasTrabalhados(ResultadoGeracao r, Funcionario f, DateOnly de, DateOnly ate) =>
        r.Alocacoes.Count(a => a.FuncionarioId == f.Id && a.Trabalha && a.Data >= de && a.Data <= ate);

    [Fact]
    public void Varejo_com_8_pessoas_gera_o_mes_sem_bloqueios_nem_falta_de_cobertura()
    {
        var equipe = Nomes.Select(n => Func(n)).ToArray();

        var r = GeradorEscala.Gerar(Varejo(equipe));

        Assert.DoesNotContain(r.Violacoes, v => v.Severidade is Severidade.Bloqueio or Severidade.Critico);
        Assert.True(r.PodePublicar);
    }

    [Fact]
    public void Cada_dia_do_periodo_tem_uma_alocacao_por_pessoa()
    {
        var equipe = Nomes.Select(n => Func(n)).ToArray();

        var r = GeradorEscala.Gerar(Varejo(equipe));

        Assert.Equal(31 * 8, r.Alocacoes.Count);
        Assert.Equal(31 * 8, r.Alocacoes.Select(a => (a.FuncionarioId, a.Data)).Distinct().Count());
    }

    [Fact]
    public void Seis_por_um_trabalha_5_ou_6_dias_por_semana_e_quase_o_mes_todo()
    {
        // a semana que antecede a folga de domingo obrigatória pode ter duas folgas (limite de 6 dias seguidos)
        var equipe = Nomes.Select(n => Func(n)).ToArray();

        var r = GeradorEscala.Gerar(Varejo(equipe));

        foreach (var segunda in new[] { D(5), D(12), D(19) })
            Assert.All(equipe, f => Assert.InRange(DiasTrabalhados(r, f, segunda, segunda.AddDays(6)), 5, 6));
        Assert.All(equipe, f => Assert.InRange(DiasTrabalhados(r, f, D(1), D(31)), 24, 27));
    }

    [Fact]
    public void Cinco_por_dois_trabalha_5_dias_nas_semanas_completas()
    {
        var equipe = Nomes.Select((n, i) => i < 2 ? Func(n, Regime.CincoPorDois, cargaSemanal: 36.67) : Func(n)).ToArray();

        var r = GeradorEscala.Gerar(Varejo(equipe));

        Assert.Equal(5, DiasTrabalhados(r, equipe[0], D(12), D(18)));
        Assert.Equal(5, DiasTrabalhados(r, equipe[1], D(12), D(18)));
    }

    [Fact]
    public void Doze_por_trinta_e_seis_alterna_dias_e_cobre_o_plantao()
    {
        var equipe = new[] { "Ana", "Bruno", "Carla", "Diego" }
            .Select(n => Func(n, Regime.DozePorTrintaESeis, cargaSemanal: 42) with { Turnos = [Plantao.Id] })
            .ToArray();
        var entrada = new EntradaGeracao
        {
            Empresa = Comercio, Inicio = D(1), Fim = D(31), Funcionarios = equipe, Turnos = [Plantao],
            Demandas = Enum.GetValues<DayOfWeek>().Select(d => new Demanda(Plantao.Id, 2, 2, DiaSemana: d)).ToArray(),
        };

        var r = GeradorEscala.Gerar(entrada);

        Assert.DoesNotContain(r.Violacoes, v => v.Severidade is Severidade.Bloqueio or Severidade.Critico);
        Assert.All(equipe, f =>
        {
            var dias = r.Alocacoes.Where(a => a.FuncionarioId == f.Id && a.Trabalha).Select(a => a.Data).ToList();
            Assert.All(dias.Zip(dias.Skip(1)), p => Assert.Equal(2, p.Second.DayNumber - p.First.DayNumber));
        });
    }

    [Fact]
    public void So_quem_tem_a_funcao_ocupa_a_vaga_da_funcao()
    {
        var equipe = Nomes.Select((n, i) => i < 3 ? Func(n) with { Funcoes = [Caixa.Id] } : Func(n)).ToArray();
        var entrada = Varejo(equipe) with
        {
            Funcoes = [Caixa],
            Demandas = [.. DemandaVarejo(), .. Enum.GetValues<DayOfWeek>().Select(d => new Demanda(Manha.Id, 1, 1, Caixa.Id, d))],
        };

        var r = GeradorEscala.Gerar(entrada);

        Assert.DoesNotContain(r.Violacoes, v => v.Severidade is Severidade.Bloqueio or Severidade.Critico);
        var caixas = equipe.Take(3).Select(f => f.Id).ToHashSet();
        Assert.All(r.Alocacoes.Where(a => a.FuncaoId == Caixa.Id), a => Assert.Contains(a.FuncionarioId, caixas));
    }

    [Fact]
    public void Respeita_indisponibilidade_contratual()
    {
        var semDomingo = Func("Ana") with { Disponibilidades = [new Disponibilidade(DayOfWeek.Sunday, null, null)] };
        var sabadoCedo = Func("Bruno") with { Disponibilidades = [new Disponibilidade(DayOfWeek.Saturday, new(8, 0), new(17, 0))] };
        var equipe = new[] { semDomingo, sabadoCedo }.Concat(Nomes.Skip(2).Select(n => Func(n))).ToArray();

        var r = GeradorEscala.Gerar(Varejo(equipe));

        Assert.DoesNotContain(r.Alocacoes, a => a.FuncionarioId == semDomingo.Id && a.Trabalha && a.Data.DayOfWeek == DayOfWeek.Sunday);
        Assert.DoesNotContain(r.Alocacoes, a => a.FuncionarioId == sabadoCedo.Id && a.TurnoId == Tarde.Id && a.Data.DayOfWeek == DayOfWeek.Saturday);
    }

    [Fact]
    public void Afastamento_vira_ocorrencia_na_escala()
    {
        var equipe = Nomes.Select(n => Func(n)).ToArray();
        var atestado = new Ocorrencia(Guid.NewGuid(), equipe[1].Id, TipoOcorrencia.Atestado, D(14), D(15));

        var r = GeradorEscala.Gerar(Varejo(equipe) with { Ocorrencias = [atestado] });

        var doBruno = r.Alocacoes.Where(a => a.FuncionarioId == equipe[1].Id && (a.Data == D(14) || a.Data == D(15)));
        Assert.All(doBruno, a => Assert.Equal(TipoAlocacao.Ocorrencia, a.Tipo));
        Assert.DoesNotContain(r.Violacoes, v => v.Severidade == Severidade.Bloqueio);
    }

    [Fact]
    public void Mantem_alocacoes_travadas()
    {
        var equipe = Nomes.Select(n => Func(n)).ToArray();
        var travada = Trab(equipe[0], D(10), "13:40", "22:00", turno: Tarde.Id) with { Travada = true };

        var r = GeradorEscala.Gerar(Varejo(equipe) with { Travadas = [travada] });

        Assert.Contains(travada, r.Alocacoes);
    }

    [Fact]
    public void Quem_trabalhou_no_ultimo_feriado_folga_no_proximo()
    {
        var ana = Func("Ana");
        var carla = Func("Carla");
        var entrada = new EntradaGeracao
        {
            Empresa = Comercio, Inicio = D(12), Fim = D(18), Funcionarios = [ana, carla], Turnos = [Manha],
            Demandas = Enum.GetValues<DayOfWeek>().Select(d => new Demanda(Manha.Id, 1, 1, DiaSemana: d)).ToArray(),
            Feriados = [Independencia, Aparecida],
            Historico = [Trab(carla, D(7, 9), "08:00", "16:20", turno: Manha.Id), Folga(ana, D(7, 9))],
        };

        var r = GeradorEscala.Gerar(entrada);

        Assert.True(r.Alocacoes.Single(a => a.FuncionarioId == ana.Id && a.Data == D(12)).Trabalha);
        Assert.Equal(TipoAlocacao.Folga, r.Alocacoes.Single(a => a.FuncionarioId == carla.Id && a.Data == D(12)).Tipo);
    }

    [Fact]
    public void Avisa_quando_o_rodizio_de_feriado_nao_pode_ser_cumprido()
    {
        var ana = Func("Ana") with { Disponibilidades = [new Disponibilidade(DayOfWeek.Monday, null, null)] };
        var carla = Func("Carla");
        var entrada = new EntradaGeracao
        {
            Empresa = Comercio, Inicio = D(12), Fim = D(18), Funcionarios = [ana, carla], Turnos = [Manha],
            Demandas = Enum.GetValues<DayOfWeek>().Select(d => new Demanda(Manha.Id, 1, 1, DiaSemana: d)).ToArray(),
            Feriados = [Independencia, Aparecida],
            Historico = [Trab(carla, D(7, 9), "08:00", "16:20", turno: Manha.Id)],
        };

        var r = GeradorEscala.Gerar(entrada);

        var aviso = Assert.Single(r.Violacoes, v => v.RegraId == GeradorEscala.RegraRodizioFeriado);
        Assert.Equal("Carla trabalhou no último feriado (seg 07/09) e não pôde folgar em seg 12/10.", aviso.Mensagem);
    }

    [Fact]
    public void Aponta_deficit_quando_nao_ha_gente_suficiente()
    {
        var equipe = new[] { Func("Ana"), Func("Bruno") };
        var entrada = new EntradaGeracao
        {
            Empresa = Comercio, Inicio = D(13), Fim = D(13), Funcionarios = equipe, Turnos = [Manha],
            Demandas = [new Demanda(Manha.Id, 3, 3, Data: D(13))],
        };

        var r = GeradorEscala.Gerar(entrada);

        var v = Assert.Single(r.Violacoes, v => v.Severidade == Severidade.Critico);
        Assert.Contains("falta 1 pessoa", v.Mensagem);
    }

    [Fact]
    public void Loja_fechada_no_domingo_ninguem_trabalha()
    {
        var equipe = Nomes.Select(n => Func(n)).ToArray();
        var funcionamento = Enum.GetValues<DayOfWeek>().Where(d => d != DayOfWeek.Sunday)
            .Select(d => new PeriodoFuncionamento(d, new(8, 0), new(22, 30))).ToArray();

        var r = GeradorEscala.Gerar(Varejo(equipe) with { Funcionamento = funcionamento });

        Assert.DoesNotContain(r.Alocacoes, a => a.Trabalha && a.Data.DayOfWeek == DayOfWeek.Sunday);
    }

    [Fact]
    public void Resultado_e_deterministico()
    {
        var equipe = Nomes.Select(n => Func(n)).ToArray();

        var a = GeradorEscala.Gerar(Varejo(equipe));
        var b = GeradorEscala.Gerar(Varejo(equipe));

        Assert.Equal(a.Alocacoes, b.Alocacoes);
    }

    [Fact]
    public void Segundo_mes_continua_a_partir_do_historico_sem_bloqueios()
    {
        var equipe = Nomes.Select(n => Func(n)).ToArray();
        var outubro = GeradorEscala.Gerar(Varejo(equipe));

        var novembro = GeradorEscala.Gerar(Varejo(equipe) with { Inicio = D(1, 11), Fim = D(30, 11), Historico = outubro.Alocacoes });

        Assert.DoesNotContain(novembro.Violacoes, v => v.Severidade is Severidade.Bloqueio or Severidade.Critico);
    }

    /// Uma pessoa só, sem demanda: isola a contagem de dias da disputa por cobertura.
    static EntradaGeracao Sozinha(Funcionario f) => new()
    {
        Empresa = Comercio, Inicio = D(1), Fim = D(31), Funcionarios = [f], Turnos = [Manha, Tarde],
        Feriados = [Aparecida],
    };

    [Fact]
    public void Quem_trabalha_no_feriado_folga_um_dia_a_mais_na_semana_seguinte()
    {
        // Aparecida cai na segunda 12/10; a semana seguinte vai de 19 a 25/10
        var bia = Func("Bia");
        // trava o feriado como dia de trabalho nas duas gerações: só a compensação muda
        var noFeriado = Alocacao.Trabalho(bia.Id, D(12), Manha.Inicio, Manha.Fim, Manha.Intervalo, Manha.Id) with { Travada = true };
        var entrada = Sozinha(bia) with { Travadas = [noFeriado] };

        var com = GeradorEscala.Gerar(entrada with { CompensarFeriado = true });
        var sem = GeradorEscala.Gerar(entrada);

        Assert.Equal(6, DiasTrabalhados(sem, bia, D(19), D(25)));
        Assert.Equal(5, DiasTrabalhados(com, bia, D(19), D(25)));
    }

    [Fact]
    public void Quem_folga_no_feriado_nao_ganha_compensacao()
    {
        var bia = Func("Bia") with { Situacao = new SituacaoInicial(D(1), TrabalhouUltimoFeriado: false) };
        var folgaNoFeriado = Alocacao.Folga(bia.Id, D(12)) with { Travada = true };

        var r = GeradorEscala.Gerar(Sozinha(bia) with { CompensarFeriado = true, Travadas = [folgaNoFeriado] });

        Assert.Equal(6, DiasTrabalhados(r, bia, D(19), D(25)));
    }

    [Fact]
    public void Rodizio_de_turnos_tira_a_pessoa_do_turno_principal_durante_o_mes()
    {
        var equipe = Nomes.Select(n => Func(n) with { TurnoPrincipal = Manha.Id }).ToArray();
        var entrada = Varejo(equipe);

        var sem = GeradorEscala.Gerar(entrada);
        var com = GeradorEscala.Gerar(entrada with { RodizioTurnos = true });

        int NaTarde(ResultadoGeracao r, Funcionario f) =>
            r.Alocacoes.Count(a => a.FuncionarioId == f.Id && a.Trabalha && a.TurnoId == Tarde.Id);

        Assert.All(equipe, f => Assert.True(NaTarde(com, f) > 0, $"{f.Nome} nunca pegou a tarde"));
        Assert.True(equipe.Sum(f => NaTarde(com, f)) > equipe.Sum(f => NaTarde(sem, f)));
        Assert.DoesNotContain(com.Violacoes, v => v.Severidade is Severidade.Bloqueio or Severidade.Critico);
    }
}
