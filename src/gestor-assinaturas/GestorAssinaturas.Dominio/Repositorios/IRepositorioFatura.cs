using GestorAssinaturas.Dominio.Faturas;

namespace GestorAssinaturas.Dominio.Repositorios;

public interface IRepositorioFatura
{
    Task<Fatura?> ObterPorIdentificadorAsync(Guid identificador, CancellationToken tokenDeCancelamento = default);

    Task<IReadOnlyCollection<Fatura>> ListarPorAssinaturaAsync(
        Guid identificadorDaAssinatura,
        CancellationToken tokenDeCancelamento = default);

    Task AdicionarAsync(Fatura fatura, CancellationToken tokenDeCancelamento = default);
}
