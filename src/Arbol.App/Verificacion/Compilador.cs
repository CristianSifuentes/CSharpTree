using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Arbol.App.Verificacion;

/// <summary>El juez: compila fragmentos en memoria con el mismo compilador de C# 14 y devuelve sus errores.</summary>
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
        LanguageVersion version = LanguageVersion.CSharp14)
    {
        var opciones = CSharpParseOptions.Default.WithLanguageVersion(version);
        var arboles = fuentes.Select((codigo, i) => CSharpSyntaxTree.ParseText(codigo, opciones, path: $"Archivo{i + 1}.cs"));

        var compilacion = CSharpCompilation.Create(
            "Prueba",
            arboles,
            s_referencias.Value,
            new CSharpCompilationOptions(salida, nullableContextOptions: NullableContextOptions.Enable));

        var errores = compilacion
            .GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToImmutableArray();

        return new Resultado(errores);
    }

    public sealed record Resultado(ImmutableArray<Diagnostic> Errores)
    {
        public bool Compila => Errores.IsEmpty;

        public bool Tiene(string id) => Errores.Any(e => e.Id == id);

        public string Codigos => Compila ? "compila" : string.Join(' ', Errores.Select(e => e.Id).Distinct());

        public string PrimerMensaje => Errores.FirstOrDefault()?.GetMessage(System.Globalization.CultureInfo.InvariantCulture) ?? "";
    }
}
