using GestorAssinaturas.Dominio.Comum;

namespace GestorAssinaturas.Aplicacao.Portas.Persistencia;

public interface IUnitOfWork
{
    IRepositorioPlano Planos { get; }

    IRepositorioCliente Clientes { get; }

    IRepositorioAssinatura Assinaturas { get; }

    IRepositorioFatura Faturas { get; }

    Task<Resultado<bool>> IniciarTransacaoAsync(CancellationToken cancellationToken = default);

    Task<Resultado<bool>> ConfirmarTransacaoAsync(CancellationToken cancellationToken = default);

    Task<Resultado<bool>> DesfazerTransacaoAsync(CancellationToken cancellationToken = default);

    Task<Resultado<int>> SalvarAlteracoesAsync(CancellationToken cancellationToken = default);
}
