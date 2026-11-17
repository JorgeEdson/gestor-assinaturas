using GestorAssinaturas.Dominio.Planos;

namespace GestorAssinaturas.Dominio.Repositorios;

public interface IRepositorioPlano
{
    Task<Plano?> ObterPorIdentificadorAsync(Guid identificador, CancellationToken tokenDeCancelamento = default);

    Task AdicionarAsync(Plano plano, CancellationToken tokenDeCancelamento = default);
}
