using GestorAssinaturas.Dominio.Repositorios;

namespace GestorAssinaturas.Infraestrutura.Persistencia;

public sealed class UnidadeDeTrabalho : IUnidadeDeTrabalho
{
    private readonly ContextoDeDados _contextoDeDados;

    public UnidadeDeTrabalho(
        ContextoDeDados contextoDeDados,
        IRepositorioPlano planos,
        IRepositorioCliente clientes,
        IRepositorioAssinatura assinaturas,
        IRepositorioFatura faturas)
    {
        ArgumentNullException.ThrowIfNull(contextoDeDados);
        ArgumentNullException.ThrowIfNull(planos);
        ArgumentNullException.ThrowIfNull(clientes);
        ArgumentNullException.ThrowIfNull(assinaturas);
        ArgumentNullException.ThrowIfNull(faturas);

        _contextoDeDados = contextoDeDados;
        Planos = planos;
        Clientes = clientes;
        Assinaturas = assinaturas;
        Faturas = faturas;
    }

    public IRepositorioPlano Planos { get; }

    public IRepositorioCliente Clientes { get; }

    public IRepositorioAssinatura Assinaturas { get; }

    public IRepositorioFatura Faturas { get; }

    public async Task SalvarAlteracoesAsync(CancellationToken tokenDeCancelamento = default)
    {
        await _contextoDeDados.SaveChangesAsync(tokenDeCancelamento);
    }
}
