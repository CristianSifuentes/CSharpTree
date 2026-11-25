using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Arbol.App.Verificacion;

/// <summary>El juez: compila fragmentos en memoria con el mismo compilador de C# 15 y devuelve sus errores.</summary>
public static class Compilador
{
    private static readonly Lazy<ImmutableArray<MetadataReference>> s_referencias = new(() =>
        [
            .. ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
                .Split(Path.PathSeparator)
                .Where(ruta => ruta.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                .Select(ruta => (MetadataReference)MetadataReference.CreateFromFile(ruta)),
        ]);

    public static Resultado Compilar(
        IReadOnlyList<string> fuentes,
        OutputKind salida = OutputKind.DynamicallyLinkedLibrary,
        LanguageVersion version = LanguageVersion.CSharp15,
        IEnumerable<MetadataReference>? extra = null)
    {
        var opciones = CSharpParseOptions.Default.WithLanguageVersion(version);
        var arboles = fuentes.Select((codigo, i) => CSharpSyntaxTree.ParseText(codigo, opciones, path: $"Archivo{i + 1}.cs"));

        var compilacion = CSharpCompilation.Create(
            "Prueba",
            arboles,
            s_referencias.Value.AddRange(extra ?? []),
            new CSharpCompilationOptions(salida, nullableContextOptions: NullableContextOptions.Enable));

        var errores = compilacion
            .GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToImmutableArray();

        return new Resultado(errores);
    }

    /// <summary>Compila una biblioteca aparte y la devuelve como referencia: sirve para probar reglas entre ensamblados.</summary>
    public static MetadataReference Biblioteca(string codigo, LanguageVersion version = LanguageVersion.CSharp15)
    {
        var arbol = CSharpSyntaxTree.ParseText(codigo, CSharpParseOptions.Default.WithLanguageVersion(version));
        var compilacion = CSharpCompilation.Create(
            "Biblioteca",
            [arbol],
            s_referencias.Value,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
        using var flujo = new MemoryStream();
        var emitido = compilacion.Emit(flujo);
        if (!emitido.Success)
        {
            throw new InvalidOperationException($"La biblioteca no compiló: {string.Join(' ', emitido.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).Select(d => d.Id))}");
        }

        return MetadataReference.CreateFromImage(flujo.ToArray());
    }

    public sealed record Resultado(ImmutableArray<Diagnostic> Errores)
    {
        public bool Compila => Errores.IsEmpty;

        public bool Tiene(string id) => Errores.Any(e => e.Id == id);

        public string Codigos => Compila ? "compila" : string.Join(' ', Errores.Select(e => e.Id).Distinct());

        public string PrimerMensaje => Errores.FirstOrDefault()?.GetMessage(System.Globalization.CultureInfo.InvariantCulture) ?? "";
    }
}
