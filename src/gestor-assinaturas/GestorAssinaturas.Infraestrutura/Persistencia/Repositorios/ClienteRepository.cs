using GestorAssinaturas.Aplicacao.Portas.Persistencia;
using GestorAssinaturas.Dominio.Clientes;
using GestorAssinaturas.Dominio.Comum;
using Microsoft.EntityFrameworkCore;

namespace GestorAssinaturas.Infraestrutura.Persistencia.Repositorios;

public sealed class ClienteRepository : IRepositorioCliente
{
    private readonly GestorAssinaturasDbContext _contextoDeDados;

    public ClienteRepository(GestorAssinaturasDbContext contextoDeDados)
    {
        ArgumentNullException.ThrowIfNull(contextoDeDados);

        _contextoDeDados = contextoDeDados;
    }

    public async Task<Resultado<Cliente>> ObterPorIdentificadorAsync(
        Guid identificador,
        CancellationToken cancellationToken = default)
    {
        return await Resultado<Cliente>.TentarAsync(async () =>
        {
            var cliente = await _contextoDeDados.Clientes
                .FirstOrDefaultAsync(clienteArmazenado => clienteArmazenado.Identificador == identificador, cancellationToken);

            return Resultado<Cliente>.Sucesso(cliente);
        });
    }

    public async Task AdicionarAsync(Cliente cliente, CancellationToken cancellationToken = default)
    {
        await _contextoDeDados.Clientes.AddAsync(cliente, cancellationToken);
    }
}
