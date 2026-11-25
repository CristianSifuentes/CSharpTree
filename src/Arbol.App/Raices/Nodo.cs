namespace Arbol.App.Raices;

/// <summary>Un nodo del árbol: qué es (<see cref="Nivel"/>), cómo se llama y qué contiene.</summary>
public sealed class Nodo(Nivel nivel, string etiqueta, string? detalle = null)
{
    private readonly List<Nodo> _hijos = [];

    public Nivel Nivel { get; } = nivel;
    public string Etiqueta { get; } = etiqueta;
    public string? Detalle { get; set; } = detalle;

    /// <summary>La versión de C# que hizo posible este nodo, si es una novedad reciente.</summary>
    public string? Novedad { get; set; }

    public Nodo? Padre { get; private set; }
    public IReadOnlyList<Nodo> Hijos => _hijos;

    /// <summary>Los hermanos: los otros hijos del mismo padre. Mismo nivel, ninguno dentro del otro.</summary>
    public IEnumerable<Nodo> Hermanos => Padre?._hijos.Where(h => !ReferenceEquals(h, this)) ?? [];

    public Nodo Agregar(Nodo hijo)
    {
        hijo.Padre = this;
        _hijos.Add(hijo);
        return hijo;
    }

    public IEnumerable<Nodo> Ruta()
    {
        var pila = new Stack<Nodo>();
        for (var actual = this; actual is not null; actual = actual.Padre)
        {
            pila.Push(actual);
        }

        return pila;
    }

    public IEnumerable<Nodo> Descendientes()
    {
        foreach (var hijo in _hijos)
        {
            yield return hijo;
            foreach (var nieto in hijo.Descendientes())
            {
                yield return nieto;
            }
        }
    }

    /// <summary>Una copia del árbol que se detiene en <paramref name="hasta"/>.</summary>
    public Nodo Podar(Nivel hasta)
    {
        var copia = new Nodo(Nivel, Etiqueta, Detalle) { Novedad = Novedad };
        foreach (var hijo in _hijos.Where(h => h.Nivel <= hasta))
        {
            copia.Agregar(hijo.Podar(hasta));
        }

        return copia;
    }

    public override string ToString() => $"{Nivel.Kanji} {Etiqueta}";
}
