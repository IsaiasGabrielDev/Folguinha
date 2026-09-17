using Folguinha.Application;
using Folguinha.Domain;

namespace Folguinha.Web.Servicos;

/// Loja de exemplo para testar o app sem cadastrar nada (dados fictícios).
public static class Exemplo
{
    public static DadosDaUnidade Loja()
    {
        var manha = new Turno(Guid.NewGuid(), "Manhã", new(8, 0), new(16, 20), TimeSpan.FromHours(1));
        var tarde = new Turno(Guid.NewGuid(), "Tarde", new(13, 40), new(22, 0), TimeSpan.FromHours(1));
        var caixa = new Funcao(Guid.NewGuid(), "Caixa");
        var atendente = new Funcao(Guid.NewGuid(), "Atendente");
        string[] nomes = ["Ana Souza", "Bruno Lima", "Carla Mendes", "Diego Santos", "Elisa Prado", "Felipe Nunes", "Gabi Rocha", "Hugo Alves"];

        var funcionarios = nomes.Select((nome, i) => new Funcionario(Guid.NewGuid(), nome, Regime.SeisPorUm, TimeSpan.FromHours(44))
        {
            Funcoes = [i < 3 ? caixa.Id : atendente.Id],
            TurnoPrincipal = i % 2 == 0 ? manha.Id : tarde.Id,
            Situacao = new SituacaoInicial(new DateOnly(2026, 9, 27 + i % 4)),
        }).ToList();

        DayOfWeek[] semana = [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday];
        return new DadosDaUnidade
        {
            Empresa = new Empresa(Guid.NewGuid(), "Loja Centro (exemplo)", RamoAtividade.Comercio),
            Funcionarios = funcionarios,
            Turnos = [manha, tarde],
            Funcoes = [caixa, atendente],
            Demandas =
            [
                .. Enum.GetValues<DayOfWeek>().SelectMany(d => semana.Contains(d)
                    ? new[] { new Demanda(manha.Id, 2, 3, DiaSemana: d), new Demanda(tarde.Id, 2, 3, DiaSemana: d) }
                    : [new Demanda(manha.Id, 1, 2, DiaSemana: d), new Demanda(tarde.Id, 1, 2, DiaSemana: d)]),
                .. Enum.GetValues<DayOfWeek>().Select(d => new Demanda(manha.Id, 1, 1, caixa.Id, d)),
            ],
            Feriados = [new Feriado(new DateOnly(2026, 10, 12), "Nossa Senhora Aparecida", AbrangenciaFeriado.Nacional)],
        };
    }
}
