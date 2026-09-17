using System.Globalization;
using Folguinha.Application;
using Folguinha.Domain;
using Folguinha.Domain.Regras;
using static Folguinha.Domain.Texto;

namespace Folguinha.Infrastructure.Planilhas;

public sealed record OpcoesExportacao(bool Individuais = true, bool Consolidada = true, bool Relatorio = true);

/// Planilhas da escala (ESPEC §12): calendário e lista consolidados, uma aba por funcionário e o relatório de validação.
/// Exporta a versão pedida; sem versão, exporta o estado atual (rascunho, se houver).
public static class ExportacaoEscala
{
    public const string AbaCalendario = "Calendário";
    public const string AbaLista = "Lista";
    public const string AbaValidacao = "Validação";

    public static byte[] Gerar(DadosDaUnidade dados, Escala escala, OpcoesExportacao opcoes, VersaoEscala? versao = null)
    {
        var alocacoes = versao?.Alocacoes ?? escala.Atual;
        var rotulo = RotuloVersao(escala, versao);
        var abas = new List<Aba>();
        if (opcoes.Consolidada)
        {
            abas.Add(Calendario(dados, escala, alocacoes));
            abas.Add(Lista(dados, escala, alocacoes));
        }
        if (opcoes.Individuais)
            abas.AddRange(dados.Funcionarios.OrderBy(f => f.Nome, StringComparer.CurrentCulture)
                .Select(f => Individual(dados, escala, alocacoes, f, rotulo)));
        if (opcoes.Relatorio)
            abas.Add(Relatorio(dados, escala, versao, alocacoes, rotulo));
        if (abas.Count == 0) throw new EscalaException("Escolha ao menos uma planilha para exportar.");
        return PlanilhaXlsx.Gerar(abas);
    }

    public static string NomeArquivo(DadosDaUnidade dados, Escala escala, VersaoEscala? versao)
    {
        var loja = new string((dados.Empresa?.Nome ?? "folguinha").Select(c => char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : '-').ToArray()).Trim('-');
        var sufixo = versao is null && escala.Rascunho is not null ? "rascunho" : $"v{(versao ?? escala.UltimaVersao)?.Numero}";
        return $"escala-{loja}-{escala.Inicio:yyyy-MM-dd}-a-{escala.Fim:yyyy-MM-dd}-{sufixo}.xlsx";
    }

    // ---------- consolidadas ----------

    static Aba Calendario(DadosDaUnidade dados, Escala escala, IReadOnlyList<Alocacao> alocacoes)
    {
        var dias = Dias(escala).ToList();
        var porChave = alocacoes.ToDictionary(a => (a.FuncionarioId, a.Data));
        var feriados = Feriados(dados);
        IReadOnlyList<Celula> cabecalho =
        [
            new("Funcionário", EstiloCelula.Cabecalho), new("Setor", EstiloCelula.Cabecalho), new("Função", EstiloCelula.Cabecalho),
            .. dias.Select(d => new Celula(Dia(d) + (feriados.ContainsKey(d) ? " *" : ""), EstiloCelula.Cabecalho)),
            new("Horas", EstiloCelula.Cabecalho), new("Folgas", EstiloCelula.Cabecalho),
        ];
        var linhas = new List<IReadOnlyList<Celula>> { cabecalho };
        foreach (var f in Ordenados(dados))
        {
            var minhas = dias.Select(d => porChave.GetValueOrDefault((f.Id, d))).ToList();
            linhas.Add(
            [
                new(f.Nome, EstiloCelula.Negrito), new(f.Setor ?? ""), new(Funcoes(dados, f)),
                .. minhas.Select(a => Codigo(dados, a)),
                new(Horas(minhas.OfType<Alocacao>().Where(a => a.Trabalha).Aggregate(TimeSpan.Zero, (t, a) => t + a.HorasComputadas))),
                new(minhas.Count(a => a is { Tipo: TipoAlocacao.Folga })),
            ]);
        }
        linhas.Add([]);
        linhas.Add([new("Legenda: sigla do turno = trabalho · F = folga · A = ausência aprovada · * = feriado")]);
        return new Aba(AbaCalendario)
        {
            Linhas = linhas,
            Larguras = [22, 14, 14, .. dias.Select(_ => 10.0), 9, 8],
            Filtro = true,
            CongelarCabecalho = true,
        };
    }

