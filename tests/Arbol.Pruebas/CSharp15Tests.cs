using Arbol.App.Raices;
using Tienda.Dominio.Catalogo;
using Tienda.Dominio.Extensiones;
using Tienda.Dominio.Pedidos;
using Tienda.Dominio.Sentencias;

namespace Arbol.Pruebas;

/// <summary>Los brotes de C# 15 que ya viven en el espécimen, probados en ejecución.</summary>
public sealed class CSharp15Tests
{
    [Fact]
    public void Una_union_guarda_uno_de_sus_casos()
    {
        ResultadoPago pago = new Aprobado("A-77", 120m);      // conversión implícita desde el caso
        ResultadoPago espera = new Pendiente(new DateTimeOffset(2026, 11, 25, 13, 5, 0, TimeSpan.Zero));

        Assert.True(pago.EsFinal);
        Assert.False(espera.EsFinal);
        Assert.Equal("aprobado A-77 por 120.00", pago.Describir());
        Assert.IsType<Aprobado>(pago.Value);
        Assert.True(typeof(ResultadoPago).IsValueType);       // por dentro, un struct
    }

    [Fact]
    public void Una_jerarquia_closed_es_abstract_y_exhaustiva()
    {
        Assert.True(typeof(EstadoEnvio).IsAbstract);
        Assert.Equal("preparando", Envios.Etiqueta(EstadoEnvio.Inicial));
        Assert.Equal("en camino (MX-1)", Envios.Etiqueta(new EnCamino("MX-1")));
    }

    [Fact]
    public void El_indexador_de_extension_busca_por_sku()
    {
        var cafe = new Producto { Id = "CAFE-01", Nombre = "Café", Precio = 45m };
        var pedido = Pedido.Para("Noa").Con(cafe, 2);

        Assert.Equal(2, pedido["CAFE-01"]?.Cantidad);
        Assert.Null(pedido["TE-99"]);
    }

    [Fact]
    public void Break_y_continue_con_etiqueta()
    {
        int[][] tablero = [[1, 2, 3], [-1, 4, 6], [8, 10, 12]];

        var pares = Caja.PrimerosPares(tablero, 3);

        Assert.Equal([(0, 1), (2, 0), (2, 1)], pares);       // la fila con -1 se salta entera
    }

    [Fact]
    public void El_lector_ve_la_union_como_un_tipo_de_csharp_15()
    {
        var archivo = LectorDeArbol.LeerCodigo("record A; record B; union U(A, B) { public int N => 1; }");
        var union = archivo.Descendientes().Single(n => n.Etiqueta.StartsWith("union", StringComparison.Ordinal));

        Assert.Equal(Nivel.Tipo, union.Nivel);
        Assert.Equal("C# 15", union.Novedad);
        Assert.Contains(union.Hijos, h => h.Etiqueta == "propiedad N");
    }
}
