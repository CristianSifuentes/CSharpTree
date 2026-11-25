# 樹形 · CSharpTree

> El compañero en código del PDF *El árbol de C#*. Una solución .NET 10 / C# 14 que **contiene** cada nivel del árbol, **se lee a sí misma** con Roslyn para dibujarse como planta y **compila cada afirmación** del PDF para ver si es cierta.

```
根 CSharpTree.slnx                     la raíz: solo agrupa, el compilador nunca la ve
├─ 幹 src/Tienda.Dominio               el espécimen: una tienda donde crece cada rama de C#
├─ 幹 src/Arbol.App      → arbol.exe   el jardinero: lee, dibuja y verifica
└─ 幹 tests/Arbol.Pruebas              el juez: 140 pruebas que confirman el PDF
```

## Cómo correrlo

```bash
dotnet --version                       # 10.0.x
dotnet build                           # 0 warnings: TreatWarningsAsErrors está activado
dotnet test                            # 140 pruebas
dotnet run --project src/Arbol.App     # todo: censo, árbol, rutas, verificación y láminas
```

Comandos sueltos:

| comando | qué hace |
|---|---|
| `arbol --hasta tipo` | dibuja el árbol hasta un nivel (`solución`…`expresión`, o su kanji: `--hasta 葉`) |
| `arbol --hasta expresión --proyecto Tienda.Dominio/Pedidos/Pedido.cs` | baja hasta las semillas de un solo archivo |
| `censo` | cuántos nodos hay en cada nivel |
| `ruta Agregar` | dónde vive un nodo y quiénes son sus hermanos (mismo nivel) |
| `verificar` | compila las 124 afirmaciones y la matriz; escribe `salida/verificacion.md` |
| `herbario --proyecto Tienda.Dominio --hasta miembro` | genera una lámina SVG del árbol real |

## Dónde vive cada nivel en el código

| nivel | dónde verlo |
|---|---|
| 根 solución | `CSharpTree.slnx` (formato XML del SDK de .NET 9+) · `Directory.Build.props` reparte reglas a todos los proyectos |
| 幹 proyecto | los tres `.csproj`; `Arbol.App` compila a `arbol.exe`, los otros a `.dll` |
| 枝 archivo | `Ensamblado.cs` (solo `global using`, `using` y `[assembly:]`) · `Program.cs` (el único con top-level statements) |
| 茎 namespace | file-scoped en casi todos; `Catalogo/Bloques.cs` tiene dos namespaces de bloque y uno anidado |
| 芽 tipo | `Catalogo/Tipos.cs`: los seis hermanos (class, struct, record, interface, enum, delegate) + ref struct + herencia |
| 葉 miembro | `Pedidos/Pedido.cs`: campo, constante, propiedad con `field`, indexador, constructor y evento **partial**, operador `+` y `+=`, tipo anidado |
| 花 sentencia | `Sentencias/Caja.cs`: declaración, if, foreach, for, switch, while, do, try/catch/finally, using, lock, yield, función local |
| 種 expresión | `Caja.cs`: patrones de lista, raw strings, lambdas con `out` sin tipos, `?.=`, `nameof(List<>)` |

La propia app usa lo que enseña: `Raices/Nivel.cs` le presta propiedades (`Kanji`, `Planta`…) al enum `Nivel` con un **bloque `extension`** de C# 14.

## Lo que dijo el compilador

`arbol verificar` mete cada regla del PDF en un fragmento de código, lo compila en memoria con Roslyn 5 (C# 14) y compara:

- **Ubicación, archivo, herencia, miembros:** 39 casos con su código de error (CS0116, CS8802, CS1529, CS0525, CS1106, CS1109, CS1721, CS0509, CS0527, CS0146, CS0144, CS0506, CS0507, CS0708, CS0715…).
- **Novedades C# 9 → 14:** 25 casos que compilan con su versión **y fallan con la anterior**.
- **La matriz miembro × tipo:** 60 celdas, cada una compilada en forma natural y en forma static.

Tres detalles del PDF se corrigieron gracias a esto:

1. `private int x;` dentro de un método **no** da CS0106: el parser lo lee como el fin del método (CS1513). CS0106 aparece con `static int x;`.
2. Matriz: un operador en una interfaz compila tal cual (●), no «con condiciones».
3. Matriz: el bloque `extension` en una static class es ●; sus condiciones (no genérica → CS9283, de primer nivel) van en la nota.

## Salidas

`salida/` guarda lo que genera `todo`: `verificacion.md`, `consola.txt`, `herbario-dominio.svg` y `herbario-solucion.svg`.

---

cristian_ · ARTE · CÓDIGO · IA  ·  `› while (vivo) crecer();`
