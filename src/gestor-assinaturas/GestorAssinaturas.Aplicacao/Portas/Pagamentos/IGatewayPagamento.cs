using GestorAssinaturas.Dominio.Comum;

namespace GestorAssinaturas.Aplicacao.Portas.Pagamentos;

public interface IGatewayPagamento
{
    Task<Resultado<RetornoDaCobranca>> ProcessarCobrancaAsync(
        RequisicaoDeCobranca requisicaoDeCobranca,
        CancellationToken cancellationToken = default);
}
