// 枝 Otro archivo, el mismo tipo: `partial` reparte un tipo entre archivos, nunca entre proyectos.
namespace Tienda.Dominio.Pedidos;

public sealed partial class Pedido
{
    private Action<Pedido>? _cambio;

    // 葉 La implementación del constructor partial declarado en Pedido.cs.
    public partial Pedido(string cliente)
    {
        Cliente = cliente;
        Interlocked.Increment(ref s_creados);
    }

    // 葉 La implementación del evento partial: con accessors add / remove.
    public partial event Action<Pedido>? Cambio
    {
        add => _cambio += value;
        remove => _cambio -= value;
    }
}
