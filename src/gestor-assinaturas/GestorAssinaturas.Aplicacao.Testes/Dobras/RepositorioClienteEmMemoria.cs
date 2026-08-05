using GestorAssinaturas.Aplicacao.Portas.Persistencia;
using GestorAssinaturas.Dominio.Clientes;
using GestorAssinaturas.Dominio.Comum;

namespace GestorAssinaturas.Aplicacao.Testes.Dobras;

public sealed class RepositorioClienteEmMemoria : IRepositorioCliente
{
    private readonly Dictionary<Guid, Cliente> _clientes = new();

    public IReadOnlyCollection<Cliente> Clientes => _clientes.Values.ToList();

    public Task<Resultado<Cliente>> ObterPorIdentificadorAsync(
        Guid identificador,
        CancellationToken cancellationToken = default)
    {
        return _clientes.TryGetValue(identificador, out var cliente)
            ? Task.FromResult(Resultado<Cliente>.Sucesso(cliente))
            : Task.FromResult(Resultado<Cliente>.Sucesso());
    }

    public Task AdicionarAsync(Cliente cliente, CancellationToken cancellationToken = default)
    {
        _clientes[cliente.Identificador] = cliente;

        return Task.CompletedTask;
    }
}
