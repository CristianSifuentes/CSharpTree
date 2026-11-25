# 証 Verificación de «El árbol de C#»

Compilador: Roslyn 5.12.0.0 · C# 14 · .NET 11.0.0

**151 de 151** afirmaciones confirmadas compilando código real.

## Cada cosa en su rama · reglas de ubicación

| | id | afirmación | código | el compilador dice |
|---|---|---|---|---|
| ✓ | U1 | 芽 Un tipo nunca va dentro de un método | `class C { void M() { class X { } } }` | CS1513 CS1022 |
| ✓ | U1b | 芽 Un tipo sí puede ir dentro de otro tipo (anidado) | `namespace App { class C { enum Fase { A } } }` | compila |
| ✓ | U2 | 葉 Un miembro suelto en un namespace es CS0116 | `namespace App; int contador;` | CS0116 |
| ✓ | U2b | 葉 Lo más parecido a un global: miembro static de clase static | `namespace App; static class Config { public static int Max = 3; }` | compila |
| ✓ | U3 | 枝 Top-level en dos archivos es CS8802 | `System.Console.WriteLine(1); ⏎ System.Console.WriteLine(2);` | CS8802 |
| ✓ | U3b | 枝 Top-level en un solo archivo compila | `System.Console.WriteLine(1); ⏎ class Otro { }` | compila |
| ✓ | U3c | 枝 Un using después de código es CS1529 | `System.Console.WriteLine(1); ⏎ using System.Linq;` | CS1529 |
| ✓ | U3d | 枝 Top-level después de un namespace es CS8803 | `namespace A { } ⏎ System.Console.WriteLine(1);` | CS8803 |
| ✓ | U4 | 芽 Una interfaz no tiene campos de instancia: CS0525 | `interface I { int x; }` | CS0525 |
| ✓ | U4b | 芽 …pero sí propiedades | `interface I { int X { get; } }` | compila |
| ✓ | U5 | 葉 Extensión en clase genérica: CS1106 | `static class E<T> { static void M(this int x) { } }` | CS1106 |
| ✓ | U5b | 葉 Extensión en clase no static: CS1106 | `class E { static void M(this int x) { } }` | CS1106 |
| ✓ | U5c | 葉 Extensión en clase anidada: CS1109 | `static class A { static class E { static void M(this int x) { } } }` | CS1109 |
| ✓ | U5d | 葉 Bloque extension en clase static de primer nivel compila | `static class E { extension(int x) { public bool Par => x % 2 == 0; } }` | compila |
| ✓ | U5e | 葉 Bloque extension en clase genérica no compila | `static class E<T> { extension(int x) { public bool Par => x % 2 == 0; } }` | CS9283 |

## Dos árboles que se cruzan · archivo y namespace

| | id | afirmación | código | el compilador dice |
|---|---|---|---|---|
| ✓ | A1 | 茎 file-scoped y namespace de bloque no se mezclan | `namespace A; ⏎ namespace B { }` | CS8955 |
| ✓ | A2 | 茎 Varios namespaces de bloque sí conviven | `namespace A { } ⏎ namespace B { namespace C { } }` | compila |
| ✓ | A3 | 茎 Un namespace no es una carpeta: se reparte en archivos | `namespace Tienda { class A { } } ⏎ namespace Tienda { class B { A a = new(); } }` | compila |
| ✓ | A4 | 芽 partial reparte un tipo entre archivos | `partial class P { int x; } ⏎ partial class P { int Y => x; }` | compila |

## Una sola savia · herencia

| | id | afirmación | código | el compilador dice |
|---|---|---|---|---|
| ✓ | H1 | 系 Una sola clase base: CS1721 | `class A { } class B { } class C : A, B { }` | CS1721 |
| ✓ | H1b | 系 Una base y muchas interfaces sí | `interface I { } interface J { } class A { } class C : A, I, J { }` | compila |
| ✓ | H2 | 系 sealed no tiene herederos: CS0509 | `sealed class A { } class B : A { }` | CS0509 |
| ✓ | H3 | 系 struct solo implementa interfaces: CS0527 | `class Base { } struct S : Base { }` | CS0527 |
| ✓ | H3b | 系 struct con interfaces sí | `struct S : System.IComparable<S> { public int CompareTo(S o) => 0; }` | compila |
| ✓ | H4 | 系 Sin ciclos: CS0146 | `class A : B { } class B : A { }` | CS0146 |
| ✓ | H5 | 系 abstract no se instancia: CS0144 | `abstract class F { } class U { object M() => new F(); }` | CS0144 |
| ✓ | H6 | 系 override solo sobre virtual/abstract/override: CS0506 | `class A { public void D() { } } class B : A { public override void D() { } }` | CS0506 |
| ✓ | H7 | 系 override con otra visibilidad: CS0507 | `class A { public virtual void D() { } } class B : A { protected override void D() { } }` | CS0507 |

