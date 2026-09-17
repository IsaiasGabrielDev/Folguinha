using static Folguinha.Domain.Texto;

namespace Folguinha.Domain.Regras;

public sealed class JornadaDiaria() : Regra(Padrao)
{
    public static readonly DefinicaoRegra Padrao = new("clt.jornada-diaria", "Jornada diária",
        "CF art. 7º XIII; CLT arts. 58, 59 e 59-A", NivelRegra.Obrigatoria);

    public override IEnumerable<Violacao> Validar(ContextoValidacao ctx)
    {
        foreach (var f in ctx.Funcionarios)
        foreach (var a in ctx.TrabalhoDe(f).Where(a => ctx.NoPeriodo(a.Data)))
        {
            var horas = a.HorasComputadas;
            var (normal, maximo) = f.Regime switch
            {
                Regime.DozePorTrintaESeis => (TimeSpan.FromHours(12), TimeSpan.FromHours(12)),
                // 44h em 5 dias = 8h48, compensando o sábado (art. 59 §2º)
                Regime.CincoPorDois => (Min(f.CargaSemanal / 5, TimeSpan.FromHours(10)), TimeSpan.FromHours(10)),
                _ => (TimeSpan.FromHours(8), TimeSpan.FromHours(10)),
            };

            if (horas > maximo)
                yield return Violacao(f, a.Data,
                    $"{f.Nome} teria jornada de {Horas(horas)} em {Dia(a.Data)} (máximo {Horas(maximo)}).",
                    $"Encurte o turno de {f.Nome} ou divida a cobertura com outra pessoa.");
            else if (horas > normal && !f.PermiteHoraExtra)
                yield return Violacao(f, a.Data,
                    $"{f.Nome} faria {Horas(horas - normal)} de hora extra em {Dia(a.Data)}, mas não tem hora extra permitida.",
                    $"Ajuste o turno para até {Horas(normal)} ou permita hora extra no cadastro.");
        }
    }

    static TimeSpan Min(TimeSpan a, TimeSpan b) => a < b ? a : b;
}

public sealed class JornadaSemanal() : Regra(Padrao)
{
    public static readonly DefinicaoRegra Padrao = new("clt.jornada-semanal", "Jornada semanal",
        "CF art. 7º XIII; CLT arts. 58-A e 59", NivelRegra.Obrigatoria);

    public override IEnumerable<Violacao> Validar(ContextoValidacao ctx)
    {
        foreach (var f in ctx.Funcionarios.Where(f => f.Regime != Regime.DozePorTrintaESeis))
        {
            var limite = f.Contrato == TipoContrato.TempoParcial
                // até 30h sem extra, ou até 26h com até 6h extras
                ? (f.CargaSemanal > TimeSpan.FromHours(26) ? TimeSpan.FromHours(30) : f.CargaSemanal + TimeSpan.FromHours(6))
                : f.CargaSemanal + (f.PermiteHoraExtra ? f.LimiteHoraExtraSemanal : TimeSpan.Zero);

            foreach (var semana in ctx.TrabalhoDe(f).GroupBy(a => FimDaSemana(a.Data)))
            {
                var total = semana.Aggregate(TimeSpan.Zero, (s, a) => s + a.HorasComputadas);
                if (total > limite && ctx.NoPeriodo(semana.Key))
                    yield return Violacao(f, semana.Key,
                        $"{f.Nome} somaria {Horas(total)} na semana até {Dia(semana.Key)} (limite {Horas(limite)}).",
                        $"Reduza {Horas(total - limite)} na semana de {f.Nome} ou redistribua os turnos.");
            }
        }
    }

    /// Semanas de segunda a domingo.
    static DateOnly FimDaSemana(DateOnly d) => d.AddDays((7 - (int)d.DayOfWeek) % 7);
}

public sealed class Intrajornada() : Regra(Padrao)
{
    public static readonly DefinicaoRegra Padrao = new("clt.intrajornada", "Intervalo intrajornada",
        "CLT art. 71", NivelRegra.Obrigatoria);

