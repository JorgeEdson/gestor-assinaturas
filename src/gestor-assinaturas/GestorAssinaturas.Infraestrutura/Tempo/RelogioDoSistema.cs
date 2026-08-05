using GestorAssinaturas.Aplicacao.Portas;

namespace GestorAssinaturas.Infraestrutura.Tempo;

public sealed class RelogioDoSistema : IRelogioDoSistema
{
    public DateOnly ObterDataAtual()
    {
        return DateOnly.FromDateTime(DateTime.UtcNow);
    }

    public DateTimeOffset ObterInstanteAtual()
    {
        return DateTimeOffset.UtcNow;
    }
}