    static Aba Lista(DadosDaUnidade dados, Escala escala, IReadOnlyList<Alocacao> alocacoes)
    {
        var feriados = Feriados(dados);
        var linhas = new List<IReadOnlyList<Celula>>
        {
            Cabecalho("Funcionário", "Matrícula", "Setor", "Função", "Data", "Dia", "Situação", "Turno", "Entrada", "Saída", "Horas", "Observações"),
        };
        foreach (var a in alocacoes.Where(a => escala.Cobre(a.Data)).OrderBy(a => a.Data).ThenBy(a => dados.Funcionario(a.FuncionarioId)?.Nome))
        {
            if (dados.Funcionario(a.FuncionarioId) is not { } f) continue;
            linhas.Add(
            [
                new(f.Nome), new(f.Matricula ?? ""), new(f.Setor ?? ""), new(Funcao(dados, a, f)),
                new(a.Data.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)), new(DiaDaSemana(a.Data)),
                new(Situacao(a), Estilo(dados, a)), new(a.Trabalha ? NomeTurno(dados, a) : ""),
                new(a.Trabalha ? Hora(a.Inicio) : ""), new(a.Trabalha ? Hora(a.Fim) : ""),
                a.Trabalha ? new Celula(Math.Round(a.HorasComputadas.TotalHours, 2)) : new Celula(null),
                new(Observacoes(dados, a, feriados)),
            ]);
        }
        return new Aba(AbaLista)
        {
            Linhas = linhas,
            Larguras = [22, 11, 14, 14, 11, 9, 12, 12, 8, 8, 7, 30],
            Filtro = true,
            CongelarCabecalho = true,
        };
    }

    // ---------- individual (§12.1) ----------

    static Aba Individual(DadosDaUnidade dados, Escala escala, IReadOnlyList<Alocacao> alocacoes, Funcionario f, string rotulo)
    {
        var feriados = Feriados(dados);
        var minhas = alocacoes.Where(a => a.FuncionarioId == f.Id && escala.Cobre(a.Data)).ToDictionary(a => a.Data);
        List<IReadOnlyList<Celula>> linhas =
        [
            [new("Funcionário", EstiloCelula.Negrito), new(f.Nome)],
            [new("Matrícula", EstiloCelula.Negrito), new(f.Matricula ?? "—")],
            [new("Cargo / função", EstiloCelula.Negrito), new(string.Join(" · ", new[] { f.Cargo, Funcoes(dados, f) }.Where(x => !string.IsNullOrWhiteSpace(x))))],
            [new("Unidade / setor", EstiloCelula.Negrito), new(string.Join(" · ", new[] { dados.Empresa?.Nome, dados.NomeUnidade, f.Setor }.Where(x => !string.IsNullOrWhiteSpace(x))))],
            [new("Período", EstiloCelula.Negrito), new(escala.Nome)],
            [new("Versão", EstiloCelula.Negrito), new(rotulo)],
            [],
            Cabecalho("Data", "Dia", "Situação", "Entrada", "Início intervalo", "Fim intervalo", "Saída", "Total do dia", "Domingo/feriado", "Observações"),
        ];

        TimeSpan totalSemana = TimeSpan.Zero, total = TimeSpan.Zero, extras = TimeSpan.Zero;
        int folgas = 0, domingos = 0, feriadosTrabalhados = 0;
        foreach (var dia in Dias(escala))
        {
            var a = minhas.GetValueOrDefault(dia);
            var especial = string.Join(" · ", new[]
            {
                dia.DayOfWeek == DayOfWeek.Sunday ? "Domingo" : null,
                feriados.TryGetValue(dia, out var fer) ? fer.Nome : null,
            }.OfType<string>());

            if (a is { Trabalha: true })
            {
                var (iniIntervalo, fimIntervalo) = Intervalo(a);
                linhas.Add(
                [
                    new(dia.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)), new(DiaDaSemana(dia)),
                    new(NomeTurno(dados, a), Estilo(dados, a)), new(Hora(a.Inicio)),
                    new(iniIntervalo is { } i ? Hora(i) : ""), new(fimIntervalo is { } fi ? Hora(fi) : ""),
                    new(Hora(a.Fim)), new(Horas(a.HorasComputadas)),
                    new(especial, especial == "" ? EstiloCelula.Normal : EstiloCelula.Alerta), new(Observacoes(dados, a, feriados)),
                ]);
                totalSemana += a.HorasComputadas;
                total += a.HorasComputadas;
                if (dia.DayOfWeek == DayOfWeek.Sunday) domingos++;
                if (fer is not null) feriadosTrabalhados++;
            }
            else
            {
                if (a is { Tipo: TipoAlocacao.Folga }) folgas++;
                linhas.Add(
                [
                    new(dia.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)), new(DiaDaSemana(dia)),
                    new(a is null ? "—" : Situacao(a), a is null ? EstiloCelula.Normal : Estilo(dados, a)),
                    new(null), new(null), new(null), new(null), new(null),
                    new(especial), new(a is null ? "" : Observacoes(dados, a, feriados)),
                ]);
            }

            if (dia.DayOfWeek == DayOfWeek.Sunday || dia == escala.Fim)
            {
                var extra = f.Regime == Regime.DozePorTrintaESeis ? TimeSpan.Zero : Positivo(totalSemana - f.CargaSemanal);
                extras += extra;
                linhas.Add(
                [
                    new("Total da semana", EstiloCelula.Negrito), new(null), new(null), new(null), new(null), new(null), new(null),
                    new(Horas(totalSemana), EstiloCelula.Negrito),
                    new(extra > TimeSpan.Zero ? $"{Horas(extra)} extras previstas" : "", extra > TimeSpan.Zero ? EstiloCelula.Alerta : EstiloCelula.Normal),
                ]);
                totalSemana = TimeSpan.Zero;
            }
        }

        linhas.Add([]);
        linhas.Add([new("Total no período", EstiloCelula.Negrito), new(Horas(total))]);
        linhas.Add([new("Carga semanal", EstiloCelula.Negrito), new(Horas(f.CargaSemanal))]);
        linhas.Add([new("Horas extras previstas", EstiloCelula.Negrito), new(Horas(extras))]);
        linhas.Add([new("Folgas", EstiloCelula.Negrito), new(folgas)]);
        linhas.Add([new("Domingos trabalhados", EstiloCelula.Negrito), new(domingos)]);
        linhas.Add([new("Feriados trabalhados", EstiloCelula.Negrito), new(feriadosTrabalhados)]);
        linhas.Add([]);
        linhas.Add([new("Escala planejada: não comprova a jornada realizada (ESPEC §12.4).")]);

        return new Aba(f.Nome) { Linhas = linhas, Larguras = [16, 9, 16, 9, 10, 10, 9, 11, 18, 30] };
    }

    // ---------- relatório de validação (§12.3) ----------

    static Aba Relatorio(DadosDaUnidade dados, Escala escala, VersaoEscala? versao, IReadOnlyList<Alocacao> alocacoes, string rotulo)
    {
        var efetiva = versao ?? (escala.Rascunho is null ? escala.UltimaVersao : null);
        var violacoes = efetiva?.Violacoes ?? Escalas.Validar(dados, escala, alocacoes);
        var regras = efetiva?.RegrasVerificadas ?? [.. Escalas.Regras(dados, escala.Inicio)
            .Select(r => new RegraAplicada(r.Definicao.Id, r.Definicao.Nome, r.Definicao.Fundamento, r.Definicao.Nivel, r.Definicao.Versao))];
        string Nome(Guid? id) => id is { } x ? dados.Funcionario(x)?.Nome ?? "—" : "Equipe";

        List<IReadOnlyList<Celula>> linhas =
        [
            [new("Relatório de validação da escala", EstiloCelula.Negrito)],
            [new("Unidade"), new(string.Join(" · ", new[] { dados.Empresa?.Nome, dados.NomeUnidade }.Where(x => !string.IsNullOrWhiteSpace(x))))],
            [new("Período"), new(escala.Nome)],
            [new("Versão"), new(rotulo)],
            [new("Responsável pela geração"), new(dados.NomeGestor)],
            [new("Responsável pela aprovação"), new(efetiva?.Autor ?? "— (não publicada)")],
            [new("Publicação"), new(efetiva is null ? "—" : efetiva.PublicadaEm.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture))],
            [new("Motivo"), new(efetiva?.Motivo ?? "—")],
            [],
            [new("Regras verificadas", EstiloCelula.Negrito)],
            Cabecalho("Regra", "Fundamento", "Nível", "Versão", "Resultado"),
        ];
        foreach (var r in regras)
        {
            var qtd = violacoes.Count(v => v.RegraId == r.Id);
            linhas.Add([new(r.Nome), new(r.Fundamento), new(Nivel(r.Nivel)), new(r.Versao),
                new(qtd == 0 ? "Atendida" : $"{qtd} aviso(s)", qtd == 0 ? EstiloCelula.Tarde : EstiloCelula.Alerta)]);
        }

        linhas.Add([]);
        linhas.Add([new("Conflitos e alertas", EstiloCelula.Negrito)]);
        linhas.Add(Cabecalho("Gravidade", "Data", "Funcionário", "Mensagem", "Sugestão"));
        if (violacoes.Count == 0) linhas.Add([new("Nenhum")]);
        foreach (var v in violacoes)
            linhas.Add([new(Gravidade(v.Severidade), v.Severidade is Severidade.Bloqueio or Severidade.Critico ? EstiloCelula.Ocorrencia : EstiloCelula.Alerta),
                new(v.Data is { } d ? Dia(d) : ""), new(Nome(v.FuncionarioId)), new(v.Mensagem), new(v.Sugestao ?? "")]);

        linhas.Add([]);
        linhas.Add([new("Alertas aceitos com justificativa", EstiloCelula.Negrito)]);
        linhas.Add(Cabecalho("Data", "Funcionário", "Alerta", "Justificativa"));
        var aceitos = efetiva?.AlertasAceitos ?? [];
        if (aceitos.Count == 0) linhas.Add([new("Nenhum")]);
        foreach (var a in aceitos)
            linhas.Add([new(a.Data is { } d ? Dia(d) : ""), new(Nome(a.FuncionarioId)), new(a.Mensagem), new(a.Justificativa)]);

        if (efetiva is not null && escala.Versoes.Count > 1)
        {
            linhas.Add([]);
            linhas.Add([new("Histórico de versões", EstiloCelula.Negrito)]);
            linhas.Add(Cabecalho("Versão", "Publicada em", "Autor", "Motivo", "Afetados"));
            foreach (var v in escala.Versoes)
                linhas.Add([new(v.Numero), new(v.PublicadaEm.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture)), new(v.Autor), new(v.Motivo),
                    new(string.Join(", ", v.Afetados.Select(id => Nome(id))))]);
        }

        linhas.Add([]);
        linhas.Add([new("O Folguinha auxilia na validação, mas não substitui o Departamento Pessoal, o contador ou a assessoria jurídica. Convenções coletivas podem alterar as regras.")]);
        return new Aba(AbaValidacao) { Linhas = linhas, Larguras = [26, 40, 18, 50, 40] };
    }

    // ---------- auxiliares ----------

    static IEnumerable<DateOnly> Dias(Escala e)
    {
        for (var d = e.Inicio; d <= e.Fim; d = d.AddDays(1)) yield return d;
    }

    static IEnumerable<Funcionario> Ordenados(DadosDaUnidade dados) =>
        dados.Funcionarios.OrderBy(f => f.Setor ?? "").ThenBy(f => f.Nome, StringComparer.CurrentCulture);

    static Dictionary<DateOnly, Feriado> Feriados(DadosDaUnidade dados) =>
        dados.Feriados.Where(f => f.Considerado).GroupBy(f => f.Data).ToDictionary(g => g.Key, g => g.First());

    static IReadOnlyList<Celula> Cabecalho(params string[] titulos) => [.. titulos.Select(t => new Celula(t, EstiloCelula.Cabecalho))];

    static string RotuloVersao(Escala escala, VersaoEscala? versao)
    {
        var v = versao ?? (escala.Rascunho is null ? escala.UltimaVersao : null);
        return v is null
            ? "Rascunho — ainda não publicada"
            : $"Versão {v.Numero}, publicada em {v.PublicadaEm.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture)} por {v.Autor}";
    }

    static string DiaDaSemana(DateOnly d) => d.DayOfWeek switch
    {
        DayOfWeek.Monday => "segunda", DayOfWeek.Tuesday => "terça", DayOfWeek.Wednesday => "quarta",
        DayOfWeek.Thursday => "quinta", DayOfWeek.Friday => "sexta", DayOfWeek.Saturday => "sábado", _ => "domingo",
    };

    static string Situacao(Alocacao a) => a.Tipo switch
    {
        TipoAlocacao.Trabalho => "Trabalho",
        TipoAlocacao.Folga => "Folga",
        _ => "Ausência",
    };

    static string NomeTurno(DadosDaUnidade dados, Alocacao a) =>
        dados.Turnos.FirstOrDefault(t => t.Id == a.TurnoId)?.Nome ?? "Trabalho";

    static Celula Codigo(DadosDaUnidade dados, Alocacao? a) => a switch
    {
        null => new("—"),
        { Tipo: TipoAlocacao.Folga } => new("F", EstiloCelula.Folga),
        { Tipo: TipoAlocacao.Ocorrencia } => new("A", EstiloCelula.Ocorrencia),
        _ => new(Sigla(NomeTurno(dados, a)), Estilo(dados, a)),
    };

    static string Sigla(string nome) => nome.Length <= 3 ? nome : nome[..1].ToUpperInvariant();

    static EstiloCelula Estilo(DadosDaUnidade dados, Alocacao a) => a.Tipo switch
    {
        TipoAlocacao.Folga => EstiloCelula.Folga,
        TipoAlocacao.Ocorrencia => EstiloCelula.Ocorrencia,
        _ when a.Inicio.Hour >= 18 || a.Noturno > TimeSpan.FromHours(2) => EstiloCelula.Noite,
        _ when a.Inicio.Hour >= 11 => EstiloCelula.Tarde,
        _ => EstiloCelula.Manha,
    };

    static string Funcoes(DadosDaUnidade dados, Funcionario f) =>
        string.Join(", ", (f.Funcoes ?? []).Select(id => dados.Funcoes.FirstOrDefault(x => x.Id == id)?.Nome).OfType<string>());

    static string Funcao(DadosDaUnidade dados, Alocacao a, Funcionario f) =>
        dados.Funcoes.FirstOrDefault(x => x.Id == a.FuncaoId)?.Nome ?? Funcoes(dados, f);

    /// Sem motivo de afastamento (LGPD): só o que o gestor operacional precisa.
    static string Observacoes(DadosDaUnidade dados, Alocacao a, Dictionary<DateOnly, Feriado> feriados)
    {
        var itens = new List<string>();
        if (a.Tipo == TipoAlocacao.Ocorrencia) itens.Add("Ausência aprovada");
        if (a.Tipo == TipoAlocacao.Folga && dados.Ocorrencias.Any(o => o.FuncionarioId == a.FuncionarioId && o.Tipo == TipoOcorrencia.FolgaExtra && o.Cobre(a.Data)))
            itens.Add("Folga extra");
        if (a.Trabalha && feriados.TryGetValue(a.Data, out var f)) itens.Add($"Feriado trabalhado ({f.Nome}): compensar ou pagar em dobro");
        if (a.Travada && a.Tipo != TipoAlocacao.Ocorrencia) itens.Add("Ajuste manual");
        return string.Join("; ", itens);
    }

    /// Horário sugerido do intervalo: no meio da jornada, arredondado para 15 min.
    static (TimeOnly? Inicio, TimeOnly? Fim) Intervalo(Alocacao a)
    {
        if (a.Intervalo <= TimeSpan.Zero) return (null, null);
        var meio = TimeSpan.FromMinutes(Math.Round(((a.FimEm - a.InicioEm - a.Intervalo) / 2).TotalMinutes / 15) * 15);
        var inicio = a.Inicio.Add(meio);
        return (inicio, inicio.Add(a.Intervalo));
    }

    static TimeSpan Positivo(TimeSpan t) => t > TimeSpan.Zero ? t : TimeSpan.Zero;

    static string Nivel(NivelRegra n) => n switch
    {
        NivelRegra.Obrigatoria => "Obrigatória",
        NivelRegra.Alerta => "Alerta",
        _ => "Preferência",
    };

    static string Gravidade(Severidade s) => s switch
    {
        Severidade.Bloqueio => "Bloqueio",
        Severidade.Critico => "Crítico",
        Severidade.Atencao => "Atenção",
        _ => "Informativo",
    };
}
