using System.Globalization;

namespace Folguinha.Domain;

/// Formatação fixa em pt-BR, independente da cultura do aparelho.
public static class Texto
{
    static readonly string[] Dias = ["dom", "seg", "ter", "qua", "qui", "sex", "sáb"];

    public static string Dia(DateOnly d) => $"{Dias[(int)d.DayOfWeek]} {d.ToString("dd'/'MM", CultureInfo.InvariantCulture)}";

    public static string Hora(TimeOnly t) => t.ToString("HH':'mm", CultureInfo.InvariantCulture);

    public static string Horas(TimeSpan t) =>
        t.Minutes == 0 ? $"{(int)t.TotalHours}h" : $"{(int)t.TotalHours}h{t.Minutes:00}";
}
