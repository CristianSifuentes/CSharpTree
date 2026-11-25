namespace Tienda.Dominio.Pedidos;

// 芽 record (C# 9): igualdad por valor, deconstrucción y `with` gratis.
public sealed record Linea(string Sku, int Cantidad, decimal PrecioUnitario)
{
    // 葉 Un record también puede tener miembros propios.
    public decimal Importe => Cantidad * PrecioUnitario;
}

// 芽 record struct (C# 10): lo mismo, pero de valor.
public readonly record struct Dinero(decimal Monto, string Moneda = "MXN")
{
    public override string ToString() => $"{Monto:0.00} {Moneda}";
}
