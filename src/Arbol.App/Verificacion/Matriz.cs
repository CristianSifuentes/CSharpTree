namespace Arbol.App.Verificacion;

/// <summary>Lo que el PDF dice de cada celda de la matriz "¿Qué hoja crece en qué brote?".</summary>
public enum Celda
{
    /// <summary>— no</summary>
    No,

    /// <summary>● sí</summary>
    Si,

    /// <summary>○ solo en su forma static</summary>
    SoloStatic,
}

public sealed record FilaMatriz(string Miembro, string Instancia, string? Static, Celda[] EsperadoPdf);

public sealed record ResultadoCelda(string Miembro, string Contenedor, Celda Esperado, Celda Observado, string Detalle)
{
    public bool Cumple => Esperado == Observado;
}

/// <summary>
/// La matriz miembro × tipo, verificada a fuerza de compilar: cada celda mete el miembro en el tipo
/// (primero en su forma natural, luego en su forma static) y mira si compila.
/// </summary>
public static class Matriz
{
    public static IReadOnlyList<string> Contenedores { get; } = ["class", "struct", "interface", "enum", "static class"];

    private static readonly Celda S = Celda.Si;
    private static readonly Celda N = Celda.No;
    private static readonly Celda C = Celda.SoloStatic;

    //                                                                                         class struct interface enum static class
    public static IReadOnlyList<FilaMatriz> Filas { get; } =
    [
        new("campo de instancia", "int x;", null, [S, S, N, N, N]),
        new("campo static", "static int x;", null, [S, S, S, N, S]),
        new("constante", "const int X = 1;", null, [S, S, S, S, S]),
        new("propiedad", "int P { get; set; }", "static int P { get; set; }", [S, S, S, N, C]),
        new("indexador", "int this[int i] { get { return 0; } }", "static int this[int i] { get { return 0; } }", [S, S, S, N, N]),
        new("método", "void M() { }", "static void M() { }", [S, S, S, N, C]),
        new("constructor", "public T() { }", "static T() { }", [S, S, C, N, C]),
        new("finalizador", "~T() { }", null, [S, N, N, N, N]),
        new("evento", "event System.Action E;", "static event System.Action E;", [S, S, S, N, C]),
        new("operador", "public static T operator +(T a, T b) => a;", null, [S, S, S, N, N]),
        new("tipo anidado", "class Anidado { }", null, [S, S, S, N, S]),
        new("bloque extension", "extension(int x) { public bool Par => x % 2 == 0; }", null, [N, N, N, N, S]),
    ];

    public static IEnumerable<ResultadoCelda> Verificar()
    {
        foreach (var fila in Filas)
        {
            for (var c = 0; c < Contenedores.Count; c++)
            {
                var contenedor = Contenedores[c];
                var natural = Compilar(contenedor, fila.Miembro == "constante" && contenedor == "enum" ? "X = 1" : fila.Instancia);
                Celda observado;
                string detalle;
                if (natural.Compila)
                {
                    observado = Celda.Si;
                    detalle = "compila";
                }
                else if (fila.Static is not null && Compilar(contenedor, fila.Static).Compila)
                {
                    observado = Celda.SoloStatic;
                    detalle = $"natural: {natural.Codigos} · static: compila";
                }
                else
                {
                    observado = Celda.No;
                    detalle = natural.Codigos;
                }

                yield return new ResultadoCelda(fila.Miembro, contenedor, fila.EsperadoPdf[c], observado, detalle);
            }
        }
    }

    private static Compilador.Resultado Compilar(string contenedor, string miembro)
    {
        // El tipo se llama T para que constructores y operadores puedan nombrarlo.
        var codigo = $"{contenedor} T {{ {miembro} }}";
        return Compilador.Compilar([codigo]);
    }

    public static string Simbolo(this Celda celda) => celda switch
    {
        Celda.Si => "●",
        Celda.SoloStatic => "○",
        _ => "—",
    };
}
