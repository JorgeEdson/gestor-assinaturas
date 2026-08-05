using GestorAssinaturas.Dominio.Assinaturas;
using GestorAssinaturas.Dominio.Comum;

namespace GestorAssinaturas.Aplicacao.Portas.Persistencia;

public interface IRepositorioAssinatura
{
    Task<Resultado<Assinatura>> ObterPorIdentificadorAsync(Guid identificador, CancellationToken cancellationToken = default);

    Task AdicionarAsync(Assinatura assinatura, CancellationToken cancellationToken = default);
}
