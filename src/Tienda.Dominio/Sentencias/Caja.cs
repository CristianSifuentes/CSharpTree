namespace Tienda.Dominio.Sentencias;

using Tienda.Dominio.Catalogo;
using Tienda.Dominio.Extensiones;

// 芽 Un tipo cuyos métodos existen para mostrar el jardín de 花 sentencias y 種 expresiones.
public sealed class Caja
{
    private readonly Lock _candado = new();          // System.Threading.Lock: `lock` lo usa directo (C# 13)
    private decimal _acumulado;

    public decimal Acumulado => _acumulado;

    // 葉 método con `params` de colección (C# 13): cualquier colección, no solo arreglos.
    public decimal Cobrar(Pedido pedido, params ReadOnlySpan<Descuento> descuentos)
    {
        var total = pedido.Total;                                    // 花 declaración local

        if (pedido.EstaVacio)                                        // 花 if
        {
            return 0m;                                               // 花 return
        }

        foreach (var descuento in descuentos)                        // 花 foreach
        {
            total = descuento(total);                                // 種 llamada + asignación
        }

        lock (_candado)                                              // 花 lock
        {
            _acumulado += Redondear(total);
        }

        return Redondear(total);

        // 花 función local static: solo existe dentro de Cobrar y no captura nada.
        static decimal Redondear(decimal monto) => Math.Round(monto, 2, MidpointRounding.ToEven);
    }

    // 葉 iterador: `yield` convierte el cuerpo en una secuencia perezosa.
    public static IEnumerable<string> Ticket(Pedido pedido)
    {
        // 種 raw string literal (C# 11)
        yield return $"""
            ── {pedido.Cliente} ──
            """;

        for (var i = 0; i < pedido.Cantidad; i++)                   // 花 for
        {
            var linea = pedido[i];                                   // 種 indexador
            yield return $"{linea.Sku,-10}{linea.Cantidad,4} × {linea.PrecioUnitario,8:0.00}";
        }

        yield return $"TOTAL {pedido.Total:0.00}";
    }

    // 葉 switch como sentencia, while y do.
    public static string Describir(Categoria categoria)
    {
        string texto;
        switch (categoria)                                           // 花 switch
        {
            case Categoria.Cafe or Categoria.Te:
                texto = "bebida";
                break;
            case Categoria.Pan:
                texto = "comida";
                break;
            default:
                texto = "objeto";
                break;
        }

        var vueltas = 0;
        while (texto.Length < 6)                                     // 花 while
        {
            texto += '.';
            vueltas++;
        }

        do                                                           // 花 do
        {
            vueltas--;
        }
        while (vueltas > 0);

        return texto;
    }

    // 葉 try / catch con filtro / finally, using de declaración y throw.
    public static string Intentar(Func<decimal> calculo, StringWriter bitacora)
    {
        using var _ = new Registro(bitacora);                        // 花 using (declaración)
        try                                                          // 花 try
        {
            return calculo().ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
        }
        catch (InvalidOperationException ex) when (ex.Message.Length > 0)   // filtro `when`
        {
            return $"falló: {ex.Message}";
        }
        finally
        {
            bitacora.Write('·');
        }
    }

    // 葉 patrones de lista (C# 11) en una expresión switch.
    public static string Forma(int[] cantidades) => cantidades switch
    {
        [] => "vacío",
        [var unica] => $"una: {unica}",
        [var primera, .., var ultima] when primera == ultima => "simétrica",
        [_, _, ..] => $"{cantidades.Length} elementos",
    };

    // 葉 Lambdas con modificadores sin escribir tipos (C# 14): (texto, out valor) => …
    public delegate bool Lector(string texto, out int valor);

    public static Lector LectorEntero { get; } = (texto, out valor) => int.TryParse(texto, out valor);

    // 葉 asignación condicional con ?. (C# 14): si el pedido es null, no pasa nada.
    public static void Anotar(Pedido? pedido, string nota) => pedido?.Nota = nota;

    // 葉 nameof de un genérico sin argumentos (C# 14).
    public static string NombreDeLista => nameof(List<>);

    private sealed class Registro(StringWriter destino) : IDisposable   // 葉 tipo anidado con constructor primario
    {
        public void Dispose() => destino.Write('✓');
    }
}
