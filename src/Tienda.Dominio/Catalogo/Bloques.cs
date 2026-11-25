// 枝 Un archivo con namespaces de bloque: aquí sí pueden convivir varios, y anidarse.
//    (Con un namespace file-scoped esto no se puede: un solo namespace por archivo.)

namespace Tienda.Dominio.Inventario
{
    using Tienda.Dominio.Catalogo;                // 茎 un using también puede vivir dentro de un namespace

    public sealed class Almacen
    {
        private readonly Dictionary<string, int> _existencias = [];

        public int this[string sku]
        {
            get => _existencias.GetValueOrDefault(sku);
            set => _existencias[sku] = value;
        }

        public bool Surtir(Producto producto, int cantidad)
        {
            var hay = this[producto.Id];
            if (hay < cantidad) return false;
            this[producto.Id] = hay - cantidad;
            Auditoria.Bitacora.Anotar($"salida {producto.Id} × {cantidad}");
            return true;
        }
    }

    // 茎 namespace anidado: Tienda.Dominio.Inventario.Auditoria. El árbol vuelve a brotar 茎.
    namespace Auditoria
    {
        internal static class Bitacora
        {
            private static readonly List<string> s_entradas = [];
            public static IReadOnlyList<string> Entradas => s_entradas;
            public static void Anotar(string texto) => s_entradas.Add(texto);
        }
    }
}

// 茎 Un segundo namespace hermano en el mismo archivo.
namespace Tienda.Dominio.Envios
{
    public static class Tarifas
    {
        public static decimal PorZona(int zona) => zona switch
        {
            1 => 99m,
            2 => 149m,
            >= 3 and <= 5 => 199m,
            _ => throw new ArgumentOutOfRangeException(nameof(zona), zona, "Zona desconocida."),
        };
    }
}
