using System.Globalization;
using Folguinha.Application;
using Folguinha.Domain;
using Folguinha.Domain.Regras;

namespace Folguinha.Web.Servicos;

/// Textos e classes visuais usados pelas telas.
public static class Formatos
{
    public static string Data(DateOnly d) => d.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

    public static string DataHora(DateTimeOffset d) => d.ToLocalTime().ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);

    public static string Dia(DateOnly d) => Texto.Dia(d);

    public static string Hora(TimeOnly t) => Texto.Hora(t);

    public static string Horas(TimeSpan t) => Texto.Horas(t);

    static readonly string[] Meses = ["janeiro", "fevereiro", "março", "abril", "maio", "junho", "julho", "agosto", "setembro", "outubro", "novembro", "dezembro"];

    public static string Mes(DateOnly d) => $"{Meses[d.Month - 1]} de {d.Year}";

    public static string MesCurto(int mes) => Meses[mes - 1][..3].ToUpperInvariant();

    public static string DiaSemanaCurto(DayOfWeek d) => d switch
    {
        DayOfWeek.Monday => "Seg", DayOfWeek.Tuesday => "Ter", DayOfWeek.Wednesday => "Qua", DayOfWeek.Thursday => "Qui",
        DayOfWeek.Friday => "Sex", DayOfWeek.Saturday => "Sáb", _ => "Dom",
    };

    public static string DiaSemana(DayOfWeek d) => d switch
    {
        DayOfWeek.Monday => "Segunda", DayOfWeek.Tuesday => "Terça", DayOfWeek.Wednesday => "Quarta", DayOfWeek.Thursday => "Quinta",
        DayOfWeek.Friday => "Sexta", DayOfWeek.Saturday => "Sábado", _ => "Domingo",
    };

    /// Segunda a domingo.
    public static readonly DayOfWeek[] Semana =
        [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday];

    public static DateOnly Segunda(DateOnly d) => d.AddDays(-(((int)d.DayOfWeek + 6) % 7));

    public static string Periodo(Escala e) =>
        e.Inicio.Day == 1 && e.Fim == e.Inicio.AddMonths(1).AddDays(-1)
            ? char.ToUpperInvariant(Mes(e.Inicio)[0]) + Mes(e.Inicio)[1..]
            : $"{Dia(e.Inicio)} a {Dia(e.Fim)}";

    public static string Regime(Regime r) => r switch
    {
        Domain.Regime.SeisPorUm => "6x1",
        Domain.Regime.CincoPorDois => "5x2",
        Domain.Regime.DozePorTrintaESeis => "12x36",
        _ => "Personalizado",
    };

    public static string Contrato(TipoContrato c) => c switch
    {
        TipoContrato.Clt => "CLT (tempo integral)",
        TipoContrato.TempoParcial => "Tempo parcial",
        TipoContrato.Aprendiz => "Aprendiz",
        _ => "Intermitente",
    };

    public static string Restricao(TipoRestricao t) => t switch
    {
        TipoRestricao.Contratual => "Contratual",
        TipoRestricao.Permanente => "Restrição permanente",
        TipoRestricao.Temporaria => "Temporária aprovada",
        _ => "Preferência",
    };

    public static string Posicao(PosicaoFolgaExtra p) => p switch
    {
        PosicaoFolgaExtra.FimDeSemana => "Sábado e domingo",
        PosicaoFolgaExtra.JuntoDaFolgaNormal => "Colada na folga normal",
        _ => "Qualquer dia",
    };

    public static string Abrangencia(AbrangenciaFeriado a) => a switch
    {
        AbrangenciaFeriado.Nacional => "Nacional",
        AbrangenciaFeriado.Estadual => "Estadual",
        AbrangenciaFeriado.Municipal => "Municipal",
        _ => "Ponto facultativo",
    };

    public static string Nivel(NivelRegra n) => n switch
    {
        NivelRegra.Obrigatoria => "Obrigatória",
        NivelRegra.Alerta => "Alerta",
        _ => "Preferência",
    };

    public static string Severidade(Severidade s) => s switch
    {
        Domain.Regras.Severidade.Bloqueio => "Bloqueio",
        Domain.Regras.Severidade.Critico => "Crítico",
        Domain.Regras.Severidade.Atencao => "Atenção",
        _ => "Informativo",
    };

    public static string ClasseSeveridade(Severidade s) => s switch
    {
        Domain.Regras.Severidade.Bloqueio => "sev-bloqueio",
        Domain.Regras.Severidade.Critico => "sev-critico",
        Domain.Regras.Severidade.Atencao => "sev-atencao",
        _ => "sev-info",
    };

    public static string Estado(Escala e, bool semBloqueio) => e.Estado switch
    {
        EstadoEscala.Rascunho => semBloqueio ? "Rascunho validado" : "Rascunho",
        EstadoEscala.EmValidacao => "Alterações não publicadas",
        EstadoEscala.Publicada => $"Publicada · v{e.Versoes.Count}",
        EstadoEscala.Alterada => $"Publicada · v{e.Versoes.Count}",
        EstadoEscala.Encerrada => $"Encerrada · v{e.Versoes.Count}",
        _ => e.Estado.ToString(),
    };

    public static string ClasseEstado(Escala e) => e.Estado switch
    {
        EstadoEscala.Rascunho or EstadoEscala.EmValidacao => "chip-ambar",
        EstadoEscala.Encerrada => "chip-neutro",
        _ => "chip-verde",
    };

    /// Classe de cor do turno (paleta em app.css), pela posição do turno no cadastro.
    public static string ClasseTurno(DadosDaUnidade dados, Alocacao? a) => a switch
    {
        null => "cel-vazia",
        { Tipo: TipoAlocacao.Folga } => "cel-folga",
        { Tipo: TipoAlocacao.Ocorrencia } => "cel-ausencia",
        _ => $"cel-turno-{Math.Max(0, dados.Turnos.ToList().FindIndex(t => t.Id == a.TurnoId)) % 5}",
    };

    public static string ClasseTurno(DadosDaUnidade dados, Guid turnoId) =>
        $"cel-turno-{Math.Max(0, dados.Turnos.ToList().FindIndex(t => t.Id == turnoId)) % 5}";

    public static string Sigla(DadosDaUnidade dados, Alocacao? a) => a switch
    {
        null => "–",
        { Tipo: TipoAlocacao.Folga } => "F",
        { Tipo: TipoAlocacao.Ocorrencia } => "A",
        _ => Sigla(dados.Turnos.FirstOrDefault(t => t.Id == a.TurnoId)?.Nome ?? "T"),
    };

    public static string Sigla(string nome) => nome.Length <= 2 ? nome : nome[..1].ToUpperInvariant();

    public static string Iniciais(string nome)
    {
        var partes = nome.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return partes.Length switch
        {
            0 => "?",
            1 => partes[0][..Math.Min(2, partes[0].Length)].ToUpperInvariant(),
            _ => $"{char.ToUpperInvariant(partes[0][0])}{char.ToUpperInvariant(partes[^1][0])}",
        };
    }

    public static string ClasseAvatar(Guid id) => $"avatar-{Math.Abs(id.GetHashCode()) % 6}";
}
