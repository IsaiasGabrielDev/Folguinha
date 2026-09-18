using Folguinha.Domain.Geracao;
using Folguinha.Domain.Regras;
using static Folguinha.Domain.Tests.Dados;

namespace Folguinha.Domain.Tests;

/// Faixa de horário: mínimo de pessoas presentes num intervalo, somando quem estiver
/// em qualquer turno que cubra aquele instante (ESPEC §6, pico x horário morto).
public class FaixaHorarioTests
{
    readonly CoberturaMinima _regra = new();
    static readonly Turno Manha = new(Guid.NewGuid(), "Manhã", new(8, 0), new(16, 20), TimeSpan.FromHours(1));
    static readonly Turno Tarde = new(Guid.NewGuid(), "Tarde", new(13, 40), new(22, 0), TimeSpan.FromHours(1));
    static readonly Turno[] Turnos = [Manha, Tarde];

    static Demanda Faixa(string inicio, string fim, int minimo, DayOfWeek dia) =>
        new(null, minimo, minimo, DiaSemana: dia, Inicio: TimeOnly.Parse(inicio), Fim: TimeOnly.Parse(fim));

    [Fact]
    public void Aponta_o_instante_mais_vazio_e_nao_a_media_do_dia()
    {
        // 3 pessoas à tarde cobrem o pico, mas às 8h a loja abre com 1 só.
        var ana = Func("Ana");
        Funcionario[] tarde = [Func("Bruno"), Func("Carla"), Func("Diego")];
        var pico = Faixa("08:00", "22:00", 2, DayOfWeek.Thursday);

        var v = Assert.Single(_regra.Validar(Ctx([ana, .. tarde],
            [Trab(ana, D(15), "08:00", "16:20", turno: Manha.Id),
             .. tarde.Select(f => Trab(f, D(15), "13:40", "22:00", turno: Tarde.Id))],
            inicio: D(15), fim: D(15), demandas: [pico], turnos: Turnos)));

        Assert.Equal(Severidade.Critico, v.Severidade);
        Assert.Equal("qui 15/10 · 08:00–22:00: falta 1 pessoa às 08:00.", v.Mensagem);
    }

    [Fact]
    public void Soma_os_dois_turnos_na_sobreposicao()
    {
        // Das 13:40 às 16:20 manhã e tarde estão juntas: 1 + 1 atende um mínimo de 2.
        var ana = Func("Ana");
        var bruno = Func("Bruno");
        var pico = Faixa("14:00", "16:00", 2, DayOfWeek.Thursday);

        Assert.Empty(_regra.Validar(Ctx([ana, bruno],
            [Trab(ana, D(15), "08:00", "16:20", turno: Manha.Id), Trab(bruno, D(15), "13:40", "22:00", turno: Tarde.Id)],
            inicio: D(15), fim: D(15), demandas: [pico], turnos: Turnos)));
    }

    [Fact]
    public void Horario_morto_aceita_menos_gente_que_o_pico()
    {
        // O que o modelo por turno não sabia dizer: 1 basta na abertura, 2 no pico.
        var ana = Func("Ana");
        var bruno = Func("Bruno");
        Demanda[] demandas =
        [
            Faixa("08:00", "10:00", 1, DayOfWeek.Thursday),
            Faixa("14:00", "16:00", 2, DayOfWeek.Thursday),
        ];

        Assert.Empty(_regra.Validar(Ctx([ana, bruno],
            [Trab(ana, D(15), "08:00", "16:20", turno: Manha.Id), Trab(bruno, D(15), "13:40", "22:00", turno: Tarde.Id)],
            inicio: D(15), fim: D(15), demandas: demandas, turnos: Turnos)));
    }

    [Fact]
    public void Turno_sozinho_na_faixa_precisa_segurar_o_minimo_inteiro()
    {
        var faixaDoDia = Faixa("08:00", "22:00", 2, DayOfWeek.Thursday);
        var soNoPico = Faixa("14:00", "16:00", 2, DayOfWeek.Thursday);

        // 8h–13:40 só tem manhã e 16:20–22h só tem tarde: os dois turnos ficam sozinhos.
        Assert.Equal(Turnos, faixaDoDia.TurnosSozinhos(Turnos).ToArray());
        // dentro da sobreposição ninguém fica sozinho: os turnos somam
        Assert.Empty(soNoPico.TurnosSozinhos(Turnos));
    }

