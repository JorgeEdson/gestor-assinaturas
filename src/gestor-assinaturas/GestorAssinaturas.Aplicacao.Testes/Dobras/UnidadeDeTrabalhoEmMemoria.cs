using GestorAssinaturas.Dominio.Repositorios;

namespace GestorAssinaturas.Aplicacao.Testes.Dobras;

public sealed class UnidadeDeTrabalhoEmMemoria : IUnidadeDeTrabalho
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

    public Task SalvarAlteracoesAsync(CancellationToken tokenDeCancelamento = default)
    {
        QuantidadeDeSalvamentos++;

        return Task.CompletedTask;
    }
}
