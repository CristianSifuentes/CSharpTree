using Arbol.App.Raices;

namespace Arbol.Pruebas;

/// <summary>El lector debe ver la misma jerarquía que el PDF dibuja: quién contiene a quién y quién es hermano de quién.</summary>
public sealed class LectorTests
{
    private const string Codigo = """
        using System;

        namespace Tienda;

        public class Pedido
        {
            private int _n;
            public enum Fase { Nuevo }

            public int Sumar(int x)
            {
                var y = x + _n;
                return y;

                static int Doble(int z) => z * 2;
            }
        }

        public static class Extras
        {
            extension(Pedido p)
            {
                public bool Vacio => true;
            }
        }
        """;

    private static readonly Nodo s_archivo = LectorDeArbol.LeerCodigo(Codigo);

    private static Nodo Buscar(string etiqueta) => s_archivo.Descendientes().First(n => n.Etiqueta == etiqueta);

    [Fact]
    public void La_ruta_baja_nivel_por_nivel_de_archivo_a_expresion()
    {
        var expresion = Buscar("declaración y").Hijos.Single();

        var niveles = expresion.Ruta().Select(n => n.Nivel);

        Assert.Equal([Nivel.Archivo, Nivel.Namespace, Nivel.Tipo, Nivel.Miembro, Nivel.Sentencia, Nivel.Expresion], niveles);
    }

    [Fact]
    public void Los_miembros_son_hermanos_aunque_uno_sea_un_tipo_anidado()
    {
        var sumar = Buscar("método Sumar()");

        Assert.Contains(sumar.Hermanos, h => h.Etiqueta == "campo _n");
        Assert.Contains(sumar.Hermanos, h => h is { Etiqueta: "enum Fase", Nivel: Nivel.Tipo });
        Assert.Equal("class Pedido", sumar.Padre!.Etiqueta);
    }

    [Fact]
    public void Un_tipo_nunca_cuelga_de_un_metodo()
    {
        var tipos = s_archivo.Descendientes().Where(n => n.Nivel == Nivel.Tipo);

        Assert.All(tipos, t => Assert.True(t.Padre!.Nivel is Nivel.Namespace or Nivel.Tipo or Nivel.Archivo));
    }

    [Fact]
    public void La_funcion_local_es_una_sentencia_del_metodo()
    {
        var doble = Buscar("función local Doble");

        Assert.Equal(Nivel.Sentencia, doble.Nivel);
        Assert.Equal("método Sumar()", doble.Padre!.Etiqueta);
    }

    [Fact]
    public void El_bloque_extension_es_un_miembro_de_csharp_14()
    {
        var bloque = Buscar("extension(Pedido p)");

        Assert.Equal(Nivel.Miembro, bloque.Nivel);
        Assert.Equal("C# 14", bloque.Novedad);
        Assert.Contains(bloque.Hijos, h => h.Etiqueta == "propiedad Vacio");
    }

    [Fact]
    public void Using_y_namespace_viven_en_el_archivo()
    {
        Assert.Equal(["using System", "namespace Tienda"], s_archivo.Hijos.Select(h => h.Etiqueta));
    }

    [Fact]
    public void La_solucion_real_tiene_los_tres_proyectos()
    {
        var slnx = Arbol.App.Comandos.BuscarSolucion();

        var raiz = LectorDeArbol.LeerSolucion(slnx);

        Assert.Equal(Nivel.Solucion, raiz.Nivel);
        Assert.Equal(["Tienda.Dominio.csproj", "Arbol.App.csproj", "Arbol.Pruebas.csproj"], raiz.Hijos.Select(h => h.Etiqueta));
    }
}
