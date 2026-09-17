using Folguinha.Domain;
using Folguinha.Domain.Regras;

namespace Folguinha.Domain.Tests;

/// Atalhos para montar cenários. Outubro/2026: dia 1 é quinta, domingos 4, 11, 18, 25.
static class Dados
{
    public static readonly Empresa Comercio = new(Guid.NewGuid(), "Loja Centro", RamoAtividade.Comercio);
    public static readonly Empresa Industria = new(Guid.NewGuid(), "Fábrica", RamoAtividade.Outro);

    public static DateOnly D(int dia, int mes = 10) => new(2026, mes, dia);

    public static Funcionario Func(
        string nome = "Diego",
        Regime regime = Regime.SeisPorUm,
        double cargaSemanal = 44,
        TipoContrato contrato = TipoContrato.Clt,
        bool menor = false,
        bool horaExtra = false,
        double limiteHoraExtraSemanal = 0) =>
        new(Guid.NewGuid(), nome, regime, TimeSpan.FromHours(cargaSemanal), contrato, menor, horaExtra,
            TimeSpan.FromHours(limiteHoraExtraSemanal));

    public static Alocacao Trab(Funcionario f, DateOnly dia, string inicio, string fim, int intervaloMin = 60,
        Guid? turno = null, Guid? funcao = null) =>
        Alocacao.Trabalho(f.Id, dia, TimeOnly.Parse(inicio), TimeOnly.Parse(fim), TimeSpan.FromMinutes(intervaloMin), turno, funcao);

    public static Alocacao Folga(Funcionario f, DateOnly dia) => Alocacao.Folga(f.Id, dia);

    public static ContextoValidacao Ctx(
        Funcionario[] funcionarios,
        Alocacao[] alocacoes,
        DateOnly? inicio = null,
        DateOnly? fim = null,
        Empresa? empresa = null,
        Feriado[]? feriados = null,
        Ocorrencia[]? ocorrencias = null,
        Demanda[]? demandas = null,
        Turno[]? turnos = null,
        Funcao[]? funcoes = null) =>
        new(empresa ?? Comercio, inicio ?? D(1), fim ?? D(31), funcionarios, alocacoes,
            feriados ?? [], ocorrencias ?? [], demandas ?? [], turnos ?? [], funcoes ?? []);

    /// Seis dias de trabalho iguais seguidos, a partir de `inicio`.
    public static Alocacao[] Semana(Funcionario f, DateOnly inicio, string entrada, string saida, int intervaloMin = 60) =>
        Enumerable.Range(0, 6).Select(i => Trab(f, inicio.AddDays(i), entrada, saida, intervaloMin)).ToArray();
}