## Miembros, sentencias y expresiones

| | id | afirmación | código | el compilador dice |
|---|---|---|---|---|
| ✓ | M1 | 葉 static class: miembro de instancia es CS0708 | `static class S { public int X; }` | CS0708 |
| ✓ | M2 | 葉 static class: operador es CS0715 | `static class S { public static int operator +(S a, S b) => 0; }` | CS0715 CS0721 |
| ✓ | M3 | 葉 struct no admite finalizador | `struct S { ~S() { } }` | CS0575 |
| ✓ | M4 | 葉 interface no admite constructor de instancia | `interface I { I() { } }` | CS0526 |
| ✓ | M5 | 葉 interface sí admite constructor static | `interface I { static I() { } }` | compila |
| ✓ | M6 | 葉 Los miembros son hermanos: el orden no importa | `class C { int Y => X; int X = 1; }` | compila |
| ✓ | S1 | 花 Un local con private no compila (es error de sintaxis) | `class C { void M() { private int x = 0; } }` | CS1513 CS1022 |
| ✓ | S1b | 花 Una función local con public no compila | `class C { void M() { public void F() { } } }` | CS1513 CS1022 |
| ✓ | S1c | 花 Un local con static tampoco | `class C { void M() { static int x = 0; } }` | CS0106 |
| ✓ | S2 | 花 Función local static no captura: CS8421 | `class C { int M() { int a = 1; return F(); static int F() => a; } }` | CS8421 |
| ✓ | E1 | 種 Una sentencia suelta como expresión no vale: CS0201 | `class C { void M() { 1 + 1; } }` | CS0201 |

## Brotes nuevos · compila con su versión y falla con la anterior

| | id | afirmación | código | el compilador dice |
|---|---|---|---|---|
| ✓ | N9a | 枝 C# 9 · top-level statements | `System.Console.WriteLine(1);` | compila · antes: CS8400 |
| ✓ | N9b | 芽 C# 9 · record | `record R(int X);` | compila · antes: CS8805 CS8400 CS8112 CS0246 |
| ✓ | N9c | 葉 C# 9 · init | `class C { public int X { get; init; } }` | compila · antes: CS8400 |
| ✓ | N10a | 茎 C# 10 · file-scoped namespace | `namespace A; ⏎ class C { }` | compila · antes: CS8773 |
| ✓ | N10b | 茎 C# 10 · global using | `global using System; ⏎ class C { }` | compila · antes: CS8773 |
| ✓ | N10c | 芽 C# 10 · record struct | `record struct R(int X);` | compila · antes: CS8773 |
| ✓ | N11a | 芽 C# 11 · file class | `file class C { }` | compila · antes: CS8936 |
| ✓ | N11b | 葉 C# 11 · required | `class C { public required int X { get; init; } }` | compila · antes: CS8936 |
| ✓ | N11c | 葉 C# 11 · static abstract en interfaces | `interface I<T> where T : I<T> { static abstract T Crear(); }` | compila · antes: CS8703 |
| ✓ | N11d | 種 C# 11 · patrones de lista | `class C { bool M(int[] a) => a is [1, ..]; }` | compila · antes: CS8936 |
| ✓ | N11e | 種 C# 11 · raw strings | `class C { string S = """ ⏎   hola ⏎   """; }` | compila · antes: CS8936 |
| ✓ | N12a | 芽 C# 12 · constructor primario en class | `class C(int x) { int X => x; }` | compila · antes: CS9058 |
| ✓ | N12b | 種 C# 12 · expresiones de colección | `class C { int[] A = [1, 2, 3]; }` | compila · antes: CS9058 |
| ✓ | N12c | 枝 C# 12 · using alias de cualquier tipo | `using Par = (int, int); ⏎ class C { Par P; }` | compila · antes: CS9058 |
| ✓ | N13a | 花 C# 13 · params con colecciones | `class C { void M(params System.ReadOnlySpan<int> xs) { } }` | compila · antes: CS9202 |
| ✓ | N13b | 葉 C# 13 · propiedades partial | `partial class C { public partial int X { get; } } ⏎ partial class C { public partial int X => 1; }` | compila · antes: CS8703 |
| ✓ | N13c | 芽 C# 13 · ref struct que implementa interfaces | `ref struct R : System.IDisposable { public void Dispose() { } }` | compila · antes: CS9202 |
| ✓ | N14a | 葉 C# 14 · field en propiedades | `class C { public string N { get; set => field = value.Trim(); } = ""; }` | compila · antes: CS9260 CS0103 |
| ✓ | N14b | 葉 C# 14 · bloques extension | `static class E { extension(int x) { public bool Par => x % 2 == 0; } }` | compila · antes: CS1513 CS1022 CS9260 CS0710 CS0708 CS0103 |
| ✓ | N14c | 葉 C# 14 · constructores partial | `partial class C { public partial C(int x); } ⏎ partial class C { public partial C(int x) { } }` | compila · antes: CS0246 CS0501 CS0542 CS0111 CS9260 CS0161 |
| ✓ | N14d | 葉 C# 14 · eventos partial | `partial class C { public partial event System.Action E; } ⏎ partial class C { public partial event System.Action E { add { } remove { } } }` | compila · antes: CS8703 |
| ✓ | N14e | 葉 C# 14 · operadores += propios | `class C { int n; public void operator +=(int x) => n += x; }` | compila · antes: CS9260 |
| ✓ | N14f | 花 C# 14 · lambdas con ref/out sin tipos | `delegate bool P(string s, out int v); ⏎ class C { P p = (s, out v) => int.TryParse(s, out v); }` | compila · antes: CS9260 |
| ✓ | N14g | 種 C# 14 · asignación con ?. | `class C { public int X; static void M(C? c) => c?.X = 1; }` | compila · antes: CS9260 |
| ✓ | N14h | 種 C# 14 · nameof(List<>) | `class C { string N = nameof(System.Collections.Generic.List<>); }` | compila · antes: CS9260 |

