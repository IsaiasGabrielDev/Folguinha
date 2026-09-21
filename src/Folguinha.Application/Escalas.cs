using Folguinha.Domain;
using Folguinha.Domain.Geracao;
using Folguinha.Domain.Regras;
using static Folguinha.Domain.Texto;

namespace Folguinha.Application;

/// ESPEC §14. `EmValidacao` = alterações em andamento sobre uma escala já publicada;
/// `Validada` é mostrado pela tela quando o rascunho não tem bloqueios.
public enum EstadoEscala { Rascunho, EmValidacao, Validada, Publicada, Alterada, Encerrada }

/// Escala de um período. Versões publicadas nunca mudam; ajustes vão para o rascunho até a próxima publicação.
public sealed record Escala(Guid Id, DateOnly Inicio, DateOnly Fim)
{
    public IReadOnlyList<VersaoEscala> Versoes { get; init; } = [];
    public IReadOnlyList<Alocacao>? Rascunho { get; init; }
    public bool Encerrada { get; init; }

    public VersaoEscala? UltimaVersao => Versoes.Count > 0 ? Versoes[^1] : null;

    public IReadOnlyList<Alocacao> Atual => Rascunho ?? UltimaVersao?.Alocacoes ?? [];

    public EstadoEscala Estado =>
        Encerrada ? EstadoEscala.Encerrada
        : Rascunho is not null ? (Versoes.Count == 0 ? EstadoEscala.Rascunho : EstadoEscala.EmValidacao)
        : Versoes.Count > 1 ? EstadoEscala.Alterada
        : EstadoEscala.Publicada;

    public bool Cobre(DateOnly dia) => dia >= Inicio && dia <= Fim;

    public string Nome => $"{Inicio:dd/MM/yyyy} a {Fim:dd/MM/yyyy}";
}

/// Uma publicação (ESPEC §12.3 e §14).
public sealed record VersaoEscala(int Numero, DateTimeOffset PublicadaEm, string Autor, string Motivo, IReadOnlyList<Alocacao> Alocacoes)
{
    public IReadOnlyList<Guid> Afetados { get; init; } = [];
    public IReadOnlyList<AlertaAceito> AlertasAceitos { get; init; } = [];
    public IReadOnlyList<RegraAplicada> RegrasVerificadas { get; init; } = [];
    /// Todos os avisos que existiam na publicação (inclusive informativos).
    public IReadOnlyList<Violacao> Violacoes { get; init; } = [];
}

public sealed record AlertaAceito(string RegraId, Guid? FuncionarioId, DateOnly? Data, string Mensagem, string Justificativa);

public sealed record RegraAplicada(string Id, string Nome, string Fundamento, NivelRegra Nivel, int Versao);

public sealed record Mudanca(Guid FuncionarioId, DateOnly Data, Alocacao? Antes, Alocacao? Depois);

public sealed record ResultadoAjuste(DadosDaUnidade Dados, IReadOnlyList<Violacao> Violacoes, IReadOnlyList<Violacao> Novas, IReadOnlyList<Violacao> Resolvidas);

/// `Pendencias`: bloqueios, ou alertas sem justificativa, que impediram a publicação.
public sealed record ResultadoPublicacao(DadosDaUnidade Dados, bool Publicado, IReadOnlyList<Violacao> Pendencias);

public sealed class EscalaException(string mensagem) : InvalidOperationException(mensagem);

/// Casos de uso da escala. Funções puras sobre DadosDaUnidade: quem chama salva o resultado.
public static class Escalas
{
    public static IReadOnlyList<Regra> Regras(DadosDaUnidade dados, DateOnly data) =>
        ConfiguracaoDeRegras.Aplicar(dados.RegrasConfiguradas, data);

