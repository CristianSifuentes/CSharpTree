// 枝 Seis brotes a la misma altura: class, struct, record, interface, enum y delegate son hermanos.
namespace Tienda.Dominio.Catalogo;

// 芽 enum: solo constantes con nombre sobre un entero.
public enum Categoria { Cafe, Te, Pan, Taza }

// 芽 delegate: la firma de un método convertida en tipo. No tiene miembros propios.
public delegate decimal Descuento(decimal monto);

// 芽 interface: un contrato. Admite static abstract (C# 11), constantes, campos static y métodos por defecto,
//    pero nunca campos de instancia (CS0525).
public interface IIdentificable<TSelf> where TSelf : IIdentificable<TSelf>
{
    const int LongitudMaxima = 32;                 // 葉 constante en interfaz (C# 8)
    static int Contador;                           // 葉 campo static en interfaz (C# 8)

    string Id { get; }                             // 葉 propiedad abstracta
    static abstract TSelf Crear(string id);        // 葉 miembro static abstract (C# 11)

    static string NuevoId() => $"ID-{Interlocked.Increment(ref Contador):0000}";

    bool EsValido() => Id.Length is > 0 and <= LongitudMaxima;   // 葉 método por defecto (C# 8)
}

// 芽 class con miembros required (C# 11) que implementa la interfaz con static abstract.
public sealed class Producto : IIdentificable<Producto>
{
    public required string Id { get; init; }
    public required string Nombre { get; init; }
    public Categoria Categoria { get; init; }
    public decimal Precio { get; init; }

    public static Producto Crear(string id) => new() { Id = id, Nombre = id };

    public Linea ComoLinea(int cantidad) => new(Id, cantidad, Precio);
}

// 芽 struct con constructor primario (C# 12): de valor, sin herencia de clases, solo interfaces.
public readonly struct Coordenada(double latitud, double longitud) : IEquatable<Coordenada>
{
    public double Latitud => latitud;
    public double Longitud => longitud;

    public bool Equals(Coordenada otra) => Latitud.Equals(otra.Latitud) && Longitud.Equals(otra.Longitud);
    public override bool Equals(object? obj) => obj is Coordenada otra && Equals(otra);
    public override int GetHashCode() => HashCode.Combine(Latitud, Longitud);

    public static bool operator ==(Coordenada a, Coordenada b) => a.Equals(b);
    public static bool operator !=(Coordenada a, Coordenada b) => !a.Equals(b);
}

// 芽 ref struct que implementa una interfaz (C# 13).
public ref struct Contador : IDisposable
{
    private int _abiertos;
    public readonly int Abiertos => _abiertos;
    public void Abrir() => _abiertos++;
    public void Dispose() => _abiertos = 0;
}

// 系 El linaje: una sola clase base, abstract no se instancia, override solo sobre virtual/abstract.
public abstract class Envio
{
    public abstract decimal Costo(decimal kilos);
    public virtual string Nombre => "envío estándar";
}

public class EnvioTerrestre : Envio
{
    public override decimal Costo(decimal kilos) => 99m + (kilos * 12m);
}

public sealed class EnvioExpres : EnvioTerrestre      // sealed: aquí se corta la rama
{
    public override decimal Costo(decimal kilos) => base.Costo(kilos) * 1.8m;
    public override string Nombre => "envío exprés";
}

// 芽 class con finalizador: el único tipo que lo admite es class (struct no, interface no).
public sealed class Recibo
{
    public static int Finalizados;
    ~Recibo() => Interlocked.Increment(ref Finalizados);
}
