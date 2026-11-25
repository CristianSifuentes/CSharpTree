using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Arbol.App.Verificacion;

/// <summary>
/// Una afirmación del PDF convertida en experimento: un fragmento de código y lo que el PDF dice que pasará.
/// <para><see cref="Diagnostico"/> = el código de error prometido; <c>null</c> + <see cref="DebeCompilar"/> = false
/// significa "no compila", sin prometer un código concreto.</para>
/// </summary>
public sealed record Afirmacion(
    string Id,
    string Kanji,
    string Texto,
    string[] Fuentes,
    bool DebeCompilar,
    string? Diagnostico = null,
    OutputKind Salida = OutputKind.DynamicallyLinkedLibrary,
    LanguageVersion? Desde = null)
{
    public Afirmacion(string id, string kanji, string texto, string codigo, bool debeCompilar, string? diagnostico = null)
        : this(id, kanji, texto, [codigo], debeCompilar, diagnostico)
    {
    }

    public Veredicto Comprobar()
    {
        var resultado = Compilador.Compilar(Fuentes, Salida);
        var cumple = DebeCompilar
            ? resultado.Compila
            : !resultado.Compila && (Diagnostico is null || resultado.Tiene(Diagnostico));

        // Para las novedades: además debe FALLAR con la versión anterior del lenguaje.
        string? antes = null;
        if (Desde is { } version && cumple)
        {
            var anterior = Compilador.Compilar(Fuentes, Salida, (LanguageVersion)((int)version - 100));
            antes = anterior.Compila ? "también compila antes" : $"antes: {anterior.Codigos}";
            cumple = !anterior.Compila;
        }

        var observado = resultado.Codigos + (antes is null ? "" : $" · {antes}");
        return new Veredicto(this, cumple, observado, resultado.PrimerMensaje);
    }

    public override string ToString() => $"{Id} {Texto}";
}

public sealed record Veredicto(Afirmacion Afirmacion, bool Cumple, string Observado, string Mensaje);

/// <summary>Todas las afirmaciones del PDF "El árbol de C#", agrupadas por página.</summary>
public static class Afirmaciones
{
    public static IReadOnlyList<Afirmacion> Ubicacion { get; } =
    [
        new("U1", "芽", "Un tipo nunca va dentro de un método", "class C { void M() { class X { } } }", false),
        new("U1b", "芽", "Un tipo sí puede ir dentro de otro tipo (anidado)", "namespace App { class C { enum Fase { A } } }", true),
        new("U2", "葉", "Un miembro suelto en un namespace es CS0116", "namespace App; int contador;", false, "CS0116"),
        new("U2b", "葉", "Lo más parecido a un global: miembro static de clase static", "namespace App; static class Config { public static int Max = 3; }", true),
        new("U3", "枝", "Top-level en dos archivos es CS8802",
            ["System.Console.WriteLine(1);", "System.Console.WriteLine(2);"], false, "CS8802", OutputKind.ConsoleApplication),
        new("U3b", "枝", "Top-level en un solo archivo compila",
            ["System.Console.WriteLine(1);", "class Otro { }"], true, null, OutputKind.ConsoleApplication),
        new("U3c", "枝", "Un using después de código es CS1529",
            ["System.Console.WriteLine(1);\nusing System.Linq;"], false, "CS1529", OutputKind.ConsoleApplication),
        new("U3d", "枝", "Top-level después de un namespace es CS8803",
            ["namespace A { }\nSystem.Console.WriteLine(1);"], false, "CS8803", OutputKind.ConsoleApplication),
        new("U4", "芽", "Una interfaz no tiene campos de instancia: CS0525", "interface I { int x; }", false, "CS0525"),
        new("U4b", "芽", "…pero sí propiedades", "interface I { int X { get; } }", true),
        new("U5", "葉", "Extensión en clase genérica: CS1106", "static class E<T> { static void M(this int x) { } }", false, "CS1106"),
        new("U5b", "葉", "Extensión en clase no static: CS1106", "class E { static void M(this int x) { } }", false, "CS1106"),
        new("U5c", "葉", "Extensión en clase anidada: CS1109", "static class A { static class E { static void M(this int x) { } } }", false, "CS1109"),
        new("U5d", "葉", "Bloque extension en clase static de primer nivel compila",
            "static class E { extension(int x) { public bool Par => x % 2 == 0; } }", true),
        new("U5e", "葉", "Bloque extension en clase genérica no compila",
            "static class E<T> { extension(int x) { public bool Par => x % 2 == 0; } }", false),
    ];