    public TimeSpan IntervaloMaximo { get; init; } = TimeSpan.FromHours(2);

    public override IEnumerable<Violacao> Validar(ContextoValidacao ctx)
    {
        var seis = TimeSpan.FromHours(6);
        var quatro = TimeSpan.FromHours(4);
        foreach (var f in ctx.Funcionarios)
        foreach (var a in ctx.TrabalhoDe(f).Where(a => ctx.NoPeriodo(a.Data)))
        {
            var (minimo, maximo) = a.Trabalhado > seis ? (TimeSpan.FromHours(1), IntervaloMaximo)
                : a.Trabalhado > quatro ? (TimeSpan.FromMinutes(15), TimeSpan.MaxValue)
                : (TimeSpan.Zero, TimeSpan.MaxValue);

            if (a.Intervalo < minimo)
                yield return Violacao(f, a.Data,
                    $"{f.Nome} trabalharia {Horas(a.Trabalhado)} em {Dia(a.Data)} com intervalo de {Minutos(a.Intervalo)} (mínimo {Minutos(minimo)}).",
                    $"Defina um intervalo de pelo menos {Minutos(minimo)}.");
            else if (a.Intervalo > maximo)
                yield return Violacao(f, a.Data,
                    $"{f.Nome} teria intervalo de {Minutos(a.Intervalo)} em {Dia(a.Data)} (máximo {Minutos(maximo)} sem acordo).",
                    "Reduza o intervalo ou registre o acordo que permite ampliá-lo.");
        }
    }

    static string Minutos(TimeSpan t) => t < TimeSpan.FromHours(1) ? $"{(int)t.TotalMinutes} min" : Horas(t);
}

public sealed class Interjornada() : Regra(Padrao)
{
    public static readonly DefinicaoRegra Padrao = new("clt.interjornada", "Descanso entre jornadas",
        "CLT art. 66", NivelRegra.Obrigatoria);

    public TimeSpan Minimo { get; init; } = TimeSpan.FromHours(11);

    public override IEnumerable<Violacao> Validar(ContextoValidacao ctx)
    {
        foreach (var f in ctx.Funcionarios)
        foreach (var (antes, depois) in ctx.TrabalhoDe(f).Zip(ctx.TrabalhoDe(f).Skip(1)))
        {
            var descanso = depois.InicioEm - antes.FimEm;
            if (descanso < Minimo && ctx.NoPeriodo(depois.Data))
                yield return Violacao(f, depois.Data,
                    $"{f.Nome} terminaria às {Hora(antes.Fim)} de {Dia(DateOnly.FromDateTime(antes.FimEm))} e começaria às {Hora(depois.Inicio)} de {Dia(depois.Data)}: descanso de {Horas(descanso)} (mínimo {Horas(Minimo)}).",
                    $"Mude o turno de {Dia(depois.Data)} para começar a partir de {Hora(TimeOnly.FromDateTime(antes.FimEm + Minimo))}.");
        }
    }
}

public sealed class DiasConsecutivos() : Regra(Padrao)
{
    public static readonly DefinicaoRegra Padrao = new("clt.dsr-semanal", "Repouso semanal remunerado",
        "CLT art. 67; Lei 605/1949; OJ 410 SDI-1 TST", NivelRegra.Obrigatoria);

    public int MaximoSeguidos { get; init; } = 6;

