using GestorAssinaturas.Dominio.Clientes;

namespace GestorAssinaturas.Dominio.Repositorios;

public interface IRepositorioCliente
{
    Task<Cliente?> ObterPorIdentificadorAsync(Guid identificador, CancellationToken tokenDeCancelamento = default);

    Task AdicionarAsync(Cliente cliente, CancellationToken tokenDeCancelamento = default);
}