    public static IReadOnlySet<DateOnly> DiasFechados(DadosDaUnidade dados, DateOnly inicio, DateOnly fim)
    {
        var fechados = new HashSet<DateOnly>(dados.DatasFechadas.Where(d => d >= inicio && d <= fim));
        if (dados.Funcionamento.Count > 0)
            for (var d = inicio; d <= fim; d = d.AddDays(1))
                if (dados.Funcionamento.All(p => p.Dia != d.DayOfWeek)) fechados.Add(d);
        return fechados;
    }

    /// Alocações anteriores ao início (outras escalas + situação inicial de quem ainda não tem histórico).
    public static IReadOnlyList<Alocacao> Historico(DadosDaUnidade dados, DateOnly antesDe, Guid? ignorarEscala = null)
    {
        var reais = dados.Escalas.Where(e => e.Id != ignorarEscala && e.Inicio < antesDe)
            .SelectMany(e => e.Atual).Where(a => a.Data < antesDe).ToList();
        var comHistorico = reais.Select(a => a.FuncionarioId).ToHashSet();
        var presumidos = dados.Funcionarios.Where(f => !comHistorico.Contains(f.Id))
            .SelectMany(f => f.HistoricoPresumido(antesDe, dados.Turnos, dados.Feriados));
        return [.. reais, .. presumidos];
    }

    public static ContextoValidacao Contexto(DadosDaUnidade dados, Escala escala, IReadOnlyList<Alocacao>? alocacoes = null) =>
        new(Empresa(dados), escala.Inicio, escala.Fim, dados.Funcionarios,
            [.. Historico(dados, escala.Inicio, escala.Id), .. alocacoes ?? escala.Atual],
            dados.Feriados, dados.Ocorrencias, dados.Demandas, dados.Turnos, dados.Funcoes)
        {
            DiasFechados = DiasFechados(dados, escala.Inicio, escala.Fim),
        };

    public static IReadOnlyList<Violacao> Validar(DadosDaUnidade dados, Escala escala, IReadOnlyList<Alocacao>? alocacoes = null) =>
        [.. Validador.Validar(Contexto(dados, escala, alocacoes), Regras(dados, escala.Inicio))];

    /// Cria o rascunho do período, ou regera o rascunho de uma escala existente mantendo o que está travado.
    public static (DadosDaUnidade Dados, Escala Escala, IReadOnlyList<Violacao> Violacoes) Gerar(
        DadosDaUnidade dados, DateOnly inicio, DateOnly fim)
    {
        if (fim < inicio) throw new EscalaException("O fim do período vem antes do início.");
        if (dados.Funcionarios.Count == 0) throw new EscalaException("Cadastre os funcionários antes de gerar a escala.");
        if (dados.Turnos.Count == 0) throw new EscalaException("Cadastre os turnos antes de gerar a escala.");

        var existente = dados.Escalas.FirstOrDefault(e => e.Inicio == inicio && e.Fim == fim);
        if (existente is { Encerrada: true }) throw new EscalaException("Essa escala está encerrada e não pode ser gerada de novo.");
        if (dados.Escalas.FirstOrDefault(e => e.Id != existente?.Id && e.Inicio <= fim && e.Fim >= inicio) is { } conflito)
            throw new EscalaException($"Já existe a escala de {conflito.Nome} nesse período.");

        var resultado = GeradorEscala.Gerar(new EntradaGeracao
        {
            Empresa = Empresa(dados),
            Inicio = inicio,
            Fim = fim,
            Funcionarios = dados.Funcionarios,
            Turnos = dados.Turnos,
            Funcoes = dados.Funcoes,
            Demandas = dados.Demandas,
            Feriados = dados.Feriados,
            Ocorrencias = dados.Ocorrencias,
            Funcionamento = dados.Funcionamento,
            DatasFechadas = dados.DatasFechadas,
            // o gerador já soma a situação inicial de quem não tem histórico
            Historico = dados.Escalas.Where(e => e.Id != existente?.Id && e.Inicio < inicio).SelectMany(e => e.Atual).Where(a => a.Data < inicio).ToList(),
            Travadas = existente?.Atual.Where(a => a.Travada).ToList() ?? [],
            Regras = Regras(dados, inicio),
            RodizioFeriados = dados.RodizioFeriados,
            CompensarFeriado = dados.CompensarFeriado,
            RodizioTurnos = dados.RodizioTurnos,
        });

        var escala = (existente ?? new Escala(Guid.NewGuid(), inicio, fim)) with { Rascunho = resultado.Alocacoes };
        var novos = dados.ComEscala(escala);
        // os avisos do gerador (ex.: rodízio de feriado) somam-se à validação completa
        var avisos = resultado.Violacoes.Where(v => v.RegraId.StartsWith("ger.", StringComparison.Ordinal));
        return (novos, escala, [.. Validar(novos, escala), .. avisos]);
    }

