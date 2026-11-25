using System.Diagnostics;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Arbol.App.Raices;

/// <summary>
/// Lee una solución real con Roslyn y la convierte en el árbol del herbario:
/// 根 solución → 幹 proyecto → 枝 archivo → 茎 namespace → 芽 tipo → 葉 miembro → 花 sentencia → 種 expresión.
/// </summary>
public static class LectorDeArbol
{
    public static CSharpParseOptions Opciones { get; } =
        CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp15);

    /// <summary>Hasta qué profundidad se siguen las expresiones anidadas (a + b * f(c) es un árbol infinito en potencia).</summary>
    public static int MaxProfundidadExpresion { get; set; } = 2;

    // ─────────────────────────────── 根 · 幹 · 枝 (el árbol físico) ───────────────────────────────

    public static Nodo LeerSolucion(string rutaSlnx)
    {
        var carpeta = Path.GetDirectoryName(Path.GetFullPath(rutaSlnx))!;
        var raiz = new Nodo(Nivel.Solucion, Path.GetFileName(rutaSlnx), "solo agrupa: el compilador nunca la ve");

        foreach (var proyecto in XDocument.Load(rutaSlnx).Descendants("Project"))
        {
            var ruta = Path.Combine(carpeta, proyecto.Attribute("Path")!.Value.Replace('\\', '/'));
            raiz.Agregar(LeerProyecto(ruta));
        }

        return raiz;
    }

    public static Nodo LeerProyecto(string rutaCsproj)
    {
        var carpeta = Path.GetDirectoryName(Path.GetFullPath(rutaCsproj))!;
        var xml = XDocument.Load(rutaCsproj);
        var nombre = xml.Descendants("AssemblyName").FirstOrDefault()?.Value ?? Path.GetFileNameWithoutExtension(rutaCsproj);
        var esExe = xml.Descendants("OutputType").Any(o => o.Value.Equals("Exe", StringComparison.OrdinalIgnoreCase));
        var proyecto = new Nodo(Nivel.Proyecto, Path.GetFileName(rutaCsproj), $"→ {nombre}.{(esExe ? "exe" : "dll")}");

        var separador = Path.DirectorySeparatorChar;
        var archivos = Directory
            .EnumerateFiles(carpeta, "*.cs", SearchOption.AllDirectories)
            .Where(a => !a.Contains($"{separador}obj{separador}") && !a.Contains($"{separador}bin{separador}"))
            .Order(StringComparer.Ordinal);

        foreach (var archivo in archivos)
        {
            proyecto.Agregar(LeerCodigo(File.ReadAllText(archivo), Path.GetRelativePath(carpeta, archivo)));
        }

        return proyecto;
    }

    // ─────────────────────────────── 枝 → 茎 (donde se cruzan los dos árboles) ───────────────────────────────

    public static Nodo LeerCodigo(string codigo, string nombreArchivo = "Codigo.cs")
    {
        var unidad = (CompilationUnitSyntax)CSharpSyntaxTree.ParseText(codigo, Opciones).GetRoot();
        var archivo = new Nodo(Nivel.Archivo, nombreArchivo);

        foreach (var alias in unidad.Externs)
        {
            archivo.Agregar(new Nodo(Nivel.Namespace, $"extern alias {alias.Identifier}", "1º"));
        }

        foreach (var directiva in unidad.Usings)
        {
            archivo.Agregar(Using(directiva));
        }

        foreach (var lista in unidad.AttributeLists)
        {
            foreach (var atributo in lista.Attributes)
            {
                archivo.Agregar(new Nodo(Nivel.Namespace, $"[{lista.Target?.Identifier.Text}: {atributo.Name}]", "atributo de ensamblado"));
            }
        }

        var sueltas = unidad.Members.OfType<GlobalStatementSyntax>().ToList();
        if (sueltas.Count > 0)
        {
            var topLevel = archivo.Agregar(new Nodo(Nivel.Namespace, "top-level statements", "solo un archivo por proyecto") { Novedad = "C# 9" });
            foreach (var sentencia in sueltas)
            {
                topLevel.Agregar(Sentencia(sentencia.Statement, 0));
            }
        }

        AgregarDeNamespace(archivo, unidad.Members);
        return archivo;
    }

    private static Nodo Using(UsingDirectiveSyntax u)
    {
        var tipo = (u.GlobalKeyword.IsKind(SyntaxKind.None) ? "" : "global ")
                   + "using"
                   + (u.StaticKeyword.IsKind(SyntaxKind.None) ? "" : " static")
                   + (u.Alias is null ? "" : $" {u.Alias.Name} =");
        var nodo = new Nodo(Nivel.Namespace, $"{tipo} {u.NamespaceOrType}");
        if (!u.GlobalKeyword.IsKind(SyntaxKind.None))
        {
            nodo.Novedad = "C# 10";
            nodo.Detalle = "para todo el proyecto";
        }

        return nodo;
    }

    // ─────────────────────────────── 茎 → 芽 ───────────────────────────────

    private static void AgregarDeNamespace(Nodo padre, SyntaxList<MemberDeclarationSyntax> miembros)
    {
        foreach (var miembro in miembros)
        {
            switch (miembro)
            {
                case BaseNamespaceDeclarationSyntax ns:
                    var esFile = ns is FileScopedNamespaceDeclarationSyntax;
                    var nodo = padre.Agregar(new Nodo(Nivel.Namespace, $"namespace {ns.Name}", esFile ? "file-scoped" : "de bloque { }")
                    {
                        Novedad = esFile ? "C# 10" : null,
                    });
                    foreach (var u in ns.Usings)
                    {
                        nodo.Agregar(Using(u));
                    }

                    AgregarDeNamespace(nodo, ns.Members);
                    break;

                case BaseTypeDeclarationSyntax or DelegateDeclarationSyntax:
                    padre.Agregar(Tipo(miembro));
                    break;
            }
        }
    }

    // ─────────────────────────────── 芽 → 葉 ───────────────────────────────

    private static Nodo Tipo(MemberDeclarationSyntax declaracion)
    {
        var modificadores = string.Join(' ', declaracion.Modifiers.Select(m => m.Text));
        switch (declaracion)
        {
            case DelegateDeclarationSyntax d:
                return new Nodo(Nivel.Tipo, $"delegate {d.Identifier}", "firma de método · sin miembros");

            case EnumDeclarationSyntax e:
                var enumeracion = new Nodo(Nivel.Tipo, $"enum {e.Identifier}", Unir(modificadores, "valor"));
                foreach (var valor in e.Members)
                {
                    enumeracion.Agregar(new Nodo(Nivel.Miembro, $"constante {valor.Identifier}"));
                }

                return enumeracion;

            case TypeDeclarationSyntax t:
                var palabra = t switch
                {
                    RecordDeclarationSyntax r when r.ClassOrStructKeyword.IsKind(SyntaxKind.StructKeyword) => "record struct",
                    RecordDeclarationSyntax => "record",
                    _ => t.Keyword.Text,
                };
                var tipo = new Nodo(Nivel.Tipo, $"{palabra} {t.Identifier}{t.TypeParameterList}", modificadores.Length > 0 ? modificadores : null)
                {
                    Novedad = t switch
                    {
                        UnionDeclarationSyntax => "C# 15",
                        _ when t.Modifiers.Any(SyntaxKind.ClosedKeyword) => "C# 15",
                        _ when t.Modifiers.Any(SyntaxKind.FileKeyword) => "C# 11",
                        { ParameterList: not null } and (ClassDeclarationSyntax or StructDeclarationSyntax) => "C# 12",
                        RecordDeclarationSyntax r when r.ClassOrStructKeyword.IsKind(SyntaxKind.StructKeyword) => "C# 10",
                        RecordDeclarationSyntax => "C# 9",
                        _ => null,
                    },
                };
                if (t.ParameterList is { Parameters.Count: > 0 } primario)
                {
                    tipo.Agregar(new Nodo(Nivel.Miembro, $"constructor primario {primario}"));
                }

                foreach (var miembro in t.Members)
                {
                    tipo.Agregar(Miembro(miembro));
                }

                return tipo;

            default:
                return new Nodo(Nivel.Tipo, declaracion.Kind().ToString());
        }
    }

    private static Nodo Miembro(MemberDeclarationSyntax m)
    {
        var parcial = m.Modifiers.Any(SyntaxKind.PartialKeyword);
        switch (m)
        {
            // Un bloque extension es sintácticamente una declaración de tipo, pero vive como miembro.
            case ExtensionBlockDeclarationSyntax ext:
                var bloque = new Nodo(Nivel.Miembro, $"extension{ext.ParameterList}", "bloque de extensión") { Novedad = "C# 14" };
                foreach (var miembro in ext.Members)
                {
                    bloque.Agregar(Miembro(miembro));
                }

                return bloque;

            case BaseTypeDeclarationSyntax or DelegateDeclarationSyntax:
                var anidado = Tipo(m);
                anidado.Detalle = Unir(anidado.Detalle, "anidado ↺ vuelve a 芽");
                return anidado;

            case FieldDeclarationSyntax f:
                var esConst = f.Modifiers.Any(SyntaxKind.ConstKeyword);
                var campo = new Nodo(
                    Nivel.Miembro,
                    $"{(esConst ? "constante" : "campo")} {string.Join(", ", f.Declaration.Variables.Select(v => v.Identifier.Text))}",
                    f.Modifiers.Any(SyntaxKind.StaticKeyword) ? "static" : null);
                foreach (var v in f.Declaration.Variables.Where(v => v.Initializer is not null))
                {
                    campo.Agregar(Expresion(v.Initializer!.Value, 0));
                }

                return campo;

            case PropertyDeclarationSyntax p:
                var propiedad = new Nodo(Nivel.Miembro, $"propiedad {p.Identifier}", Modificadores(p, "required", "static"));
                if (p.DescendantNodes().OfType<FieldExpressionSyntax>().Any())
                {
                    propiedad.Novedad = "C# 14";
                    propiedad.Detalle = Unir(propiedad.Detalle, "usa field");
                }

                AgregarAccesores(propiedad, p.AccessorList, p.ExpressionBody);
                if (p.Initializer is not null)
                {
                    propiedad.Agregar(Expresion(p.Initializer.Value, 0));
                }

                return propiedad;

            case IndexerDeclarationSyntax i:
                var indexador = new Nodo(Nivel.Miembro, $"indexador this{i.ParameterList}");
                AgregarAccesores(indexador, i.AccessorList, i.ExpressionBody);
                return indexador;

            case ConstructorDeclarationSyntax c:
                return ConCuerpo(
                    new Nodo(Nivel.Miembro, $"constructor {c.Identifier}{c.ParameterList}", parcial ? "partial" : Modificadores(c, "static"))
                    {
                        Novedad = parcial ? "C# 14" : null,
                    },
                    c.Body,
                    c.ExpressionBody);

            case DestructorDeclarationSyntax d:
                return ConCuerpo(new Nodo(Nivel.Miembro, $"finalizador ~{d.Identifier}()"), d.Body, d.ExpressionBody);

            case MethodDeclarationSyntax metodo:
                return ConCuerpo(
                    new Nodo(Nivel.Miembro, $"método {metodo.Identifier}{metodo.TypeParameterList}()", Modificadores(metodo, "static", "abstract", "override", "virtual")),
                    metodo.Body,
                    metodo.ExpressionBody);

            case OperatorDeclarationSyntax o:
                var deInstancia = !o.Modifiers.Any(SyntaxKind.StaticKeyword);
                return ConCuerpo(
                    new Nodo(Nivel.Miembro, $"operador {o.OperatorToken.Text}", deInstancia ? "de instancia: asignación compuesta" : "static")
                    {
                        Novedad = deInstancia ? "C# 14" : null,
                    },
                    o.Body,
                    o.ExpressionBody);

            case ConversionOperatorDeclarationSyntax conv:
                return ConCuerpo(new Nodo(Nivel.Miembro, $"conversión {conv.ImplicitOrExplicitKeyword.Text} {conv.Type}"), conv.Body, conv.ExpressionBody);

            case EventFieldDeclarationSyntax ef:
                return new Nodo(Nivel.Miembro, $"evento {string.Join(", ", ef.Declaration.Variables.Select(v => v.Identifier.Text))}", parcial ? "partial" : null)
                {
                    Novedad = parcial ? "C# 14" : null,
                };

            case EventDeclarationSyntax e:
                var evento = new Nodo(Nivel.Miembro, $"evento {e.Identifier}", parcial ? "partial · add / remove" : "add / remove")
                {
                    Novedad = parcial ? "C# 14" : null,
                };
                AgregarAccesores(evento, e.AccessorList, null);
                return evento;

            default:
                return new Nodo(Nivel.Miembro, m.Kind().ToString());
        }
    }

    private static string? Modificadores(MemberDeclarationSyntax m, params ReadOnlySpan<string> interesantes)
    {
        var encontrados = new List<string>();
        foreach (var palabra in interesantes)
        {
            if (m.Modifiers.Any(t => t.Text == palabra))
            {
                encontrados.Add(palabra);
            }
        }

        return encontrados.Count == 0 ? null : string.Join(' ', encontrados);
    }

    private static void AgregarAccesores(Nodo nodo, AccessorListSyntax? accesores, ArrowExpressionClauseSyntax? flecha)
    {
        if (flecha is not null)
        {
            nodo.Agregar(Expresion(flecha.Expression, 0));
        }

        foreach (var accesor in accesores?.Accessors ?? [])
        {
            if (accesor.ExpressionBody is not null)
            {
                nodo.Agregar(Expresion(accesor.ExpressionBody.Expression, 0));
            }

            foreach (var sentencia in accesor.Body?.Statements ?? [])
            {
                nodo.Agregar(Sentencia(sentencia, 0));
            }
        }
    }

    private static Nodo ConCuerpo(Nodo nodo, BlockSyntax? cuerpo, ArrowExpressionClauseSyntax? flecha)
    {
        if (flecha is not null)
        {
            nodo.Agregar(Expresion(flecha.Expression, 0));
        }

        foreach (var sentencia in cuerpo?.Statements ?? [])
        {
            nodo.Agregar(Sentencia(sentencia, 0));
        }

        return nodo;
    }

    // ─────────────────────────────── 葉 → 花 → 種 ───────────────────────────────

    private static Nodo Sentencia(StatementSyntax s, int profundidad)
    {
        var (nombre, novedad) = s switch
        {
            LocalDeclarationStatementSyntax l when !l.UsingKeyword.IsKind(SyntaxKind.None) => ("using var", "C# 8"),
            LocalDeclarationStatementSyntax l => ($"declaración {string.Join(", ", l.Declaration.Variables.Select(v => v.Identifier.Text))}", null),
            ExpressionStatementSyntax => ("sentencia de expresión", null),
            IfStatementSyntax => ("if", null),
            SwitchStatementSyntax => ("switch", null),
            ForStatementSyntax => ("for", null),
            ForEachStatementSyntax => ("foreach", null),
            WhileStatementSyntax => ("while", null),
            DoStatementSyntax => ("do", null),
            TryStatementSyntax => ("try", null),
            ReturnStatementSyntax => ("return", null),
            ThrowStatementSyntax => ("throw", null),
            YieldStatementSyntax => ("yield", null),
            UsingStatementSyntax => ("using", null),
            LockStatementSyntax => ("lock", null),
            BreakStatementSyntax => ("break", null),
            ContinueStatementSyntax => ("continue", null),
            LocalFunctionStatementSyntax f => ($"función local {f.Identifier}", f.Modifiers.Any(SyntaxKind.StaticKeyword) ? "C# 8" : "C# 7"),
            BlockSyntax => ("bloque { }", null),
            _ => (s.Kind().ToString(), (string?)null),
        };

        var nodo = new Nodo(Nivel.Sentencia, nombre, Resumen(s)) { Novedad = novedad };
        foreach (var hijo in Hijos(s))
        {
            nodo.Agregar(hijo switch
            {
                StatementSyntax sentencia => Sentencia(sentencia, profundidad + 1),
                ExpressionSyntax expresion => Expresion(expresion, 0),
                _ => throw new UnreachableException(),
            });
        }

        return nodo;
    }

    private static Nodo Expresion(ExpressionSyntax e, int profundidad)
    {
        var nombre = e switch
        {
            InvocationExpressionSyntax { Expression: IdentifierNameSyntax { Identifier.Text: "nameof" } } => "nameof",
            InvocationExpressionSyntax => "llamada",
            AssignmentExpressionSyntax => "asignación",
            BinaryExpressionSyntax or PrefixUnaryExpressionSyntax or PostfixUnaryExpressionSyntax => "operador",
            LiteralExpressionSyntax => "literal",
            InterpolatedStringExpressionSyntax => "cadena $",
            IdentifierNameSyntax or GenericNameSyntax => "nombre",
            MemberAccessExpressionSyntax => "acceso .",
            ConditionalAccessExpressionSyntax => "acceso ?.",
            ElementAccessExpressionSyntax => "índice [ ]",
            BaseObjectCreationExpressionSyntax => "new",
            CollectionExpressionSyntax => "colección [ ]",
            LambdaExpressionSyntax => "lambda",
            SwitchExpressionSyntax => "switch",
            IsPatternExpressionSyntax => "patrón is",
            ConditionalExpressionSyntax => "condicional ?:",
            ThrowExpressionSyntax => "throw",
            CastExpressionSyntax => "conversión",
            ParenthesizedExpressionSyntax => "( )",
            _ => "expresión",
        };

        var nodo = new Nodo(Nivel.Expresion, nombre, Resumen(e)) { Novedad = Novedad(e) };

        // Una lambda con cuerpo de bloque hace que el árbol vuelva a florecer: 種 → 花.
        if (e is LambdaExpressionSyntax { Block: { } cuerpo })
        {
            foreach (var sentencia in cuerpo.Statements)
            {
                nodo.Agregar(Sentencia(sentencia, 0));
            }

            return nodo;
        }

        // a.b se lee como una sola semilla: no se sigue partiendo.
        if (profundidad < MaxProfundidadExpresion && e is not MemberAccessExpressionSyntax)
        {
            foreach (var hija in Hijos(e).OfType<ExpressionSyntax>())
            {
                nodo.Agregar(Expresion(hija, profundidad + 1));
            }
        }

        return nodo;
    }

    private static string? Novedad(ExpressionSyntax e) => e switch
    {
        AssignmentExpressionSyntax { Left: ConditionalAccessExpressionSyntax } => "C# 14",
        InvocationExpressionSyntax { Expression: IdentifierNameSyntax { Identifier.Text: "nameof" } } inv
            when inv.DescendantNodes().OfType<OmittedTypeArgumentSyntax>().Any() => "C# 14",
        CollectionExpressionSyntax c when c.Elements.Any(el => el is WithElementSyntax) => "C# 15",
        ParenthesizedLambdaExpressionSyntax l
            when l.ParameterList.Parameters.Any(p => p.Type is null && p.Modifiers.Count > 0) => "C# 14",
        CollectionExpressionSyntax => "C# 12",
        IsPatternExpressionSyntax { Pattern: ListPatternSyntax } => "C# 11",
        _ when e.DescendantNodesAndSelf().OfType<ListPatternSyntax>().Any() && e is SwitchExpressionSyntax => "C# 11",
        InterpolatedStringExpressionSyntax s when s.StringStartToken.Text.Contains("\"\"\"", StringComparison.Ordinal) => "C# 11",
        _ => null,
    };

    /// <summary>
    /// Los hijos "interesantes" de un nodo de sintaxis: sentencias y expresiones.
    /// Las piezas intermedias (cláusulas, declaraciones, bloques) se atraviesan sin dibujarse.
    /// </summary>
    private static IEnumerable<SyntaxNode> Hijos(SyntaxNode nodo)
    {
        foreach (var hijo in nodo.ChildNodes())
        {
            switch (hijo)
            {
                case BlockSyntax bloque:
                    foreach (var s in bloque.Statements)
                    {
                        yield return s;
                    }

                    break;
                case StatementSyntax or ExpressionSyntax:
                    // Un tipo (List<int>) o un patrón no son expresiones que se evalúen: se omiten.
                    if (hijo is TypeSyntax and not IdentifierNameSyntax and not GenericNameSyntax)
                    {
                        break;
                    }

                    if (hijo is IdentifierNameSyntax or GenericNameSyntax && nodo is not (ExpressionSyntax or ExpressionStatementSyntax or ReturnStatementSyntax))
                    {
                        break;
                    }

                    yield return hijo;
                    break;
                case ParameterListSyntax or ParameterSyntax or TypeArgumentListSyntax or AttributeListSyntax:
                    break;
                default:
                    foreach (var nieto in Hijos(hijo))
                    {
                        yield return nieto;
                    }

                    break;
            }
        }
    }

    private static string Resumen(SyntaxNode nodo)
    {
        var texto = string.Join(' ', nodo.ToString().Split((char[])['\r', '\n', '\t', ' '], StringSplitOptions.RemoveEmptyEntries));
        return texto.Length <= 46 ? texto : string.Concat(texto.AsSpan(0, 45), "…");
    }

    private static string? Unir(string? a, string? b) =>
        string.IsNullOrEmpty(a) ? b : string.IsNullOrEmpty(b) ? a : $"{a} · {b}";
}
