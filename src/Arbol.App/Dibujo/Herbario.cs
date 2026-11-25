using System.Globalization;
using System.Net;
using System.Text;
using Arbol.App.Raices;

namespace Arbol.App.Dibujo;

/// <summary>
/// Convierte un árbol real en una lámina de herbario (SVG): crema, tinta y rojo.
/// Crece de izquierda a derecha: cada columna es un nivel; cada curva, "contiene a".
/// </summary>
public static class Herbario
{
    private const string Fondo = "#ECE6DA";
    private const string Tinta = "#151515";
    private const string Rojo = "#E0312B";
    private const string Gris = "#6E685E";
    private const string Puntos = "#D3CBBB";

    private const double Margen = 90;
    private const double Columna = 245;
    private const double Fila = 24;
    private const double Cabecera = 300;

    public static string Dibujar(Nodo raiz, Nivel hasta, string titulo)
    {
        var podado = raiz.Podar(hasta);
        var niveles = Nivel.Todos.Where(n => n >= podado.Nivel && n <= hasta).ToList();
        var columnaDe = niveles.Select((n, i) => (n, i)).ToDictionary(p => p.n, p => p.i);

        // 1. Posiciones: las hojas se apilan; cada padre queda a la altura media de sus hijos.
        var posiciones = new Dictionary<Nodo, (double X, double Y)>(ReferenceEqualityComparer.Instance);
        var columnas = new Dictionary<Nodo, int>(ReferenceEqualityComparer.Instance);
        var siguienteFila = 0;
        Colocar(podado, -1);
        var columnasUsadas = columnas.Values.Max() + 1;

        var hojas = siguienteFila;
        var ancho = Margen + (Columna * (columnasUsadas - 1)) + 420;
        var alto = Cabecera + (hojas * Fila) + 220;

        var svg = new StringBuilder();
        svg.AppendLine(CultureInfo.InvariantCulture, $"""<svg xmlns="http://www.w3.org/2000/svg" width="{F(ancho)}" height="{F(alto)}" viewBox="0 0 {F(ancho)} {F(alto)}">""");
        svg.AppendLine($$"""
            <defs>
              <style>
                @import url('https://fonts.googleapis.com/css2?family=Instrument+Serif:ital@0;1&amp;family=Noto+Sans+JP:wght@900&amp;family=Geist+Mono:wght@400;500&amp;display=block');
                .k { font-family: 'Noto Sans JP', sans-serif; font-weight: 900; }
                .i { font-family: 'Instrument Serif', serif; font-style: italic; }
                .m { font-family: 'Geist Mono', 'JetBrains Mono', monospace; }
                .e { paint-order: stroke; stroke: {{Fondo}}; stroke-width: 5px; stroke-linejoin: round; }
              </style>
              <pattern id="puntos" width="36" height="36" patternUnits="userSpaceOnUse">
                <circle cx="18" cy="18" r="1.3" fill="{{Puntos}}"/>
              </pattern>
            </defs>
            <rect width="100%" height="100%" fill="{{Fondo}}"/>
            <rect width="100%" height="100%" fill="url(#puntos)"/>
            """);

        // 2. Cabecera: kanji, título y una columna por nivel.
        svg.AppendLine(CultureInfo.InvariantCulture, $"""<text x="{F(Margen)}" y="70" class="m" font-size="20" fill="{Gris}" letter-spacing="1.5">cristian / herbario</text>""");
        svg.AppendLine(CultureInfo.InvariantCulture, $"""<text x="{F(ancho - Margen)}" y="70" class="m" font-size="20" fill="{Gris}" text-anchor="end">[ c# 15 ]</text>""");
        svg.AppendLine(CultureInfo.InvariantCulture, $"""<text x="{F(Margen)}" y="170" class="k" font-size="92" fill="{Tinta}">樹<tspan fill="{Rojo}">形</tspan></text>""");
        svg.AppendLine(CultureInfo.InvariantCulture, $"""<text x="{F(Margen + 215)}" y="140" class="i" font-size="52" fill="{Tinta}">{X(titulo)}</text>""");
        svg.AppendLine(CultureInfo.InvariantCulture, $"""<text x="{F(Margen + 217)}" y="172" class="m" font-size="16" fill="{Gris}">leído con Roslyn · {hojas} hojas · {podado.Descendientes().Count() + 1} nodos · lo general a la izquierda</text>""");
        svg.AppendLine(CultureInfo.InvariantCulture, $"""<line x1="{F(Margen)}" y1="200" x2="{F(ancho - Margen)}" y2="200" stroke="{Tinta}" stroke-width="2"/><circle cx="{F(ancho - Margen)}" cy="200" r="6" fill="{Rojo}"/>""");

        foreach (var nivel in niveles)
        {
            var x = Margen + (columnaDe[nivel] * Columna);
            svg.AppendLine(CultureInfo.InvariantCulture, $"""<text x="{F(x - 12)}" y="258" class="k" font-size="30" fill="{(nivel.EsExtremo ? Rojo : Tinta)}">{nivel.Kanji}</text>""");
            svg.AppendLine(CultureInfo.InvariantCulture, $"""<text x="{F(x + 26)}" y="256" class="i" font-size="24" fill="{Gris}">{nivel.Nombre}</text>""");
            svg.AppendLine(CultureInfo.InvariantCulture, $"""<line x1="{F(x)}" y1="272" x2="{F(x)}" y2="{F(alto - 190)}" stroke="{Puntos}" stroke-width="1" stroke-dasharray="1 6"/>""");
        }

        // 3. Ramas (primero, para que los nodos queden encima).
        foreach (var nodo in podado.Descendientes())
        {
            var (x, y) = posiciones[nodo];
            var (px, py) = posiciones[nodo.Padre!];
            var medio = (px + x) / 2;
            var grosor = Math.Max(0.9, 3.0 - (columnas[nodo] * 0.4));
            svg.AppendLine(CultureInfo.InvariantCulture, $"""<path d="M{F(px)},{F(py)} C{F(medio)},{F(py)} {F(medio)},{F(y)} {F(x)},{F(y)}" fill="none" stroke="{Tinta}" stroke-width="{F(grosor)}"/>""");
        }

        // 4. Nodos y etiquetas.
        foreach (var nodo in podado.Descendientes().Prepend(podado))
        {
            var (x, y) = posiciones[nodo];
            var esHoja = nodo.Hijos.Count == 0;
            var rojo = nodo.Nivel.EsExtremo || nodo.Novedad == "C# 15";
            var radio = esHoja ? 3.6 : Math.Max(4.5, 9 - columnas[nodo]);
            svg.AppendLine(CultureInfo.InvariantCulture, $"""<circle cx="{F(x)}" cy="{F(y)}" r="{F(radio)}" fill="{(rojo ? Rojo : Tinta)}"/>""");

            // Las hojas tienen espacio a la derecha; los nodos internos deben caber en su columna.
            var limite = esHoja ? 40 : 27;
            var etiqueta = X(nodo.Etiqueta.Length > limite ? nodo.Etiqueta[..(limite - 1)] + "…" : nodo.Etiqueta);
            var color = rojo ? Rojo : Tinta;
            if (esHoja)
            {
                var nota = nodo.Novedad is { } n ? $"""<tspan fill="{(n == "C# 15" ? Rojo : Gris)}" font-size="11">  {n}</tspan>""" : "";
                svg.AppendLine(CultureInfo.InvariantCulture, $"""<text x="{F(x + 10)}" y="{F(y + 4.5)}" class="m e" font-size="13.5" fill="{color}">{etiqueta}{nota}</text>""");
            }
            else
            {
                svg.AppendLine(CultureInfo.InvariantCulture, $"""<text x="{F(x + 9)}" y="{F(y - 9)}" class="m e" font-size="13.5" font-weight="500" fill="{color}">{etiqueta}</text>""");
            }
        }

        // 5. Pie: leyenda y firma.
        var pie = alto - 120;
        svg.AppendLine(CultureInfo.InvariantCulture, $"""<line x1="{F(Margen)}" y1="{F(pie - 40)}" x2="{F(ancho - Margen)}" y2="{F(pie - 40)}" stroke="{Puntos}" stroke-width="1.4" stroke-dasharray="2 8"/>""");
        svg.AppendLine(CultureInfo.InvariantCulture, $"""<circle cx="{F(Margen + 6)}" cy="{F(pie)}" r="6" fill="{Tinta}"/><text x="{F(Margen + 20)}" y="{F(pie + 5)}" class="m" font-size="15" fill="{Tinta}">contiene a → (a la derecha)</text>""");
        svg.AppendLine(CultureInfo.InvariantCulture, $"""<circle cx="{F(Margen + 296)}" cy="{F(pie)}" r="6" fill="{Rojo}"/><text x="{F(Margen + 310)}" y="{F(pie + 5)}" class="m" font-size="15" fill="{Tinta}">brote de C# 15</text>""");
        svg.AppendLine(CultureInfo.InvariantCulture, $"""<text x="{F(Margen + 480)}" y="{F(pie + 5)}" class="m" font-size="15" fill="{Gris}">misma columna = mismo nivel</text>""");
        svg.AppendLine(CultureInfo.InvariantCulture, $"""<text x="{F(Margen)}" y="{F(pie + 62)}" class="m" font-size="24" font-weight="500" fill="{Tinta}">cristian<tspan fill="#1F9D55">_</tspan></text>""");
        svg.AppendLine(CultureInfo.InvariantCulture, $"""<text x="{F(Margen)}" y="{F(pie + 86)}" class="m" font-size="13" fill="{Gris}" letter-spacing="4">ARTE · CÓDIGO · IA</text>""");
        svg.AppendLine(CultureInfo.InvariantCulture, $"""<text x="{F(ancho - Margen)}" y="{F(pie + 70)}" class="m" font-size="18" fill="{Tinta}" text-anchor="end"><tspan fill="{Rojo}">›</tspan> while (vivo) crecer();</text>""");
        svg.AppendLine("</svg>");
        return svg.ToString();

        double Colocar(Nodo nodo, int columnaPadre)
        {
            var columna = Math.Max(columnaDe[nodo.Nivel], columnaPadre + 1);
            columnas[nodo] = columna;
            var x = Margen + (columna * Columna);
            double y;
            if (nodo.Hijos.Count == 0)
            {
                y = Cabecera + (siguienteFila++ * Fila);
            }
            else
            {
                var ys = nodo.Hijos.Select(h => Colocar(h, columna)).ToList();
                y = (ys.Min() + ys.Max()) / 2;
            }

            posiciones[nodo] = (x, y);
            return y;
        }
    }

    private static string F(double valor) => valor.ToString("0.#", CultureInfo.InvariantCulture);

    private static string X(string texto) => WebUtility.HtmlEncode(texto);
}