    /// Ajuste manual: vira rascunho, fica travado e é revalidado na hora (ESPEC §8.1).
    public static ResultadoAjuste Ajustar(DadosDaUnidade dados, Guid escalaId, Alocacao nova)
    {
        var escala = Editavel(dados, escalaId);
        if (!escala.Cobre(nova.Data)) throw new EscalaException("Esse dia não faz parte da escala.");

        var antes = Validar(dados, escala);
        var alocacoes = escala.Atual.Where(a => !(a.FuncionarioId == nova.FuncionarioId && a.Data == nova.Data))
            .Append(nova with { Travada = true }).ToList();
        var novos = dados.ComEscala(escala with { Rascunho = alocacoes });
        var depois = Validar(novos, novos.Escala(escalaId)!);

        var chavesAntes = antes.Select(Chave).ToHashSet();
        var chavesDepois = depois.Select(Chave).ToHashSet();
        return new ResultadoAjuste(novos, depois,
            [.. depois.Where(v => !chavesAntes.Contains(Chave(v)))],
            [.. antes.Where(v => !chavesDepois.Contains(Chave(v)))]);
    }

    /// Desfaz as alterações não publicadas (só para escalas que já têm versão).
    public static DadosDaUnidade DescartarRascunho(DadosDaUnidade dados, Guid escalaId)
    {
        var escala = Editavel(dados, escalaId);
        return escala.Versoes.Count == 0
            ? dados with { Escalas = [.. dados.Escalas.Where(e => e.Id != escalaId)] }
            : dados.ComEscala(escala with { Rascunho = null });
    }

    public static bool PrecisaJustificativa(Violacao v) => v.Severidade is Severidade.Critico or Severidade.Atencao;

    /// Identifica um aviso entre validações (para justificativas e comparação).
    public static string Chave(Violacao v) => $"{v.RegraId}|{v.FuncionarioId}|{v.Data:yyyy-MM-dd}|{v.Mensagem}";

    static string Chave(AlertaAceito a) => $"{a.RegraId}|{a.FuncionarioId}|{a.Data:yyyy-MM-dd}|{a.Mensagem}";

    /// Avisos que já foram justificados na última publicação. Sem isso o Painel reapresenta
    /// como problema cru exatamente o que o gestor acabou de revisar e assinar ao publicar.
    public static HashSet<string> Justificados(Escala e) =>
        [.. (e.UltimaVersao?.AlertasAceitos ?? []).Select(Chave)];

