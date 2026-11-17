using GestorAssinaturas.Dominio.Assinaturas;

namespace GestorAssinaturas.Dominio.Repositorios;

public interface IRepositorioAssinatura
{
    Task<Assinatura?> ObterPorIdentificadorAsync(Guid identificador, CancellationToken tokenDeCancelamento = default);

    Task AdicionarAsync(Assinatura assinatura, CancellationToken tokenDeCancelamento = default);
}
