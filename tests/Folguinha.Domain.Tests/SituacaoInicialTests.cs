using Folguinha.Domain.Geracao;
using Folguinha.Domain.Regras;
using static Folguinha.Domain.Tests.Dados;

namespace Folguinha.Domain.Tests;

public class SituacaoInicialTests
{
    static readonly Turno Manha = new(Guid.NewGuid(), "Manhã", new(8, 0), new(16, 20), TimeSpan.FromHours(1));
    static readonly Turno Tarde = new(Guid.NewGuid(), "Tarde", new(13, 40), new(22, 0), TimeSpan.FromHours(1));
    static readonly Feriado Independencia = new(D(7, 9), "Independência", AbrangenciaFeriado.Nacional);
    static readonly Feriado Aparecida = new(D(12), "Nossa Senhora Aparecida", AbrangenciaFeriado.Nacional);
    static readonly string[] Nomes = ["Ana", "Bruno", "Carla", "Diego", "Elisa", "Felipe", "Gabi", "Hugo"];

    /// Demanda maior no meio da semana: sem a situação inicial, as folgas iriam para o fim de semana.
    static EntradaGeracao Semana(Funcionario[] equipe, int minimoFimDeSemana = 1) => new()
    {
        Empresa = Comercio, Inicio = D(5), Fim = D(11), Funcionarios = equipe, Turnos = [Manha, Tarde],
        Demandas = [.. Enum.GetValues<DayOfWeek>().SelectMany(d =>
        {
            var n = d is DayOfWeek.Saturday or DayOfWeek.Sunday ? minimoFimDeSemana : 2;
            return new[] { new Demanda(Manha.Id, n, n, DiaSemana: d), new Demanda(Tarde.Id, n, n, DiaSemana: d) };
        })],
    };

    static DateOnly PrimeiraFolga(ResultadoGeracao r, Funcionario f) =>
        r.Alocacoes.Where(a => a.FuncionarioId == f.Id && !a.Trabalha).Min(a => a.Data);

    [Fact]
    public void Respeita_os_dias_que_o_funcionario_ja_trabalhou_antes_da_escala()
    {
        // última folga na quinta 01/10: já trabalhou sex, sáb e dom → precisa folgar até qui 08/10
        var equipe = Nomes.Select(n => Func(n) with { Situacao = new SituacaoInicial(D(1)) }).ToArray();

        var r = GeradorEscala.Gerar(Semana(equipe));

        Assert.All(equipe, f => Assert.True(PrimeiraFolga(r, f) <= D(8), $"{f.Nome} folgou só em {PrimeiraFolga(r, f)}"));
        Assert.DoesNotContain(r.Violacoes, v => v.RegraId == DiasConsecutivos.Padrao.Id);
    }

    [Fact]
    public void Dias_ja_trabalhados_entram_na_validacao()
    {
        var ana = Func("Ana") with { Situacao = new SituacaoInicial(D(1)) };
        var travadas = Enumerable.Range(5, 4).Select(d => Trab(ana, D(d), "08:00", "16:20", turno: Manha.Id) with { Travada = true }).ToArray();

        var r = GeradorEscala.Gerar(Semana([ana]) with { Travadas = travadas });

        var v = Assert.Single(r.Violacoes, v => v.RegraId == DiasConsecutivos.Padrao.Id);
        Assert.Equal(D(8), v.Data);
    }

    [Fact]
    public void Folga_de_domingo_considera_o_ultimo_domingo_informado()
    {
        // domingos 20/09, 27/09 e 04/10 trabalhados → 11/10 é folga obrigatória no comércio
        var carla = Func("Carla") with { Situacao = new SituacaoInicial(D(1), UltimoDomingoDeFolga: D(13, 9)) };
        var diego = Func("Diego") with { Situacao = new SituacaoInicial(D(1), UltimoDomingoDeFolga: D(13, 9)) };
        var equipe = new[] { carla, diego }
            .Concat(Nomes.Skip(2).Select(n => Func(n) with { Situacao = new SituacaoInicial(D(4), UltimoDomingoDeFolga: D(4)) }))
            .ToArray();

        var r = GeradorEscala.Gerar(Semana(equipe, minimoFimDeSemana: 2));

        Assert.All(new[] { carla, diego }, f =>
            Assert.Equal(TipoAlocacao.Folga, r.Alocacoes.Single(a => a.FuncionarioId == f.Id && a.Data == D(11)).Tipo));
    }