    public static IReadOnlyList<Afirmacion> Archivo { get; } =
    [
        new("A1", "茎", "file-scoped y namespace de bloque no se mezclan", "namespace A;\nnamespace B { }", false),
        new("A2", "茎", "Varios namespaces de bloque sí conviven", "namespace A { }\nnamespace B { namespace C { } }", true),
        new("A3", "茎", "Un namespace no es una carpeta: se reparte en archivos",
            ["namespace Tienda { class A { } }", "namespace Tienda { class B { A a = new(); } }"], true),
        new("A4", "芽", "partial reparte un tipo entre archivos",
            ["partial class P { int x; }", "partial class P { int Y => x; }"], true),
    ];

    public static IReadOnlyList<Afirmacion> Herencia { get; } =
    [
        new("H1", "系", "Una sola clase base: CS1721", "class A { } class B { } class C : A, B { }", false, "CS1721"),
        new("H1b", "系", "Una base y muchas interfaces sí", "interface I { } interface J { } class A { } class C : A, I, J { }", true),
        new("H2", "系", "sealed no tiene herederos: CS0509", "sealed class A { } class B : A { }", false, "CS0509"),
        new("H3", "系", "struct solo implementa interfaces: CS0527", "class Base { } struct S : Base { }", false, "CS0527"),
        new("H3b", "系", "struct con interfaces sí", "struct S : System.IComparable<S> { public int CompareTo(S o) => 0; }", true),
        new("H4", "系", "Sin ciclos: CS0146", "class A : B { } class B : A { }", false, "CS0146"),
        new("H5", "系", "abstract no se instancia: CS0144", "abstract class F { } class U { object M() => new F(); }", false, "CS0144"),
        new("H6", "系", "override solo sobre virtual/abstract/override: CS0506",
            "class A { public void D() { } } class B : A { public override void D() { } }", false, "CS0506"),
        new("H7", "系", "override con otra visibilidad: CS0507",
            "class A { public virtual void D() { } } class B : A { protected override void D() { } }", false, "CS0507"),
    ];

    public static IReadOnlyList<Afirmacion> Miembros { get; } =
    [
        new("M1", "葉", "static class: miembro de instancia es CS0708", "static class S { public int X; }", false, "CS0708"),
        new("M2", "葉", "static class: operador es CS0715", "static class S { public static int operator +(S a, S b) => 0; }", false, "CS0715"),
        new("M3", "葉", "struct no admite finalizador", "struct S { ~S() { } }", false),
        new("M4", "葉", "interface no admite constructor de instancia", "interface I { I() { } }", false),
        new("M5", "葉", "interface sí admite constructor static", "interface I { static I() { } }", true),
        new("M6", "葉", "Los miembros son hermanos: el orden no importa", "class C { int Y => X; int X = 1; }", true),
        new("S1", "花", "Un local con private no compila (es error de sintaxis)", "class C { void M() { private int x = 0; } }", false),
        new("S1b", "花", "Una función local con public no compila", "class C { void M() { public void F() { } } }", false),
        new("S1c", "花", "Un local con static tampoco", "class C { void M() { static int x = 0; } }", false),
        new("S2", "花", "Función local static no captura: CS8421",
            "class C { int M() { int a = 1; return F(); static int F() => a; } }", false, "CS8421"),
        new("E1", "種", "Una sentencia suelta como expresión no vale: CS0201", "class C { void M() { 1 + 1; } }", false, "CS0201"),
    ];

