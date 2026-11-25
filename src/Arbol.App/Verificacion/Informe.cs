using System.Globalization;
using System.Text;

namespace Arbol.App.Verificacion;

/// <summary>Escribe el resultado de la verificación como Markdown, para guardarlo junto al PDF.</summary>
public static class Informe
{
    public static string Markdown(IReadOnlyList<(string Grupo, Veredicto V)> veredictos, IReadOnlyList<ResultadoCelda> celdas)
    {
        var md = new StringBuilder();
        var ok = veredictos.Count(v => v.V.Cumple) + celdas.Count(c => c.Cumple);
        var total = veredictos.Count + celdas.Count;

        md.AppendLine("# 証 Verificación de «El árbol de C#»");
        md.AppendLine();
        md.AppendLine(CultureInfo.InvariantCulture, $"Compilador: Roslyn {typeof(Microsoft.CodeAnalysis.CSharp.CSharpCompilation).Assembly.GetName().Version} · C# 14 · .NET {Environment.Version}");
        md.AppendLine();
        md.AppendLine(CultureInfo.InvariantCulture, $"**{ok} de {total}** afirmaciones confirmadas compilando código real.");

        foreach (var grupo in veredictos.GroupBy(v => v.Grupo))
        {
            md.AppendLine();
            md.AppendLine(CultureInfo.InvariantCulture, $"## {grupo.Key}");
            md.AppendLine();
            md.AppendLine("| | id | afirmación | código | el compilador dice |");
            md.AppendLine("|---|---|---|---|---|");
            foreach (var (_, v) in grupo)
            {
                var codigo = string.Join(" ⏎ ", v.Afirmacion.Fuentes).Replace("\n", " ⏎ ", StringComparison.Ordinal).Replace("|", "\\|", StringComparison.Ordinal);
                md.AppendLine(CultureInfo.InvariantCulture, $"| {(v.Cumple ? "✓" : "✗")} | {v.Afirmacion.Id} | {v.Afirmacion.Kanji} {v.Afirmacion.Texto} | `{codigo}` | {v.Observado} |");
            }
        }

        md.AppendLine();
        md.AppendLine("## 表 Matriz miembro × tipo");
        md.AppendLine();
        md.AppendLine("● compila · ○ solo en su forma static · — no compila");
        md.AppendLine();
        md.AppendLine(CultureInfo.InvariantCulture, $"| miembro | {string.Join(" | ", Matriz.Contenedores)} |");
        md.AppendLine(CultureInfo.InvariantCulture, $"|---|{string.Concat(Matriz.Contenedores.Select(_ => "---|"))}");
        foreach (var fila in celdas.GroupBy(c => c.Miembro))
        {
            md.AppendLine(CultureInfo.InvariantCulture, $"| {fila.Key} | {string.Join(" | ", fila.Select(c => c.Cumple ? c.Observado.Simbolo() : $"**{c.Observado.Simbolo()}** (PDF: {c.Esperado.Simbolo()})"))} |");
        }

        md.AppendLine();
        md.AppendLine("<details><summary>Detalle de cada celda</summary>");
        md.AppendLine();
        foreach (var c in celdas)
        {
            md.AppendLine(CultureInfo.InvariantCulture, $"- {c.Miembro} en {c.Contenedor}: {c.Detalle}");
        }

        md.AppendLine();
        md.AppendLine("</details>");
        return md.ToString();
    }
}
