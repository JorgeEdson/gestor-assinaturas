using GestorAssinaturas.Dominio.Comum;
using GestorAssinaturas.Dominio.Planos;

namespace GestorAssinaturas.Aplicacao.Portas.Persistencia;

public interface IRepositorioPlano
{
    Task<Resultado<Plano>> ObterPorIdentificadorAsync(Guid identificador, CancellationToken cancellationToken = default);

    Task AdicionarAsync(Plano plano, CancellationToken cancellationToken = default);
}