## Brotes de C# 15 · .NET 11

| | id | afirmación | código | el compilador dice |
|---|---|---|---|---|
| ✓ | N15a | 芽 C# 15 · union: un tipo nuevo | `record Gato; record Perro; union Mascota(Gato, Perro);` | compila · antes: CS8803 CS1001 CS8805 CS8112 CS0246 |
| ✓ | N15b | 芽 C# 15 · switch exhaustivo sobre una union | `record Gato; record Perro; union Mascota(Gato, Perro); class U { static string M(Mascota m) => m switch { Gato => "miau", Perro => "guau" }; static Mascota N() => new Gato(); }` | compila · antes: CS8803 CS1001 CS0246 CS8805 CS8112 |
| ✓ | N15c | 系 C# 15 · closed: jerarquía cerrada | `closed record Estado; sealed record Abierto : Estado; sealed record Cerrado : Estado;` | compila · antes: CS9327 |
| ✓ | N15d | 葉 C# 15 · indexadores en bloques extension | `using System.Linq; static class E { extension(System.Collections.Generic.IEnumerable<int> s) { public int this[int i] => s.ElementAt(i); } }` | compila · antes: CS9327 |
| ✓ | N15e | 花 C# 15 · break y continue con etiqueta | `class C { void M() { fuera: for (var i = 0; i < 3; i++) { for (var j = 0; j < 3; j++) { if (j == 1) continue fuera; if (i == 2) break fuera; } } } }` | compila · antes: CS9327 |
| ✓ | N15f | 種 C# 15 · argumentos en colecciones: with(...) | `using System.Collections.Generic; class C { List<int> L = [with(capacity: 10), 1, 2]; }` | compila · antes: CS9327 |
| ✓ | U15a | 芽 Una union tampoco va dentro de un método | `record Gato; record Perro; class C { void M() { union X(Gato, Perro); } }` | CS1001 CS8112 CS0246 |
| ✓ | U15b | 芽 Una union sí puede anidarse en otro tipo | `record Gato; record Perro; class C { public union X(Gato, Perro); }` | compila |
| ✓ | U15c | 芽 Una union admite métodos en su cuerpo | `record Gato; record Perro; union X(Gato, Perro) { public bool EsGato => Value is Gato; }` | compila |
| ✓ | U15d | 系 Nadie hereda de una union (es un struct) | `record Gato; record Perro; union X(Gato, Perro); class H : X { }` | CS0509 |
| ✓ | C15a | 系 closed no se combina con sealed: CS9381 | `closed sealed class A { }` | CS9381 |
| ✓ | C15b | 系 closed no se combina con abstract: CS9384 | `closed abstract class A { }` | CS9384 |
| ✓ | C15c | 系 Una clase closed no se instancia (es abstract): CS0144 | `closed class A { } class U { object M() => new A(); }` | CS0144 |
| ✓ | C15d | 系 closed: heredar desde otro ensamblado no compila | `sealed class Mia : Lejana.Base { }` | CS9382 |
| ✓ | C15e | 系 closed: desde el mismo ensamblado sí | `closed class Base { } sealed class Hija : Base { }` | compila |

