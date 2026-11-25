using System.Text;
using Arbol.App.Dibujo;
using Arbol.App.Raices;
using Arbol.App.Verificacion;

namespace Arbol.App;

/// <summary>Cada comando de la app es una forma distinta de mirar el mismo árbol.</summary>
internal static class Comandos
{
    public static int Arbol(Terminal t, string slnx, Nivel hasta, string? proyecto)
    {
        var raiz = Leer(slnx, proyecto);
        t.Titulo(hasta.Kanji, $"El árbol de {raiz.Etiqueta}, hasta {hasta.Nombre} ({hasta.Planta})");
        t.Arbol(raiz, hasta);
        return 0;
    }

    public static int Censo(Terminal t, string slnx)
    {
        var raiz = LectorDeArbol.LeerSolucion(slnx);
        t.Titulo("数", "Censo de la planta · cuántos nodos hay en cada nivel");
        t.Censo(raiz);
        return 0;
    }

    public static int Ruta(Terminal t, string slnx, string busqueda)
    {
        var raiz = LectorDeArbol.LeerSolucion(slnx);
        var candidatos = raiz.Descendientes().Where(n => n.Etiqueta.Contains(busqueda, StringComparison.OrdinalIgnoreCase)).ToList();
        var nodo = candidatos.FirstOrDefault(n => n.Etiqueta.EndsWith(busqueda, StringComparison.OrdinalIgnoreCase)) ?? candidatos.FirstOrDefault();
        if (nodo is null)
        {
            t.Linea($"  No encontré «{busqueda}» en el árbol.");
            return 1;
        }

        t.Titulo("道", $"¿Dónde vive «{nodo.Etiqueta}»? · {nodo.Nivel.Kanji} {nodo.Nivel.Nombre}");
        t.Ruta(nodo);
        return 0;
    }

    public static int Verificar(Terminal t, string carpetaSalida)
    {
        var veredictos = new List<(string Grupo, Veredicto V)>();
        foreach (var (titulo, lista) in Afirmaciones.Grupos)
        {
            t.Titulo("証", titulo);
            foreach (var afirmacion in lista)
            {
                var v = afirmacion.Comprobar();
                veredictos.Add((titulo, v));
                t.Veredicto(v.Cumple, afirmacion.Id, $"{afirmacion.Kanji} {afirmacion.Texto}", v.Observado);
            }
        }

        t.Titulo("表", "¿Qué hoja crece en qué brote? · la matriz, compilada celda por celda");
        var celdas = Matriz.Verificar().ToList();
        t.Linea($"  {"",-20}{string.Concat(Matriz.Contenedores.Select(c => c.PadRight(14)))}");
        foreach (var fila in celdas.GroupBy(c => c.Miembro))
        {
            var linea = new StringBuilder($"  {fila.Key,-20}");
            foreach (var celda in fila)
            {
                var simbolo = celda.Observado.Simbolo();
                linea.Append(celda.Cumple ? t.Tinta(simbolo) : t.Pinta(simbolo, true)).Append(new string(' ', 13));
            }

            t.Linea(linea.ToString());
        }

        var fallas = veredictos.Count(v => !v.V.Cumple) + celdas.Count(c => !c.Cumple);
        var total = veredictos.Count + celdas.Count;
        t.Linea();
        t.Linea($"  {t.Pinta("●", fallas > 0)} {total - fallas} de {total} afirmaciones confirmadas por el compilador{(fallas > 0 ? $" · {fallas} en rojo" : "")}.");

        Directory.CreateDirectory(carpetaSalida);
        var ruta = Path.Combine(carpetaSalida, "verificacion.md");
        File.WriteAllText(ruta, Informe.Markdown(veredictos, celdas));
        t.Linea($"  {t.Pinta($"informe: {Path.GetRelativePath(Environment.CurrentDirectory, ruta)}", false)}");
        return fallas == 0 ? 0 : 1;
    }

    public static int Herbario(Terminal t, string slnx, string? proyecto, Nivel hasta, string rutaSvg)
    {
        var raiz = Leer(slnx, proyecto);
        var titulo = proyecto is null ? "El árbol de la solución" : $"El árbol de {Path.GetFileNameWithoutExtension(raiz.Hijos[0].Etiqueta)}";
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(rutaSvg))!);
        File.WriteAllText(rutaSvg, Dibujo.Herbario.Dibujar(raiz, hasta, titulo));
        t.Titulo("画", "Lámina de herbario generada desde el código real");
        t.Linea($"  {Path.GetRelativePath(Environment.CurrentDirectory, rutaSvg)}  ({raiz.Podar(hasta).Descendientes().Count() + 1} nodos hasta {hasta.Nombre})");
        return 0;
    }

    public static int Todo(Terminal t, string slnx, string carpetaSalida)
    {
        t.Portada("de la solución a la expresión · leído con Roslyn · C# 14 · .NET 10");
        Censo(t, slnx);
        Arbol(t, slnx, Nivel.Tipo, null);
        Arbol(t, slnx, Nivel.Expresion, "Tienda.Dominio/Pedidos/Pedido.Partes.cs");
        Ruta(t, slnx, "método Agregar()");
        Ruta(t, slnx, "extension(Pedido pedido)");
        var codigo = Verificar(t, carpetaSalida);
        Herbario(t, slnx, "Tienda.Dominio", Nivel.Miembro, Path.Combine(carpetaSalida, "herbario-dominio.svg"));
        Herbario(t, slnx, null, Nivel.Tipo, Path.Combine(carpetaSalida, "herbario-solucion.svg"));
        t.Linea();
        return codigo;
    }

    /// <summary>
    /// Lee la solución completa o solo una parte: un proyecto ("Tienda.Dominio")
    /// o un archivo ("Tienda.Dominio/Pedidos/Pedido.cs"). La ruta desde la raíz se conserva.
    /// </summary>
    public static Nodo Leer(string slnx, string? filtro)
    {
        var raiz = LectorDeArbol.LeerSolucion(slnx);
        if (filtro is null)
        {
            return raiz;
        }

        var partes = filtro.Split('/', 2);
        var proyecto = raiz.Hijos.FirstOrDefault(p => p.Etiqueta.StartsWith(partes[0], StringComparison.OrdinalIgnoreCase))
            ?? throw new ArgumentException($"No hay un proyecto «{partes[0]}» en {raiz.Etiqueta}.");

        var podada = new Nodo(raiz.Nivel, raiz.Etiqueta, raiz.Detalle);
        var copiaProyecto = podada.Agregar(new Nodo(proyecto.Nivel, proyecto.Etiqueta, proyecto.Detalle));
        foreach (var archivo in proyecto.Hijos.Where(a => partes.Length == 1 || a.Etiqueta.Replace('\\', '/') == partes[1]))
        {
            copiaProyecto.Agregar(archivo.Podar(Nivel.Expresion));
        }

        return podada;
    }

    public static string BuscarSolucion()
    {
        foreach (var inicio in new[] { Environment.CurrentDirectory, AppContext.BaseDirectory })
        {
            for (var carpeta = new DirectoryInfo(inicio); carpeta is not null; carpeta = carpeta.Parent)
            {
                var slnx = carpeta.EnumerateFiles("*.slnx").FirstOrDefault();
                if (slnx is not null)
                {
                    return slnx.FullName;
                }
            }
        }

        throw new FileNotFoundException("No encontré ningún .slnx subiendo desde la carpeta actual.");
    }
}