    public override IEnumerable<Violacao> Validar(ContextoValidacao ctx)
    {
        foreach (var f in ctx.Funcionarios)
        {
            var seguidos = 0;
            DateOnly? anterior = null;
            foreach (var dia in ctx.TrabalhoDe(f).Select(a => a.Data).Distinct())
            {
                seguidos = anterior?.AddDays(1) == dia ? seguidos + 1 : 1;
                anterior = dia;
                // avisa uma vez por sequência: no 7º dia, ou no 1º dia do período se ela veio de antes
                var primeiroNoPeriodo = ctx.NoPeriodo(dia) && !ctx.NoPeriodo(dia.AddDays(-1));
                if (seguidos > MaximoSeguidos && ctx.NoPeriodo(dia) && (seguidos == MaximoSeguidos + 1 || primeiroNoPeriodo))
                    yield return Violacao(f, dia,
                        $"{f.Nome} trabalharia {seguidos} dias seguidos até {Dia(dia)} sem o descanso semanal.",
                        $"Dê uma folga a {f.Nome} até {Dia(dia.AddDays(-1))}.");
            }
        }
    }
}

public sealed class DomingoDeFolga() : Regra(Padrao)
{
    public static readonly DefinicaoRegra Padrao = new("clt.domingo", "Folga em domingo",
        "Lei 10.101/2000 art. 6º (comércio); Portaria MTP 671/2021 (demais)", NivelRegra.Obrigatoria);

    public int SemanasComercio { get; init; } = 3;
    public int SemanasOutros { get; init; } = 7;

    public override IEnumerable<Violacao> Validar(ContextoValidacao ctx)
    {
        var n = ctx.Empresa.Ramo == RamoAtividade.Comercio ? SemanasComercio : SemanasOutros;
        foreach (var f in ctx.Funcionarios.Where(f => f.Regime != Regime.DozePorTrintaESeis))
        {
            var domingos = ctx.TrabalhoDe(f).Select(a => a.Data).Where(d => d.DayOfWeek == DayOfWeek.Sunday).ToHashSet();
            foreach (var domingo in domingos.Where(ctx.NoPeriodo).Order())
            {
                var janela = Enumerable.Range(0, n).Select(i => domingo.AddDays(-7 * i));
                // aponta só o domingo que completa a sequência, não os seguintes
                if (janela.All(domingos.Contains) && !domingos.Contains(domingo.AddDays(-7 * n)))
                    yield return Violacao(f, domingo,
                        $"{f.Nome} trabalharia {n} domingos seguidos até {Dia(domingo)}.",
                        $"Dê a {f.Nome} uma folga em um desses domingos.");
            }
        }
    }
}

public sealed class FeriadoTrabalhado() : Regra(Padrao)
{
    public static readonly DefinicaoRegra Padrao = new("clt.feriado", "Feriado trabalhado",
        "Lei 605/1949 art. 9º; Súmula 146 TST", NivelRegra.Alerta);

    public override IEnumerable<Violacao> Validar(ContextoValidacao ctx)
    {
        var feriados = ctx.Feriados.Where(x => x.Considerado).ToDictionary(x => x.Data);
        foreach (var f in ctx.Funcionarios.Where(f => f.Regime != Regime.DozePorTrintaESeis))
        foreach (var a in ctx.TrabalhoDe(f))
            if (ctx.NoPeriodo(a.Data) && feriados.TryGetValue(a.Data, out var feriado))
                yield return Violacao(f, a.Data,
                    $"{f.Nome} está na escala no feriado {feriado.Nome} ({Dia(a.Data)}).",
                    "Garanta folga compensatória na mesma semana ou o pagamento em dobro.");
    }
}

public sealed class MenorTrabalhoNoturno() : Regra(Padrao)
{
    public static readonly DefinicaoRegra Padrao = new("clt.menor-noturno", "Menor sem trabalho noturno",
        "CF art. 7º XXXIII; CLT art. 404", NivelRegra.Obrigatoria);

    public override IEnumerable<Violacao> Validar(ContextoValidacao ctx)
    {
        foreach (var f in ctx.Funcionarios.Where(f => f.MenorDeIdade))
        foreach (var a in ctx.TrabalhoDe(f).Where(a => ctx.NoPeriodo(a.Data) && a.Noturno > TimeSpan.Zero))
            yield return Violacao(f, a.Data,
                $"{f.Nome} é menor de idade e trabalharia entre 22h e 5h em {Dia(a.Data)}.",
                $"Termine o turno de {f.Nome} até 22:00.");
    }
}

