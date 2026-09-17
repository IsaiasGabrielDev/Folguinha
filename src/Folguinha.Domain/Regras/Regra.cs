namespace Folguinha.Domain.Regras;

/// Obrigatória impede publicar; Alerta exige justificativa; Preferência o gerador tenta respeitar (ESPEC §2).
public enum NivelRegra { Obrigatoria, Alerta, Preferencia }

public enum Severidade { Bloqueio, Critico, Atencao, Informativo }

/// Metadados versionados de uma regra (ESPEC §7.2). Alterar uma regra = nova versão, sem mudar escalas encerradas.
public sealed record DefinicaoRegra(
    string Id,
    string Nome,
    string Fundamento,
    NivelRegra Nivel,
    int Versao = 1,
    DateOnly? VigenteDesde = null,
    DateOnly? VigenteAte = null,
    Severidade? SeveridadeFixa = null,
    string? Responsavel = null)
{
    public Severidade Severidade => SeveridadeFixa ?? Nivel switch
    {
        NivelRegra.Obrigatoria => Severidade.Bloqueio,
        NivelRegra.Alerta => Severidade.Atencao,
        _ => Severidade.Informativo,
    };

    public bool VigenteEm(DateOnly dia) => (VigenteDesde ?? DateOnly.MinValue) <= dia && dia <= (VigenteAte ?? DateOnly.MaxValue);
}

/// `FuncionarioId` nulo = problema da escala como um todo (ex.: cobertura).
public sealed record Violacao(
    string RegraId,
    Severidade Severidade,
    string Mensagem,
    string? Sugestao,
    Guid? FuncionarioId,
    DateOnly? Data);

/// Tudo que as regras enxergam. `Alocacoes` pode incluir dias antes de `Inicio` (histórico);
/// só são apontadas violações com data dentro de [Inicio, Fim].
public sealed record ContextoValidacao(
    Empresa Empresa,
    DateOnly Inicio,
    DateOnly Fim,
    IReadOnlyList<Funcionario> Funcionarios,
    IReadOnlyList<Alocacao> Alocacoes,
    IReadOnlyList<Feriado> Feriados,
    IReadOnlyList<Ocorrencia> Ocorrencias,
    IReadOnlyList<Demanda> Demandas,
    IReadOnlyList<Turno> Turnos,
    IReadOnlyList<Funcao> Funcoes)
{
    /// Dias em que a unidade não abre (a cobertura não é cobrada).
    public IReadOnlySet<DateOnly> DiasFechados { get; init; } = new HashSet<DateOnly>();

    public bool NoPeriodo(DateOnly dia) => dia >= Inicio && dia <= Fim;

    /// Dias de trabalho do funcionário em ordem cronológica.
    public IEnumerable<Alocacao> TrabalhoDe(Funcionario f) =>
        Alocacoes.Where(a => a.FuncionarioId == f.Id && a.Trabalha).OrderBy(a => a.InicioEm);
}

public abstract class Regra
{
    DefinicaoRegra _definicao;

    protected Regra(DefinicaoRegra padrao) => _definicao = padrao;

    public DefinicaoRegra Definicao { get => _definicao; init => _definicao = value; }

    /// Cópia da regra com outra definição (nível/versão/vigência configurados pelo usuário).
    public Regra ComDefinicao(DefinicaoRegra definicao)
    {
        var copia = (Regra)MemberwiseClone();
        copia._definicao = definicao;
        return copia;
    }

    public abstract IEnumerable<Violacao> Validar(ContextoValidacao ctx);

    protected Violacao Violacao(Funcionario? f, DateOnly dia, string mensagem, string? sugestao = null) =>
        new(Definicao.Id, Definicao.Severidade, mensagem, sugestao, f?.Id, dia);
}

public static class Validador
{
    public static IEnumerable<Violacao> Validar(ContextoValidacao ctx, IEnumerable<Regra> regras) =>
        regras.Where(r => r.Definicao.VigenteEm(ctx.Inicio))
            .SelectMany(r => r.Validar(ctx))
            .Where(v => v.Data is not { } d || ctx.NoPeriodo(d))
            .OrderBy(v => v.Severidade).ThenBy(v => v.Data);
}

public static class CatalogoRegras
{
    public static IReadOnlyList<Regra> Padrao() =>
    [
        new JornadaDiaria(), new JornadaSemanal(), new Intrajornada(), new Interjornada(),
        new DiasConsecutivos(), new DomingoDeFolga(), new FeriadoTrabalhado(), new MenorTrabalhoNoturno(),
        new EscaladoDuranteAfastamento(), new CoberturaMinima(), new PreferenciaDoFuncionario(),
    ];
}
