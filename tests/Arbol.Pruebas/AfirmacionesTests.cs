using Arbol.App.Verificacion;

namespace Arbol.Pruebas;

/// <summary>Cada afirmación del PDF es una prueba: si el compilador no está de acuerdo, la prueba falla.</summary>
public sealed class AfirmacionesTests
{
    public static TheoryData<string> Ids => [.. Afirmaciones.Todas.Select(a => a.Id)];

    [Theory]
    [MemberData(nameof(Ids))]
    public void El_compilador_confirma_la_afirmacion(string id)
    {
        var afirmacion = Afirmaciones.Todas.Single(a => a.Id == id);

        var veredicto = afirmacion.Comprobar();

        Assert.True(veredicto.Cumple, $"{afirmacion.Kanji} {afirmacion.Texto}\n  el compilador dice: {veredicto.Observado}\n  {veredicto.Mensaje}");
    }

    public static TheoryData<string, string> Celdas
    {
        get
        {
            var datos = new TheoryData<string, string>();
            foreach (var fila in Matriz.Filas)
            {
                foreach (var contenedor in Matriz.Contenedores)
                {
                    datos.Add(fila.Miembro, contenedor);
                }
            }

            return datos;
        }
    }

    [Theory]
    [MemberData(nameof(Celdas))]
    public void La_matriz_del_pdf_coincide_con_el_compilador(string miembro, string contenedor)
    {
        var celda = Matriz.Verificar().Single(c => c.Miembro == miembro && c.Contenedor == contenedor);

        Assert.True(celda.Cumple, $"{miembro} en {contenedor}: el PDF dice {celda.Esperado.Simbolo()}, el compilador {celda.Observado.Simbolo()} ({celda.Detalle})");
    }

    [Fact]
    public void Los_ids_no_se_repiten() =>
        Assert.Equal(Afirmaciones.Todas.Count(), Afirmaciones.Todas.Select(a => a.Id).Distinct().Count());
}
