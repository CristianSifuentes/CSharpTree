// 枝 Brotes de C# 15 (.NET 11): un tipo nuevo (union) y una jerarquía cerrada (closed).
namespace Tienda.Dominio.Pedidos;

public sealed record Aprobado(string Autorizacion, decimal Monto);
public sealed record Rechazado(string Motivo);
public sealed record Pendiente(DateTimeOffset Desde);

// 芽 union (C# 15): un séptimo hermano junto a class, struct, record, interface, enum y delegate.
//    Su valor es exactamente uno de los tipos de la lista. El compilador lo convierte en un struct.
public union ResultadoPago(Aprobado, Rechazado, Pendiente)
{
    // 葉 Una union puede tener miembros propios: métodos y propiedades calculadas (no campos de instancia).
    public bool EsFinal => Value is Aprobado or Rechazado;

    public string Describir() => this switch          // exhaustivo: no hace falta `_ =>`
    {
        Aprobado a => $"aprobado {a.Autorizacion} por {a.Monto:0.00}",
        Rechazado r => $"rechazado: {r.Motivo}",
        Pendiente p => $"pendiente desde {p.Desde:HH:mm}",
    };
}

// 系 closed (C# 15): solo este ensamblado puede heredar de EstadoEnvio. La lista de hijos queda fija.
public closed record EstadoEnvio
{
    public static EstadoEnvio Inicial => new Preparando();
}

public sealed record Preparando : EstadoEnvio;
public sealed record EnCamino(string Guia) : EstadoEnvio;
public sealed record Entregado(DateOnly Dia) : EstadoEnvio;

public static class Envios
{
    // Exhaustivo sobre una jerarquía closed: el compilador conoce a todos los hijos.
    public static string Etiqueta(EstadoEnvio estado) => estado switch
    {
        Preparando => "preparando",
        EnCamino(var guia) => $"en camino ({guia})",
        Entregado(var dia) => $"entregado el {dia:dd/MM}",
    };
}