    [Fact]
    public void Rodizio_de_feriado_usa_a_informacao_inicial()
    {
        var ana = Func("Ana") with { Situacao = new SituacaoInicial(D(11), TrabalhouUltimoFeriado: false) };
        var carla = Func("Carla") with { Situacao = new SituacaoInicial(D(11), TrabalhouUltimoFeriado: true) };
        var entrada = new EntradaGeracao
        {
            Empresa = Comercio, Inicio = D(12), Fim = D(18), Funcionarios = [ana, carla], Turnos = [Manha],
            Demandas = Enum.GetValues<DayOfWeek>().Select(d => new Demanda(Manha.Id, 1, 1, DiaSemana: d)).ToArray(),
            Feriados = [Independencia, Aparecida],
        };

        var r = GeradorEscala.Gerar(entrada);

        Assert.Equal(TipoAlocacao.Folga, r.Alocacoes.Single(a => a.FuncionarioId == carla.Id && a.Data == D(12)).Tipo);
        Assert.True(r.Alocacoes.Single(a => a.FuncionarioId == ana.Id && a.Data == D(12)).Trabalha);
    }

    [Fact]
    public void Historico_real_prevalece_sobre_a_situacao_informada()
    {
        // o cadastro diz última folga em 01/10, mas o app já registrou folga em 04/10
        var ana = Func("Ana") with { Situacao = new SituacaoInicial(D(1)) };
        var travadas = Enumerable.Range(5, 4).Select(d => Trab(ana, D(d), "08:00", "16:20", turno: Manha.Id) with { Travada = true }).ToArray();

        var r = GeradorEscala.Gerar(Semana([ana]) with { Historico = [Folga(ana, D(4))], Travadas = travadas });

        Assert.DoesNotContain(r.Violacoes, v => v.RegraId == DiasConsecutivos.Padrao.Id);
    }
}

public class FolgaExtraTests
{
    static readonly Turno Manha = new(Guid.NewGuid(), "Manhã", new(8, 0), new(16, 20), TimeSpan.FromHours(1));

    static EntradaGeracao Mes(Funcionario f, params Ocorrencia[] ocorrencias) => new()
    {
        Empresa = Industria, Inicio = D(5), Fim = D(1, 11), Funcionarios = [f], Turnos = [Manha],
        Ocorrencias = ocorrencias,
    };

    static int Dias(ResultadoGeracao r, DateOnly segunda) =>
        r.Alocacoes.Count(a => a.Trabalha && a.Data >= segunda && a.Data <= segunda.AddDays(6));

    [Fact]
    public void Folga_extra_a_cada_duas_semanas()
    {
        var carla = Func("Carla") with { FolgaExtra = new FolgaExtraPeriodica(ACadaSemanas: 2, APartirDe: D(5)) };

        var r = GeradorEscala.Gerar(Mes(carla));

        Assert.Equal([5, 6, 5, 6], new[] { D(5), D(12), D(19), D(26) }.Select(s => Dias(r, s)));
        Assert.DoesNotContain(r.Violacoes, v => v.Severidade == Severidade.Bloqueio);
    }

    [Fact]
    public void Sem_folga_extra_trabalha_6_dias_toda_semana()
    {
        var r = GeradorEscala.Gerar(Mes(Func("Carla")));

        Assert.Equal([6, 6, 6, 6], new[] { D(5), D(12), D(19), D(26) }.Select(s => Dias(r, s)));
    }

    [Fact]
    public void Folga_extra_no_fim_de_semana_fica_sabado_e_domingo()
    {
        var carla = Func("Carla") with { FolgaExtra = new FolgaExtraPeriodica(2, D(5), Posicao: PosicaoFolgaExtra.FimDeSemana) };

        var r = GeradorEscala.Gerar(Mes(carla));

        Assert.Equal([5, 6, 5, 6], new[] { D(5), D(12), D(19), D(26) }.Select(s => Dias(r, s)));
        Assert.All(new[] { D(10), D(11), D(24), D(25) }, d =>
            Assert.Equal(TipoAlocacao.Folga, r.Alocacoes.Single(a => a.Data == d).Tipo));
    }

    [Fact]
    public void Folga_extra_colada_na_folga_normal()
    {
        var carla = Func("Carla") with { FolgaExtra = new FolgaExtraPeriodica(2, D(5), Posicao: PosicaoFolgaExtra.JuntoDaFolgaNormal) };

        var r = GeradorEscala.Gerar(Mes(carla));

        Assert.Equal([5, 6, 5, 6], new[] { D(5), D(12), D(19), D(26) }.Select(s => Dias(r, s)));
        foreach (var segunda in new[] { D(5), D(19) })
        {
            var folgas = r.Alocacoes.Where(a => !a.Trabalha && a.Data >= segunda && a.Data <= segunda.AddDays(6))
                .Select(a => a.Data).Order().ToList();
            Assert.Equal(1, folgas[1].DayNumber - folgas[0].DayNumber);
        }
    }

    [Fact]
    public void Folga_extra_avulsa_vira_folga_a_mais_na_semana()
    {
        var carla = Func("Carla");
        var extra = new Ocorrencia(Guid.NewGuid(), carla.Id, TipoOcorrencia.FolgaExtra, D(14), D(14));

        var r = GeradorEscala.Gerar(Mes(carla, extra));

        Assert.Equal(TipoAlocacao.Folga, r.Alocacoes.Single(a => a.Data == D(14)).Tipo);
        Assert.Equal(5, Dias(r, D(12)));
    }
}
