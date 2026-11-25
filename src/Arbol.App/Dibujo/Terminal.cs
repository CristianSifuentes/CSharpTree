using System.Text;
using Arbol.App.Raices;

namespace Arbol.App.Dibujo;

/// <summary>Dibuja el árbol en la terminal con la paleta del herbario: tinta, rojo y gris.</summary>
public sealed class Terminal(TextWriter salida, bool color)
{
    private const string Reset = "\e[0m";
    private const string Rojo = "\e[38;2;224;49;43m";
    private const string Gris = "\e[38;2;142;134;120m";
    private const string Verde = "\e[38;2;31;157;85m";
    private const string Negrita = "\e[1m";

    public static Terminal Consola() => new(
        Console.Out,
        Environment.GetEnvironmentVariable("FORCE_COLOR") is not null
        || (!Console.IsOutputRedirected && Environment.GetEnvironmentVariable("NO_COLOR") is null));

    private string C(string codigo, string texto) => color ? $"{codigo}{texto}{Reset}" : texto;

    public void Portada(string subtitulo)
    {
        salida.WriteLine();
        salida.WriteLine($"   {C(Negrita, "樹")}{C(Rojo + Negrita, "形")}   {C(Negrita, "el árbol de C#")}");
        salida.WriteLine($"   {C(Gris, subtitulo)}");
        salida.WriteLine($"   {C(Gris, new string('─', 46))}{C(Rojo, "●")}");
        salida.WriteLine();
    }

    public void Titulo(string kanji, string texto)
    {
        salida.WriteLine();
        salida.WriteLine($"  {C(Rojo, kanji)}  {C(Negrita, texto)}");
        salida.WriteLine();
    }

    /// <summary>El árbol con guías: cada sangría es "estar dentro de"; las líneas a la misma sangría son hermanas.</summary>
    public void Arbol(Nodo raiz, Nivel hasta, int maxHijos = int.MaxValue)
    {
        salida.WriteLine($"  {Marca(raiz)} {Etiqueta(raiz)}");
        Ramas(raiz, "  ", hasta, maxHijos);
    }

    private void Ramas(Nodo nodo, string prefijo, Nivel hasta, int maxHijos)
    {
        var visibles = nodo.Hijos.Where(h => h.Nivel <= hasta).ToList();
        var ocultos = Math.Max(0, visibles.Count - maxHijos);
        if (ocultos > 0)
        {
            visibles = visibles.Take(maxHijos).ToList();
        }

        for (var i = 0; i < visibles.Count; i++)
        {
            var hijo = visibles[i];
            var ultimo = i == visibles.Count - 1 && ocultos == 0;
            salida.WriteLine($"{C(Gris, prefijo + (ultimo ? "└─ " : "├─ "))}{Marca(hijo)} {Etiqueta(hijo)}");
            Ramas(hijo, prefijo + (ultimo ? "   " : "│  "), hasta, maxHijos);
        }

        if (ocultos > 0)
        {
            salida.WriteLine(C(Gris, $"{prefijo}└─ … {ocultos} hermano(s) más"));
        }
    }

    private string Marca(Nodo nodo) => C(nodo.Nivel.EsExtremo || nodo.Novedad == "C# 15" ? Rojo : Negrita, nodo.Nivel.Kanji);

    private string Etiqueta(Nodo nodo)
    {
        var texto = new StringBuilder(nodo.Etiqueta);
        if (nodo.Detalle is { Length: > 0 } detalle)
        {
            texto.Append(C(Gris, $"  {detalle}"));
        }

        if (nodo.Novedad is { } novedad)
        {
            texto.Append(' ').Append(C(novedad == "C# 15" ? Rojo : Gris, $"[{novedad}]"));
        }

        return texto.ToString();
    }

    /// <summary>Cuántos nodos hay en cada nivel: la forma de la planta en una columna.</summary>
    public void Censo(Nodo raiz)
    {
        var todos = raiz.Descendientes().Prepend(raiz).ToList();
        var maximo = Nivel.Todos.Max(n => todos.Count(x => x.Nivel == n));
        foreach (var nivel in Nivel.Todos)
        {
            var cuantos = todos.Count(x => x.Nivel == nivel);
            var puntos = (int)Math.Ceiling(cuantos * 40.0 / Math.Max(1, maximo));
            salida.WriteLine(
                $"  {C(nivel.EsExtremo ? Rojo : Negrita, nivel.Kanji)}  {nivel.Nombre,-10} {C(Gris, nivel.Planta.PadRight(8))} {cuantos,5}  {C(nivel.EsExtremo ? Rojo : Gris, new string('•', puntos))}");
        }
    }

    /// <summary>Dónde vive un nodo (su ruta desde la raíz) y quiénes están a su mismo nivel.</summary>
    public void Ruta(Nodo nodo)
    {
        var pasos = nodo.Ruta().ToList();
        for (var i = 0; i < pasos.Count; i++)
        {
            var paso = pasos[i];
            var actual = i == pasos.Count - 1;
            salida.WriteLine($"  {new string(' ', i * 2)}{C(actual ? Rojo : Negrita, paso.Nivel.Kanji)} {(actual ? C(Rojo, paso.Etiqueta) : paso.Etiqueta)}");
        }

        var hermanos = nodo.Hermanos.ToList();
        salida.WriteLine();
        salida.WriteLine($"  {C(Gris, $"mismo nivel ({hermanos.Count} hermanos dentro de «{nodo.Padre?.Etiqueta}»):")}");
        foreach (var grupo in hermanos.GroupBy(h => h.Nivel))
        {
            var filas = grupo.Select(h => h.Etiqueta).Chunk(4).ToList();
            for (var f = 0; f < filas.Count; f++)
            {
                var kanji = f == 0 ? C(Negrita, grupo.Key.Kanji) : " ";
                salida.WriteLine($"    {kanji} {string.Join(C(Gris, " · "), filas[f])}");
            }
        }
    }

    public void Linea(string texto = "") => salida.WriteLine(texto);

    public void Veredicto(bool cumple, string id, string texto, string observado)
    {
        var marca = cumple ? C(Verde, "✓") : C(Rojo, "✗");
        var corto = observado.Length > 44 ? string.Concat(observado.AsSpan(0, 43), "…") : observado;
        salida.WriteLine($"  {marca} {C(Gris, id.PadRight(5))} {texto.PadRight(58)} {C(cumple ? Gris : Rojo, corto)}");
    }

    public string Pinta(string texto, bool rojo) => C(rojo ? Rojo : Gris, texto);

    public string Tinta(string texto) => C(Negrita, texto);
}
