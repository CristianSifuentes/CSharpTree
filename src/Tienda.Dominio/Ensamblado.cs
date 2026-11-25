// 枝 Un archivo sin namespace ni tipos: solo lo que vive al nivel del archivo.
//    Orden obligatorio: extern alias → using → atributos [assembly:] → (sentencias) → namespaces y tipos.

global using Tienda.Dominio.Pedidos;     // 茎 global using: vale para todos los archivos del proyecto
global using System.Collections.Immutable;

using System.Reflection;                 // 茎 using normal: solo vale en este archivo
using System.Runtime.CompilerServices;

[assembly: AssemblyDescription("El espécimen del herbario: una tienda donde crece cada rama de C#.")]
[assembly: InternalsVisibleTo("arbol")]