    [Fact]
    public void Gerador_poe_gente_no_pico_e_deixa_o_horario_morto_no_minimo()
    {
        // Loja pequena: 12 pessoas 6x1, aberta 8h-22h com 1 pessoa, mas 3 no pico de 11h as 15h.
        // turno principal fixo: sem ele a interjornada de 11h so deixa migrar manha -> tarde e todos escorregam
        var pessoas = Enumerable.Range(0, 12)
            .Select(i => Func($"P{i:00}") with { TurnoPrincipal = i % 2 == 0 ? Manha.Id : Tarde.Id })
            .ToArray();
        Demanda[] demandas =
        [
            .. Enum.GetValues<DayOfWeek>().Select(d => Faixa("08:00", "22:00", 1, d)),
            .. Enum.GetValues<DayOfWeek>().Select(d => Faixa("11:00", "15:00", 3, d)),
        ];

        var r = GeradorEscala.Gerar(new EntradaGeracao
        {
            Empresa = Comercio,
            Inicio = D(1),
            Fim = D(31),
            Funcionarios = pessoas,
            Turnos = Turnos,
            Demandas = demandas,
        });

        var dias = Enumerable.Range(0, 31).Select(i => D(1).AddDays(i)).ToList();
        int PresentesAs(DateOnly dia, string hora) =>
            r.Alocacoes.Count(a => a.Trabalha && a.Data == dia && Demanda.Cobre(a.Inicio, a.Fim, TimeOnly.Parse(hora)));

        // o pico manda na escala em todo dia util
        var uteis = dias.Where(d => d.DayOfWeek is not DayOfWeek.Sunday).ToList();
        Assert.All(uteis, d => Assert.True(PresentesAs(d, "11:00") >= 3, $"{d:dd/MM} tinha {PresentesAs(d, "11:00")} as 11:00"));

        // no domingo 12 pessoas em 6x1 nao dao conta do pico; o app mostra o melhor que da e avisa,
        // em vez de calar o buraco — e o aviso diz a hora exata em que a loja fica descoberta
        var faltas = new CoberturaMinima()
            .Validar(new ContextoValidacao(Comercio, D(1), D(31), pessoas, r.Alocacoes, [], [], demandas, Turnos, []))
            .ToList();
        Assert.All(faltas, v => Assert.Equal(DayOfWeek.Sunday, v.Data!.Value.DayOfWeek));
        Assert.All(faltas, v => Assert.Contains("11:00–15:00", v.Mensagem));
    }
}

/// Caso real da recepção: a folga extra nunca cai no fim de semana — o sábado+domingo
/// vem do descanso obrigatório, não dela.
public class FolgaExtraEmDiaUtilTests
{
    static readonly Turno Manha = new(Guid.NewGuid(), "Manhã", new(7, 0), new(15, 20), TimeSpan.FromHours(1));
    static readonly Turno Noite = new(Guid.NewGuid(), "Noite", new(14, 40), new(23, 0), TimeSpan.FromHours(1));

    static ResultadoGeracao Gerar(PosicaoFolgaExtra posicao, out Funcionario[] pessoas)
    {
        var equipe = Enumerable.Range(0, 8)
            .Select(i => Func($"P{i:00}") with
            {
                TurnoPrincipal = i % 2 == 0 ? Manha.Id : Noite.Id,
                FolgaExtra = new FolgaExtraPeriodica(2, D(1), Posicao: posicao),
                Situacao = new SituacaoInicial(D(1).AddDays(-1 - i % 6)),
            })
            .ToArray();
        pessoas = equipe;
        return GeradorEscala.Gerar(new EntradaGeracao
        {
            Empresa = Comercio,
            Inicio = D(1),
            Fim = D(31),
            Funcionarios = equipe,
            Turnos = [Manha, Noite],
            // fim de semana pede menos gente, que é o que atrai as folgas para lá
            Demandas = [.. Enum.GetValues<DayOfWeek>().Select(d => new Demanda(null,
                d is DayOfWeek.Saturday or DayOfWeek.Sunday ? 1 : 2,
                d is DayOfWeek.Saturday or DayOfWeek.Sunday ? 1 : 2,
                DiaSemana: d, Inicio: new(7, 0), Fim: new(23, 0)))],
        });
    }

    [Fact]
    public void Tira_a_folga_extra_do_fim_de_semana_sem_derrubar_o_domingo_obrigatorio()
    {
        var emDiaUtil = Gerar(PosicaoFolgaExtra.DiaUtil, out _);
        var solto = Gerar(PosicaoFolgaExtra.Livre, out _);

        int Em(ResultadoGeracao r, DayOfWeek dia) =>
            r.Alocacoes.Count(a => !a.Trabalha && a.Data.DayOfWeek == dia);

        // o sábado deixa de receber folga: sobrando dia útil, é ele que leva a extra
        Assert.Equal(0, Em(emDiaUtil, DayOfWeek.Saturday));
        Assert.True(Em(solto, DayOfWeek.Saturday) > 0);

        // o domingo continua, porque ali quem manda é o descanso obrigatório, não a folga extra
        Assert.True(Em(emDiaUtil, DayOfWeek.Sunday) > 0);

        // e no total sobra bem menos folga no fim de semana
        int FimDeSemana(ResultadoGeracao r) => Em(r, DayOfWeek.Saturday) + Em(r, DayOfWeek.Sunday);
        Assert.True(FimDeSemana(emDiaUtil) < FimDeSemana(solto),
            $"dia útil={FimDeSemana(emDiaUtil)}, solto={FimDeSemana(solto)}");
    }
}
