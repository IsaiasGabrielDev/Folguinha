using Folguinha.Application;
using Folguinha.Domain;

namespace Folguinha.Application.Tests;

/// O caso real: 6 pessoas com a folga extra toda na semana de 14/09 folgam juntas
/// no fim de semana de 19–20/09 e a loja fica sem ninguém.
public class FolgasExtrasTests
{
    static DadosDaUnidade Equipe(params DateOnly[] semanas) => new()
    {
        Funcionarios = [.. semanas.Select((d, i) => new Funcionario(Guid.NewGuid(), $"P{i}", Regime.SeisPorUm, TimeSpan.FromHours(44))
        {
            FolgaExtra = new FolgaExtraPeriodica(2, d, Posicao: PosicaoFolgaExtra.FimDeSemana),
        })],
    };

    static readonly DateOnly Semana = new(2026, 9, 14);

    [Fact]
    public void Acusa_quando_a_equipe_inteira_folga_na_mesma_semana()
    {
        var todosJuntos = Equipe(Semana, Semana, Semana, Semana, Semana, Semana);

        Assert.Equal(6, FolgasExtras.NaMesmaSemana(todosJuntos).Count);
    }

    [Fact]
    public void Nao_acusa_quem_ja_esta_revezado()
    {
        var revezado = Equipe(Semana, Semana.AddDays(7), Semana, Semana.AddDays(7));

        Assert.Empty(FolgasExtras.NaMesmaSemana(revezado));
    }

    [Fact]
    public void Revezar_divide_a_equipe_nas_semanas_do_ciclo_sem_mexer_na_ordem()
    {
        var todosJuntos = Equipe(Semana, Semana, Semana, Semana, Semana, Semana);
        var modelo = todosJuntos.Funcionarios[0].FolgaExtra!;

        var d = FolgasExtras.Revezar(todosJuntos, modelo);

        // a cada 2 semanas = 2 grupos, metade em cada
        var porSemana = d.Funcionarios.GroupBy(f => f.FolgaExtra!.APartirDe).ToList();
        Assert.Equal(2, porSemana.Count);
        Assert.All(porSemana, g => Assert.Equal(3, g.Count()));
        Assert.Empty(FolgasExtras.NaMesmaSemana(d));

        // a ordem da lista não muda: o gerador usa o índice para alternar plantões no 12x36
        Assert.Equal(todosJuntos.Funcionarios.Select(f => f.Id), d.Funcionarios.Select(f => f.Id));
    }
}
