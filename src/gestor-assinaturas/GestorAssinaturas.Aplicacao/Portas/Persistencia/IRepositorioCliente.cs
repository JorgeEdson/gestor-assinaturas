using GestorAssinaturas.Dominio.Clientes;
using GestorAssinaturas.Dominio.Comum;

namespace GestorAssinaturas.Aplicacao.Portas.Persistencia;

public interface IRepositorioCliente
{
    Task<Resultado<Cliente>> ObterPorIdentificadorAsync(Guid identificador, CancellationToken cancellationToken = default);

    Task AdicionarAsync(Cliente cliente, CancellationToken cancellationToken = default);
}
