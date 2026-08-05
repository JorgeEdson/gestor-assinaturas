using GestorAssinaturas.Dominio.Comum;
using GestorAssinaturas.Dominio.Faturas;

namespace GestorAssinaturas.Aplicacao.Portas.Persistencia;

public interface IRepositorioFatura
{
    Task<Resultado<Fatura>> ObterPorIdentificadorAsync(Guid identificador, CancellationToken cancellationToken = default);

    Task<Resultado<IReadOnlyCollection<Fatura>>> ListarPorAssinaturaAsync(
        Guid identificadorDaAssinatura,
        CancellationToken cancellationToken = default);

    Task AdicionarAsync(Fatura fatura, CancellationToken cancellationToken = default);
}