public sealed class EscaladoDuranteAfastamento() : Regra(Padrao)
{
    public static readonly DefinicaoRegra Padrao = new("op.afastamento", "Escalado durante afastamento",
        "Regra operacional (ESPEC §7.1)", NivelRegra.Obrigatoria);

    public override IEnumerable<Violacao> Validar(ContextoValidacao ctx)
    {
        foreach (var f in ctx.Funcionarios)
        {
            var afastamentos = ctx.Ocorrencias.Where(o => o.FuncionarioId == f.Id && o.Afasta).ToList();
            foreach (var a in ctx.TrabalhoDe(f))
                if (ctx.NoPeriodo(a.Data) && afastamentos.Any(o => o.Cobre(a.Data)))
                    // sem o tipo da ocorrência: o gestor só precisa saber que há ausência aprovada (LGPD)
                    yield return Violacao(f, a.Data,
                        $"{f.Nome} tem ausência aprovada em {Dia(a.Data)} e está na escala.",
                        "Remaneje o turno para um substituto.");
        }
    }
}

public sealed class CoberturaMinima() : Regra(Padrao)
{
    public static readonly DefinicaoRegra Padrao = new("op.cobertura", "Cobertura mínima",
        "Regra operacional (ESPEC §6)", NivelRegra.Alerta, SeveridadeFixa: Severidade.Critico);

    public override IEnumerable<Violacao> Validar(ContextoValidacao ctx)
    {
        for (var dia = ctx.Inicio; dia <= ctx.Fim; dia = dia.AddDays(1))
        {
            if (ctx.DiasFechados.Contains(dia)) continue;

            foreach (var d in Demanda.Aplicaveis(ctx.Demandas, dia))
            {
                var escalados = ctx.Alocacoes.Count(a => a.Trabalha && a.Data == dia && a.TurnoId == d.TurnoId
                    && (d.FuncaoId is null || a.FuncaoId == d.FuncaoId));
                var falta = d.Minimo - escalados;
                if (falta <= 0) continue;

                var turno = ctx.Turnos.FirstOrDefault(t => t.Id == d.TurnoId)?.Nome ?? "Turno";
                var pessoas = falta == 1 ? "falta 1 pessoa" : $"faltam {falta} pessoas";
                var funcao = ctx.Funcoes.FirstOrDefault(x => x.Id == d.FuncaoId) is { } fn ? $" na função {fn.Nome}" : "";
                yield return Violacao(null, dia, $"{Dia(dia)} · {turno}: {pessoas}{funcao}.",
                    "Veja os substitutos disponíveis para este turno.");
            }
        }
    }
}

public sealed class PreferenciaDoFuncionario() : Regra(Padrao)
{
    public static readonly DefinicaoRegra Padrao = new("pref.disponibilidade", "Preferência do funcionário",
        "Preferência cadastrada (ESPEC §2.3)", NivelRegra.Preferencia);

    public override IEnumerable<Violacao> Validar(ContextoValidacao ctx)
    {
        foreach (var f in ctx.Funcionarios)
        foreach (var a in ctx.TrabalhoDe(f).Where(a => ctx.NoPeriodo(a.Data)))
        {
            var turno = new Turno(Guid.Empty, "", a.Inicio, a.Fim, a.Intervalo);
            if ((f.Disponibilidades ?? []).Any(x => x.Tipo == TipoRestricao.Preferencia && x.ValeEm(a.Data) && !x.Comporta(turno)))
                yield return Violacao(f, a.Data,
                    $"{f.Nome} prefere outro horário em {Dia(a.Data)} ({Hora(a.Inicio)}–{Hora(a.Fim)}).",
                    $"Se possível, troque {f.Nome} com alguém do turno preferido.");
        }
    }
}
