namespace Tienda.Dominio.Extensiones;

using Tienda.Dominio.Catalogo;

// 芽 static class: no genérica y de primer nivel. Es el único hogar posible de las extensiones
//    (CS1106 si es genérica, CS1109 si está anidada).
public static class PedidoExtensiones
{
    // 葉 bloque extension (C# 14): miembros de instancia "prestados" a Pedido, incluidas propiedades.
    extension(Pedido pedido)
    {
        public bool EstaVacio => pedido.Cantidad == 0;

        public Dinero TotalEn(string moneda) => new(pedido.Total, moneda);

        public Pedido Con(Producto producto, int cantidad = 1) => pedido.Agregar(producto.ComoLinea(cantidad));
    }

    // 葉 bloque extension static (C# 14): se llama como Pedido.Para("Ana").
    extension(Pedido)
    {
        public static Pedido Para(string cliente) => new(cliente);
    }

    // 葉 método de extensión clásico (C# 3): sigue funcionando junto a los bloques.
    public static string Resumen(this Pedido pedido) =>
        pedido.EstaVacio ? $"{pedido.Cliente}: sin líneas" : $"{pedido.Cliente}: {pedido.TotalEn("MXN")}";
}
