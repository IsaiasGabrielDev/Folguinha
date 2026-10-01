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

    /// Loja de exemplo completa: 6 pessoas 6x1, aberta 7h–23h todo dia.
    /// 3 de manhã, 2 na tarde até as 22h e 1 até as 23h — por isso das 22h às 23h o mínimo é 1.
    /// Mínimo de 2 no resto do dia; seg, qua, qui e sáb costumam ter mais movimento (ideal 3).
    /// Fim de semana de folga sim, outro não (sábado + domingo); nas outras semanas a folga cai de seg a sex.
    /// A última folga, o último domingo de folga e o último feriado ficam escalonados, como se a escala
    /// anterior tivesse rodado.
    public static DadosDaUnidade LojaCompleta(DateOnly inicioDaEscala)
    {
        var manha = new Turno(Guid.NewGuid(), "Manhã", new(7, 0), new(15, 20), TimeSpan.FromHours(1));
        var tarde = new Turno(Guid.NewGuid(), "Tarde", new(13, 40), new(22, 0), TimeSpan.FromHours(1));
        var noite = new Turno(Guid.NewGuid(), "Tarde até 23h", new(14, 40), new(23, 0), TimeSpan.FromHours(1));
        var atendimento = new Funcao(Guid.NewGuid(), "Atendimento");
        Turno[] principal = [manha, manha, manha, tarde, tarde, noite];
        var ultimoDomingo = inicioDaEscala.AddDays(-1 - ((int)inicioDaEscala.AddDays(-1).DayOfWeek));

        var funcionarios = principal.Select((turno, i) => new Funcionario(Guid.NewGuid(), $"Pessoa {i + 1}", Regime.SeisPorUm, TimeSpan.FromHours(44),
            // das 22h às 23h a hora noturna é reduzida: 6 dias até as 23h passam de 44h por alguns minutos
            PermiteHoraExtra: true, LimiteHoraExtraSemanal: TimeSpan.FromHours(2))
        {
            Funcoes = [atendimento.Id],
            TurnoPrincipal = turno.Id,
            // metade da equipe folgou o último fim de semana, a outra metade folga o próximo
            Situacao = new SituacaoInicial(inicioDaEscala.AddDays(-1 - i), ultimoDomingo.AddDays(i is 0 or 1 or 3 ? 0 : -7), i % 2 == 0),
        }).ToList();

        var dias = Enum.GetValues<DayOfWeek>();
        DayOfWeek[] movimento = [DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Saturday];
        return new DadosDaUnidade
        {
            Empresa = new Empresa(Guid.NewGuid(), "Loja Avenida", RamoAtividade.Comercio),
            Uf = "SP",
            FolgaCasada = true,
            DomingoACada = 2,
            Funcionarios = funcionarios,
            Turnos = [manha, tarde, noite],
            Funcoes = [atendimento],
            Funcionamento = [.. dias.Select(d => new PeriodoFuncionamento(d, new(7, 0), new(23, 0)))],
            Demandas =
            [
                .. dias.Select(d => new Demanda(null, 2, movimento.Contains(d) ? 3 : 2, DiaSemana: d, Inicio: new(7, 0), Fim: new(22, 0))),
                .. dias.Select(d => new Demanda(null, 1, 1, DiaSemana: d, Inicio: new(22, 0), Fim: new(23, 0))),
            ],
            AceitouAviso = true,
        };
    }
}
