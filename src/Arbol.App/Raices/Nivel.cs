namespace Arbol.App.Raices;

/// <summary>Los ocho niveles del árbol de C#, de lo más general a lo más particular.</summary>
public enum Nivel
{
    Solucion,
    Proyecto,
    Archivo,
    Namespace,
    Tipo,
    Miembro,
    Sentencia,
    Expresion,
}

/// <summary>
/// La app usa en sí misma lo que enseña: un bloque <c>extension</c> (C# 14) le presta
/// propiedades al enum <see cref="Nivel"/>, que por ser enum no puede tener miembros propios.
/// </summary>
public static class NivelExtensiones
{
    extension(Nivel nivel)
    {
        public string Kanji => nivel switch
        {
            Nivel.Solucion => "根",
            Nivel.Proyecto => "幹",
            Nivel.Archivo => "枝",
            Nivel.Namespace => "茎",
            Nivel.Tipo => "芽",
            Nivel.Miembro => "葉",
            Nivel.Sentencia => "花",
            Nivel.Expresion => "種",
            _ => "・",
        };

        public string Nombre => nivel switch
        {
            Nivel.Solucion => "solución",
            Nivel.Proyecto => "proyecto",
            Nivel.Archivo => "archivo",
            Nivel.Namespace => "namespace",
            Nivel.Tipo => "tipo",
            Nivel.Miembro => "miembro",
            Nivel.Sentencia => "sentencia",
            Nivel.Expresion => "expresión",
            _ => "?",
        };

        public string Planta => nivel switch
        {
            Nivel.Solucion => "raíz",
            Nivel.Proyecto => "tronco",
            Nivel.Archivo => "rama",
            Nivel.Namespace => "tallo",
            Nivel.Tipo => "brote",
            Nivel.Miembro => "hoja",
            Nivel.Sentencia => "flor",
            Nivel.Expresion => "semilla",
            _ => "?",
        };

        public bool EsExtremo => nivel is Nivel.Solucion or Nivel.Expresion;
    }

    extension(Nivel)
    {
        public static IReadOnlyList<Nivel> Todos => Enum.GetValues<Nivel>();

        public static Nivel Desde(string texto) =>
            Enum.GetValues<Nivel>().FirstOrDefault(n =>
                string.Equals(n.Nombre, texto, StringComparison.OrdinalIgnoreCase)
                || string.Equals(n.ToString(), texto, StringComparison.OrdinalIgnoreCase)
                || n.Kanji == texto,
                Nivel.Miembro);
    }
}