    public static ResultadoPublicacao Publicar(
        DadosDaUnidade dados, Guid escalaId, string motivo, IReadOnlyDictionary<string, string> justificativas, DateTimeOffset agora)
    {
        var escala = Editavel(dados, escalaId);
        if (escala.Rascunho is null) throw new EscalaException("Não há alterações para publicar.");

        var violacoes = Validar(dados, escala);
        var bloqueios = violacoes.Where(v => v.Severidade == Severidade.Bloqueio).ToList();
        if (bloqueios.Count > 0) return new ResultadoPublicacao(dados, false, bloqueios);

        string? Justificativa(Violacao v) => justificativas.TryGetValue(Chave(v), out var j) && !string.IsNullOrWhiteSpace(j) ? j.Trim() : null;
        var semJustificativa = violacoes.Where(v => PrecisaJustificativa(v) && Justificativa(v) is null).ToList();
        if (semJustificativa.Count > 0) return new ResultadoPublicacao(dados, false, semJustificativa);

        var anterior = escala.UltimaVersao?.Alocacoes ?? [];
        var versao = new VersaoEscala(escala.Versoes.Count + 1, agora, dados.NomeGestor,
            string.IsNullOrWhiteSpace(motivo) ? (escala.Versoes.Count == 0 ? "Primeira publicação" : "Ajustes") : motivo.Trim(),
            escala.Rascunho)
        {
            Afetados = [.. Diferencas(anterior, escala.Rascunho).Select(m => m.FuncionarioId).Distinct()],
            AlertasAceitos = [.. violacoes.Where(PrecisaJustificativa)
                .Select(v => new AlertaAceito(v.RegraId, v.FuncionarioId, v.Data, v.Mensagem, Justificativa(v)!))],
            RegrasVerificadas = [.. Regras(dados, escala.Inicio)
                .Select(r => new RegraAplicada(r.Definicao.Id, r.Definicao.Nome, r.Definicao.Fundamento, r.Definicao.Nivel, r.Definicao.Versao))],
            Violacoes = violacoes,
        };
        var publicada = escala with { Versoes = [.. escala.Versoes, versao], Rascunho = null };
        return new ResultadoPublicacao(dados.ComEscala(publicada), true, []);
    }

    public static DadosDaUnidade Encerrar(DadosDaUnidade dados, Guid escalaId)
    {
        var escala = Editavel(dados, escalaId);
        if (escala.Versoes.Count == 0) throw new EscalaException("Publique a escala antes de encerrá-la.");
        if (escala.Rascunho is not null) throw new EscalaException("Publique ou descarte as alterações antes de encerrar.");
        return dados.ComEscala(escala with { Encerrada = true });
    }

    /// Diferenças por funcionário e dia (ESPEC §14).
    public static IReadOnlyList<Mudanca> Diferencas(IReadOnlyList<Alocacao> antes, IReadOnlyList<Alocacao> depois)
    {
        var a = antes.ToDictionary(x => (x.FuncionarioId, x.Data));
        var d = depois.ToDictionary(x => (x.FuncionarioId, x.Data));
        return a.Keys.Union(d.Keys)
            .Select(k => new Mudanca(k.FuncionarioId, k.Data, a.GetValueOrDefault(k), d.GetValueOrDefault(k)))
            .Where(m => !MesmoConteudo(m.Antes, m.Depois))
            .OrderBy(m => m.Data).ThenBy(m => m.FuncionarioId)
            .ToList();
    }

    /// Descrição curta de uma alocação: "Manhã 08:00–16:20", "Folga", "Ausência".
    public static string Descrever(DadosDaUnidade dados, Alocacao? a) => a switch
    {
        null => "—",
        { Tipo: TipoAlocacao.Folga } => "Folga",
        { Tipo: TipoAlocacao.Ocorrencia } => "Ausência",
        _ => $"{dados.Turnos.FirstOrDefault(t => t.Id == a.TurnoId)?.Nome ?? "Trabalho"} {Hora(a.Inicio)}–{Hora(a.Fim)}",
    };

    static bool MesmoConteudo(Alocacao? x, Alocacao? y) =>
        x is null || y is null ? x == y : (x with { Travada = false }) == (y with { Travada = false });

    static Escala Editavel(DadosDaUnidade dados, Guid escalaId)
    {
        var escala = dados.Escala(escalaId) ?? throw new EscalaException("Escala não encontrada.");
        return escala.Encerrada ? throw new EscalaException("Essa escala está encerrada.") : escala;
    }

    static Empresa Empresa(DadosDaUnidade dados) =>
        dados.Empresa ?? throw new EscalaException("Cadastre a empresa antes de montar a escala.");
}
