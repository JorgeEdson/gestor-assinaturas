using GestorAssinaturas.Dominio.Clientes;
using GestorAssinaturas.Dominio.Repositorios;

namespace GestorAssinaturas.Aplicacao.Testes.Dobras;

public sealed class RepositorioClienteEmMemoria : IRepositorioCliente
{
    private readonly Dictionary<Guid, Cliente> _clientes = new();

    public IReadOnlyCollection<Cliente> Clientes => _clientes.Values.ToList();

    public Task<Cliente?> ObterPorIdentificadorAsync(
        Guid identificador,
        CancellationToken tokenDeCancelamento = default)
    {
        _clientes.TryGetValue(identificador, out var cliente);

        return Task.FromResult(cliente);
    }

    public Task AdicionarAsync(Cliente cliente, CancellationToken tokenDeCancelamento = default)
    {
        _clientes[cliente.Identificador] = cliente;

        return Task.CompletedTask;
    }
}
