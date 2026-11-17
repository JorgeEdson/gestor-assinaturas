namespace GestorAssinaturas.Dominio.Repositorios;

public interface IUnidadeDeTrabalho
{
    IRepositorioPlano Planos { get; }

    IRepositorioCliente Clientes { get; }

    IRepositorioAssinatura Assinaturas { get; }

    IRepositorioFatura Faturas { get; }

    Task SalvarAlteracoesAsync(CancellationToken tokenDeCancelamento = default);
}
