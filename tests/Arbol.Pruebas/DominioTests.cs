using Tienda.Dominio.Catalogo;
using Tienda.Dominio.Extensiones;
using Tienda.Dominio.Pedidos;
using Tienda.Dominio.Sentencias;

namespace Arbol.Pruebas;

/// <summary>El espécimen no solo compila: cada rama hace lo que el PDF dice que hace.</summary>
public sealed class DominioTests
{
    private static readonly Producto s_cafe = new() { Id = "CAFE-01", Nombre = "Café de olla", Categoria = Categoria.Cafe, Precio = 45m };

    [Fact]
    public void Field_en_la_propiedad_recorta_y_valida()
    {
        var pedido = Pedido.Para("  Ana  ");

        Assert.Equal("Ana", pedido.Cliente);
        Assert.Throws<ArgumentException>(() => pedido.Cliente = "   ");
    }

    [Fact]
    public void El_operador_compuesto_de_instancia_modifica_el_mismo_pedido()
    {
        var pedido = Pedido.Para("Luis");
        var mismo = pedido;

        pedido += s_cafe.ComoLinea(2);          // C# 14: llama a `void operator +=(Linea)`

        Assert.Same(mismo, pedido);
        Assert.Equal(90m, pedido.Total);
    }

    [Fact]
    public void El_constructor_y_el_evento_partial_funcionan()
    {
        var antes = Pedido.Creados;
        var pedido = new Pedido("Eva");
        var avisos = 0;
        pedido.Cambio += _ => avisos++;

        pedido.Con(s_cafe).Con(s_cafe, 3);

        Assert.True(Pedido.Creados > antes);
        Assert.Equal(2, avisos);
        Assert.Equal(4, pedido[0].Cantidad + pedido[1].Cantidad);
    }

    [Fact]
    public void Los_bloques_extension_prestan_propiedades_y_metodos_static()
    {
        var vacio = Pedido.Para("Sol");

        Assert.True(vacio.EstaVacio);
        Assert.Equal("Sol: sin líneas", vacio.Resumen());
        Assert.Equal(new Dinero(0m, "USD"), vacio.TotalEn("USD"));
    }

    [Fact]
    public void La_caja_florece_en_sentencias()
    {
        var caja = new Caja();
        var pedido = Pedido.Para("Iris").Con(s_cafe, 2);

        var cobrado = caja.Cobrar(pedido, m => m * 0.9m, m => m - 1m);

        Assert.Equal(80m, cobrado);
        Assert.Equal(3, Caja.Ticket(pedido).Count());
        Assert.Equal("bebida", Caja.Describir(Categoria.Te));
        Assert.Equal("objeto", Caja.Describir(Categoria.Taza));
    }

    [Fact]
    public void Patrones_lambdas_y_nulos_de_csharp_14()
    {
        Assert.Equal("simétrica", Caja.Forma([7, 1, 7]));
        Assert.Equal("vacío", Caja.Forma([]));
        Assert.True(Caja.LectorEntero("42", out var valor));
        Assert.Equal(42, valor);
        Assert.Equal("List", Caja.NombreDeLista);

        Caja.Anotar(null, "no explota");
        var pedido = Pedido.Para("Ari");
        Caja.Anotar(pedido, "urgente");
        Assert.Equal("urgente", pedido.Nota);
    }

    [Fact]
    public void La_herencia_sigue_su_linaje()
    {
        Envio envio = new EnvioExpres();

        Assert.Equal("envío exprés", envio.Nombre);
        Assert.Equal((99m + 24m) * 1.8m, envio.Costo(2));
        Assert.True(typeof(EnvioExpres).IsSealed);
        Assert.True(typeof(Envio).IsAbstract);
    }

    [Fact]
    public void Static_abstract_en_la_interfaz()
    {
        var producto = Crear<Producto>("TE-07");

        Assert.Equal("TE-07", producto.Id);
        Assert.True(((IIdentificable<Producto>)producto).EsValido());

        static T Crear<T>(string id) where T : IIdentificable<T> => T.Crear(id);
    }
}
