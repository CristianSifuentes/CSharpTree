// 枝 Program.cs: el único archivo del proyecto con top-level statements (CS8802 si hubiera otro).
//    El compilador envuelve estas líneas en un Main invisible; usings arriba, sentencias después (CS1529).

using System.Text;
using Arbol.App;
using Arbol.App.Dibujo;
using Arbol.App.Raices;

Console.OutputEncoding = Encoding.UTF8;

var terminal = Terminal.Consola();
var slnx = Comandos.BuscarSolucion();
var salida = Path.Combine(Path.GetDirectoryName(slnx)!, "salida");

string? Opcion(string nombre) =>
    args.SkipWhile(a => a != nombre).Skip(1).FirstOrDefault();

var nivel = Opcion("--hasta") is { } texto ? Nivel.Desde(texto) : Nivel.Miembro;
var proyecto = Opcion("--proyecto");

return args.FirstOrDefault() switch
{
    "arbol" => Comandos.Arbol(terminal, slnx, nivel, proyecto),
    "censo" => Comandos.Censo(terminal, slnx),
    "ruta" when args.Length > 1 => Comandos.Ruta(terminal, slnx, string.Join(' ', args.Skip(1))),
    "verificar" => Comandos.Verificar(terminal, salida),
    "herbario" => Comandos.Herbario(terminal, slnx, proyecto, nivel, Opcion("--salida") ?? Path.Combine(salida, "herbario.svg")),
    null or "todo" => Comandos.Todo(terminal, slnx, salida),
    _ => Ayuda(),
};

int Ayuda()
{
    Console.WriteLine("""

      樹形 · arbol <comando>

        todo                       portada, censo, árbol, rutas, verificación y láminas
        arbol [--hasta nivel] [--proyecto P[/archivo.cs]]
                                   dibuja el árbol (nivel: solución…expresión o su kanji)
        censo                      cuántos nodos hay en cada nivel
        ruta <texto>               dónde vive un nodo y quiénes son sus hermanos
        verificar                  compila cada afirmación del PDF y la matriz
        herbario [--proyecto P] [--hasta nivel] [--salida archivo.svg]
                                   lámina SVG del árbol real

    """);
    return 2;
}
