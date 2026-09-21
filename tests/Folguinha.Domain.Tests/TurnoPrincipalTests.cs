using Folguinha.Domain.Geracao;
using static Folguinha.Domain.Tests.Dados;

namespace Folguinha.Domain.Tests;

/// Quem tem turno principal Manhã não pode aparecer na Tarde quando a Manhã ainda cabe.
public class TurnoPrincipalTests
{
    static readonly Turno Manha = new(Guid.NewGuid(), "Manhã", new(8, 0), new(16, 20), TimeSpan.FromHours(1));
    static readonly Turno Tarde = new(Guid.NewGuid(), "Tarde", new(13, 40), new(22, 0), TimeSpan.FromHours(1));
    static readonly Funcao Caixa = new(Guid.NewGuid(), "Caixa");

    static Demanda[] DoisPorTurno() =>
        [.. Enum.GetValues<DayOfWeek>().SelectMany(d => new[]
        {
            new Demanda(Manha.Id, 2, 2, DiaSemana: d),
            new Demanda(Tarde.Id, 2, 2, DiaSemana: d),
        })];

    static EntradaGeracao Entrada(Funcionario[] equipe, Funcao[]? funcoes = null, Demanda[]? demandas = null) => new()
    {
        Empresa = Comercio,
        Inicio = D(1),
        Fim = D(31),
        Funcionarios = equipe,
        Turnos = [Manha, Tarde],
        Funcoes = funcoes ?? [],
        Demandas = demandas ?? DoisPorTurno(),
    };

    /// Quantas vezes a pessoa foi escalada fora do turno principal dela.
    static int ForaDoPrincipal(ResultadoGeracao r, Funcionario f) =>
        r.Alocacoes.Count(a => a.FuncionarioId == f.Id && a.Trabalha && a.TurnoId != f.TurnoPrincipal);

    static Funcionario[] Equipe(int porTurno, Func<int, Funcionario, Funcionario>? ajuste = null) =>
        [.. Enumerable.Range(0, porTurno * 2).Select(i =>
        {
            var f = Func($"P{i:00}") with { TurnoPrincipal = i < porTurno ? Manha.Id : Tarde.Id };
            return ajuste is null ? f : ajuste(i, f);
        })];

    [Fact]
    public void Equipe_folgada_fica_cada_um_no_seu_turno()
    {
        // 3 por turno para uma demanda de 2: sobra gente, ninguém precisa cruzar
        var equipe = Equipe(3);

        var r = GeradorEscala.Gerar(Entrada(equipe));

        Assert.All(equipe, f => Assert.Equal(0, ForaDoPrincipal(r, f)));
    }

    /// O critério que guarda especialistas para vagas com função vinha ANTES do turno
    /// principal, então dar uma função a alguém tirava a pessoa do turno dela.
    [Fact]
    public void Ter_uma_funcao_nao_tira_a_pessoa_do_turno_principal()
    {
        // metade da equipe é Caixa; nenhuma demanda pede função, então o critério
        // "guarda especialistas" entra em ação sem ter vaga de função para guardar
        var equipe = Equipe(3, (i, f) => i % 2 == 0 ? f with { Funcoes = [Caixa.Id] } : f);

        var r = GeradorEscala.Gerar(Entrada(equipe, funcoes: [Caixa]));

        Assert.All(equipe, f => Assert.Equal(0, ForaDoPrincipal(r, f)));
    }
}