    /// <summary>Página "Brotes nuevos": cada novedad compila con su versión y FALLA con la anterior.</summary>
    public static IReadOnlyList<Afirmacion> Novedades { get; } =
    [
        Nueva("N9a", "枝", "C# 9 · top-level statements", LanguageVersion.CSharp9, "System.Console.WriteLine(1);", OutputKind.ConsoleApplication),
        Nueva("N9b", "芽", "C# 9 · record", LanguageVersion.CSharp9, "record R(int X);"),
        Nueva("N9c", "葉", "C# 9 · init", LanguageVersion.CSharp9, "class C { public int X { get; init; } }"),
        Nueva("N10a", "茎", "C# 10 · file-scoped namespace", LanguageVersion.CSharp10, "namespace A;\nclass C { }"),
        Nueva("N10b", "茎", "C# 10 · global using", LanguageVersion.CSharp10, "global using System;\nclass C { }"),
        Nueva("N10c", "芽", "C# 10 · record struct", LanguageVersion.CSharp10, "record struct R(int X);"),
        Nueva("N11a", "芽", "C# 11 · file class", LanguageVersion.CSharp11, "file class C { }"),
        Nueva("N11b", "葉", "C# 11 · required", LanguageVersion.CSharp11, "class C { public required int X { get; init; } }"),
        Nueva("N11c", "葉", "C# 11 · static abstract en interfaces", LanguageVersion.CSharp11, "interface I<T> where T : I<T> { static abstract T Crear(); }"),
        Nueva("N11d", "種", "C# 11 · patrones de lista", LanguageVersion.CSharp11, "class C { bool M(int[] a) => a is [1, ..]; }"),
        Nueva("N11e", "種", "C# 11 · raw strings", LanguageVersion.CSharp11, "class C { string S = \"\"\"\n  hola\n  \"\"\"; }"),
        Nueva("N12a", "芽", "C# 12 · constructor primario en class", LanguageVersion.CSharp12, "class C(int x) { int X => x; }"),
        Nueva("N12b", "種", "C# 12 · expresiones de colección", LanguageVersion.CSharp12, "class C { int[] A = [1, 2, 3]; }"),
        Nueva("N12c", "枝", "C# 12 · using alias de cualquier tipo", LanguageVersion.CSharp12, "using Par = (int, int);\nclass C { Par P; }"),
        Nueva("N13a", "花", "C# 13 · params con colecciones", LanguageVersion.CSharp13, "class C { void M(params System.ReadOnlySpan<int> xs) { } }"),
        Nueva("N13b", "葉", "C# 13 · propiedades partial", LanguageVersion.CSharp13,
            "partial class C { public partial int X { get; } }\npartial class C { public partial int X => 1; }"),
        Nueva("N13c", "芽", "C# 13 · ref struct que implementa interfaces", LanguageVersion.CSharp13,
            "ref struct R : System.IDisposable { public void Dispose() { } }"),
        Nueva("N14a", "葉", "C# 14 · field en propiedades", LanguageVersion.CSharp14,
            "class C { public string N { get; set => field = value.Trim(); } = \"\"; }"),
        Nueva("N14b", "葉", "C# 14 · bloques extension", LanguageVersion.CSharp14,
            "static class E { extension(int x) { public bool Par => x % 2 == 0; } }"),
        Nueva("N14c", "葉", "C# 14 · constructores partial", LanguageVersion.CSharp14,
            "partial class C { public partial C(int x); }\npartial class C { public partial C(int x) { } }"),
        Nueva("N14d", "葉", "C# 14 · eventos partial", LanguageVersion.CSharp14,
            "partial class C { public partial event System.Action E; }\npartial class C { public partial event System.Action E { add { } remove { } } }"),
        Nueva("N14e", "葉", "C# 14 · operadores += propios", LanguageVersion.CSharp14,
            "class C { int n; public void operator +=(int x) => n += x; }"),
        Nueva("N14f", "花", "C# 14 · lambdas con ref/out sin tipos", LanguageVersion.CSharp14,
            "delegate bool P(string s, out int v);\nclass C { P p = (s, out v) => int.TryParse(s, out v); }"),
        Nueva("N14g", "種", "C# 14 · asignación con ?.", LanguageVersion.CSharp14,
            "class C { public int X; static void M(C? c) => c?.X = 1; }"),
        Nueva("N14h", "種", "C# 14 · nameof(List<>)", LanguageVersion.CSharp14,
            "class C { string N = nameof(System.Collections.Generic.List<>); }"),
    ];

    public static IEnumerable<(string Titulo, IReadOnlyList<Afirmacion> Lista)> Grupos =>
    [
        ("Cada cosa en su rama · reglas de ubicación", Ubicacion),
        ("Dos árboles que se cruzan · archivo y namespace", Archivo),
        ("Una sola savia · herencia", Herencia),
        ("Miembros, sentencias y expresiones", Miembros),
        ("Brotes nuevos · compila con su versión y falla con la anterior", Novedades),
    ];

    public static IEnumerable<Afirmacion> Todas => Grupos.SelectMany(g => g.Lista);

    private static Afirmacion Nueva(string id, string kanji, string texto, LanguageVersion version, string codigo, OutputKind salida = OutputKind.DynamicallyLinkedLibrary) =>
        new(id, kanji, texto, codigo.Split("\n---\n"), true, null, salida, version);
}
