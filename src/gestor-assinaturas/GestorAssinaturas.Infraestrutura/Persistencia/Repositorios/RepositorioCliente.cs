using GestorAssinaturas.Dominio.Clientes;
using GestorAssinaturas.Dominio.Repositorios;
using Microsoft.EntityFrameworkCore;

namespace GestorAssinaturas.Infraestrutura.Persistencia.Repositorios;

public sealed class RepositorioCliente : IRepositorioCliente
{
    private readonly ContextoDeDados _contextoDeDados;

    public RepositorioCliente(ContextoDeDados contextoDeDados)
    {
        ArgumentNullException.ThrowIfNull(contextoDeDados);

        _contextoDeDados = contextoDeDados;
    }

    public async Task<Cliente?> ObterPorIdentificadorAsync(
        Guid identificador,
        CancellationToken tokenDeCancelamento = default)
    {
        return await _contextoDeDados.Clientes
            .FirstOrDefaultAsync(clienteArmazenado => clienteArmazenado.Identificador == identificador, tokenDeCancelamento);
    }

    public async Task AdicionarAsync(Cliente cliente, CancellationToken tokenDeCancelamento = default)
    {
        await _contextoDeDados.Clientes.AddAsync(cliente, tokenDeCancelamento);
    }
}
