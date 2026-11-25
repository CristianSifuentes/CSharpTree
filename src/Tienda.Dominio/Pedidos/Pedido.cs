// 枝 archivo · 茎 namespace file-scoped (C# 10): todo lo que sigue cae dentro de él.
namespace Tienda.Dominio.Pedidos;

/// <summary>
/// 芽 Un tipo. Todo lo que hay entre sus llaves es un miembro (葉), y todos los miembros son hermanos:
/// el orden en que se escriben no cambia su nivel.
/// </summary>
public sealed partial class Pedido
{
    // 葉 constante: un valor fijo en tiempo de compilación.
    public const int MaxLineas = 50;

    // 葉 campo static: uno para todo el tipo.
    private static int s_creados;

    // 葉 campo de instancia: uno por cada pedido.
    private readonly List<Linea> _lineas = [];          // 種 expresión de colección (C# 12)

    // 葉 tipo anidado: un tipo que además es miembro. Aquí el árbol vuelve a brotar 芽.
    public enum Fase { Nuevo, Pagado, Enviado }

    // 葉 propiedad con `field` (C# 14): el campo oculto existe, pero no hay que declararlo.
    public string Cliente
    {
        get;
        set => field = string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("El cliente no puede estar vacío.", nameof(value))
            : value.Trim();
    } = "";

    // 葉 propiedades automáticas.
    public Fase FaseActual { get; private set; } = Fase.Nuevo;
    public string? Nota { get; set; }

    // 葉 indexador: el tipo se deja leer como un arreglo.
    public Linea this[int indice] => _lineas[indice];

    // 葉 propiedades calculadas.
    public int Cantidad => _lineas.Count;
    public decimal Total => _lineas.Sum(l => l.Importe);
    public static int Creados => s_creados;

    // 葉 constructor partial (C# 14): aquí la declaración, la implementación vive en Pedido.Partes.cs.
    public partial Pedido(string cliente);

    // 葉 evento partial (C# 14): misma idea, otra rama del mismo tipo.
    public partial event Action<Pedido>? Cambio;

    // 葉 método: un miembro con cuerpo. Dentro empiezan las sentencias (花).
    public Pedido Agregar(Linea linea)
    {
        ArgumentNullException.ThrowIfNull(linea);                       // 花 sentencia de expresión
        if (_lineas.Count >= MaxLineas)                                  // 花 if
        {
            throw new InvalidOperationException($"Un pedido admite como máximo {MaxLineas} líneas.");
        }

        _lineas.Add(linea);
        _cambio?.Invoke(this);
        return this;                                                     // 花 return
    }

    public void Pagar() => FaseActual = FaseActual is Fase.Nuevo
        ? Fase.Pagado
        : throw new InvalidOperationException($"No se puede pagar un pedido en fase {FaseActual}.");

    // 葉 operador binario: siempre public static.
    public static Pedido operator +(Pedido pedido, Linea linea) => pedido.Agregar(linea);

    // 葉 operador de asignación compuesta (C# 14): de instancia, modifica el pedido en su sitio.
    public void operator +=(Linea linea) => Agregar(linea);

    public override string ToString() => $"Pedido de {Cliente}: {Cantidad} línea(s), {Total:0.00}";
}