## 表 Matriz miembro × tipo

● compila · ○ solo en su forma static · — no compila

| miembro | class | struct | interface | enum | static class | union |
|---|---|---|---|---|---|---|
| campo de instancia | ● | ● | — | — | — | — |
| campo static | ● | ● | ● | — | ● | ● |
| constante | ● | ● | ● | ● | ● | ● |
| propiedad | ● | ● | ● | — | ○ | ○ |
| indexador | ● | ● | ● | — | — | ● |
| método | ● | ● | ● | — | ○ | ● |
| constructor | ● | ● | ○ | — | ○ | ○ |
| finalizador | ● | — | — | — | — | — |
| evento | ● | ● | ● | — | ○ | ○ |
| operador | ● | ● | ● | — | — | ● |
| tipo anidado | ● | ● | ● | — | ● | ● |
| bloque extension | — | — | — | — | ● | — |

<details><summary>Detalle de cada celda</summary>

- campo de instancia en class: compila
- campo de instancia en struct: compila
- campo de instancia en interface: CS0525
- campo de instancia en enum: CS1041 CS1003
- campo de instancia en static class: CS0708
- campo de instancia en union: CS9373
- campo static en class: compila
- campo static en struct: compila
- campo static en interface: compila
- campo static en enum: CS1513 CS8803 CS0106 CS1022 CS8805
- campo static en static class: compila
- campo static en union: compila
- constante en class: compila
- constante en struct: compila
- constante en interface: compila
- constante en enum: compila
- constante en static class: compila
- constante en union: compila
- propiedad en class: compila
- propiedad en struct: compila
- propiedad en interface: compila
- propiedad en enum: CS1041 CS1003 CS1022
- propiedad en static class: natural: CS0708 · static: compila
- propiedad en union: natural: CS9373 · static: compila
- indexador en class: compila
- indexador en struct: compila
- indexador en interface: compila
- indexador en enum: CS1041 CS1001 CS1003 CS1022 CS0246
- indexador en static class: CS0720
- indexador en union: compila
- método en class: compila
- método en struct: compila
- método en interface: compila
- método en enum: CS1041 CS1003 CS1022
- método en static class: natural: CS0708 · static: compila
- método en union: compila
- constructor en class: compila
- constructor en struct: compila
- constructor en interface: natural: CS0526 · static: compila
- constructor en enum: CS1513 CS8803 CS0106 CS1001 CS1022 CS8805 CS0161
- constructor en static class: natural: CS0710 · static: compila
- constructor en union: natural: CS9375 · static: compila
- finalizador en class: compila
- finalizador en struct: CS0575
- finalizador en interface: CS0575
- finalizador en enum: CS1001 CS1003 CS1022
- finalizador en static class: CS0711
- finalizador en union: CS0575
- evento en class: compila
- evento en struct: compila
- evento en interface: compila
- evento en enum: CS1041 CS1003
- evento en static class: natural: CS0708 · static: compila
- evento en union: natural: CS9373 · static: compila
- operador en class: compila
- operador en struct: compila
- operador en interface: compila
- operador en enum: CS1513 CS1022 CS0563
- operador en static class: CS0715 CS0721 CS0722
- operador en union: compila
- tipo anidado en class: compila
- tipo anidado en struct: compila
- tipo anidado en interface: compila
- tipo anidado en enum: CS1513 CS1022
- tipo anidado en static class: compila
- tipo anidado en union: compila
- bloque extension en class: CS9283
- bloque extension en struct: CS9283
- bloque extension en interface: CS9283
- bloque extension en enum: CS1003 CS1513 CS1022 CS0103
- bloque extension en static class: compila
- bloque extension en union: CS9283

</details>
