using GestorAssinaturas.Aplicacao.Portas.Persistencia;
using GestorAssinaturas.Dominio.Comum;

namespace GestorAssinaturas.Aplicacao.Testes.Dobras;

public sealed class UnitOfWorkEmMemoria : IUnitOfWork
{
    public RepositorioPlanoEmMemoria PlanoEmMemoria { get; } = new();

    public RepositorioClienteEmMemoria ClienteEmMemoria { get; } = new();

    public RepositorioAssinaturaEmMemoria AssinaturaEmMemoria { get; } = new();

    public RepositorioFaturaEmMemoria FaturaEmMemoria { get; } = new();

    public int QuantidadeDeSalvamentos { get; private set; }

    public IRepositorioPlano Planos => PlanoEmMemoria;

    public IRepositorioCliente Clientes => ClienteEmMemoria;

    public IRepositorioAssinatura Assinaturas => AssinaturaEmMemoria;

    public IRepositorioFatura Faturas => FaturaEmMemoria;

    public Task<Resultado<int>> SalvarAlteracoesAsync(CancellationToken cancellationToken = default)
    {
        QuantidadeDeSalvamentos++;

        return Task.FromResult(Resultado<int>.Sucesso(1));
    }

    public Task<Resultado<bool>> IniciarTransacaoAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Resultado<bool>.Sucesso(true));
    }

    public Task<Resultado<bool>> ConfirmarTransacaoAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Resultado<bool>.Sucesso(true));
    }

    public Task<Resultado<bool>> DesfazerTransacaoAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Resultado<bool>.Sucesso(true));
    }
}
