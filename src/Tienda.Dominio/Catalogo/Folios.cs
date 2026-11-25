namespace Tienda.Dominio.Catalogo;

// 芽 Un tipo público que usa un tipo `file`.
public static class Folios
{
    public static string Nuevo() => Generador.Siguiente();
}

// 芽 file class (C# 11): solo existe dentro de este archivo. Otro archivo puede tener su propio `Generador`.
file static class Generador
{
    private static int s_ultimo;
    public static string Siguiente() => $"F-{DateTime.UtcNow:yyyyMMdd}-{Interlocked.Increment(ref s_ultimo):000}";
}
