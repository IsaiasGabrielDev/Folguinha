using Folguinha.Application;
using Folguinha.Domain;

namespace Folguinha.Web.Servicos;

/// Recepção de exemplo para testar o app sem cadastrar nada (dados fictícios).
/// Empresa pequena: 10 pessoas, aberta 8h–22h todo dia, dois turnos que se sobrepõem no meio
/// do dia. A demanda é por faixa de horário, não por turno: 2 pessoas seguram o horário morto
/// e o pico de 10h–18h pede 3. Todo mundo 6x1, com uma folga extra a cada 2 semanas caindo no
/// fim de semana — metade da equipe numa semana, metade na outra.
public static class Exemplo
{
    public static DadosDaUnidade Loja(DateOnly inicioDaEscala)
    {
        var manha = new Turno(Guid.NewGuid(), "Manhã", new(8, 0), new(16, 20), TimeSpan.FromHours(1));
        var tarde = new Turno(Guid.NewGuid(), "Tarde", new(13, 40), new(22, 0), TimeSpan.FromHours(1));
        var recepcao = new Funcao(Guid.NewGuid(), "Recepção");
        string[] nomes =
        [
            "Ana Souza", "Bruno Lima", "Carla Mendes", "Diego Santos", "Elisa Prado",
            "Felipe Nunes", "Gabi Rocha", "Hugo Alves", "Iara Freitas", "João Peixoto",
        ];

        var funcionarios = nomes.Select((nome, i) => new Funcionario(Guid.NewGuid(), nome, Regime.SeisPorUm, TimeSpan.FromHours(44))
        {
            Funcoes = [recepcao.Id],
            // sem turno principal fixo a interjornada de 11h só deixa migrar manhã → tarde e todos escorregam
            TurnoPrincipal = i / 2 % 2 == 0 ? manha.Id : tarde.Id,
            // i % 2 separa as duas metades: uma folga o fim de semana desta semana, a outra o da seguinte
            FolgaExtra = new FolgaExtraPeriodica(ACadaSemanas: 2, APartirDe: inicioDaEscala.AddDays(i % 2 == 0 ? 0 : 7),
                Posicao: PosicaoFolgaExtra.FimDeSemana),
            Situacao = new SituacaoInicial(inicioDaEscala.AddDays(-1 - i % 6)),
        }).ToList();

        var dias = Enum.GetValues<DayOfWeek>();
        var semana = dias.Where(d => d is not (DayOfWeek.Saturday or DayOfWeek.Sunday));
        return new DadosDaUnidade
        {
            Empresa = new Empresa(Guid.NewGuid(), "Recepção Centro (exemplo)", RamoAtividade.Comercio),
            Funcionarios = funcionarios,
            Turnos = [manha, tarde],
            Funcoes = [recepcao],
            Funcionamento = [.. dias.Select(d => new PeriodoFuncionamento(d, new(8, 0), new(22, 0)))],
            Demandas =
            [
                // piso: nunca menos de 2 pessoas, nem no horário morto
                .. dias.Select(d => new Demanda(null, 2, 2, DiaSemana: d, Inicio: new(8, 0), Fim: new(22, 0))),
                // pico de movimento nos dias úteis
                .. semana.Select(d => new Demanda(null, 3, 3, DiaSemana: d, Inicio: new(10, 0), Fim: new(18, 0))),
            ],
            AceitouAviso = true,
        };
    }
}
