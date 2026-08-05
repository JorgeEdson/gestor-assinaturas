using GestorAssinaturas.Aplicacao.Portas.Pagamentos;
using GestorAssinaturas.Dominio.Comum;
using Microsoft.Extensions.Logging;

namespace GestorAssinaturas.Infraestrutura.Pagamentos;

public sealed class GatewayPagamentoSimulado : IGatewayPagamento
{
    public const decimal CentavosQueSimulamRecusa = 0.99m;

    private readonly ILogger<GatewayPagamentoSimulado> _logger;

    public GatewayPagamentoSimulado(ILogger<GatewayPagamentoSimulado> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        _logger = logger;
    }

    public Task<Resultado<RetornoDaCobranca>> ProcessarCobrancaAsync(
        RequisicaoDeCobranca requisicaoDeCobranca,
        CancellationToken cancellationToken = default)
    {
        if (requisicaoDeCobranca is null)
        {
            return Task.FromResult(
                Resultado<RetornoDaCobranca>.Falha("A requisição de cobrança é obrigatória."));
        }

        var centavosDoValor = requisicaoDeCobranca.Valor.Valor % 1m;

        if (centavosDoValor == CentavosQueSimulamRecusa)
        {
            _logger.LogInformation(
                "Gateway simulado recusou a cobrança da fatura {IdentificadorDaFatura}.",
                requisicaoDeCobranca.IdentificadorDaFatura);

            return Task.FromResult(
                Resultado<RetornoDaCobranca>.Sucesso(
                    RetornoDaCobranca.Recusado("Cobrança recusada pelo emissor do cartão.")));
        }

        var identificadorDaTransacao = $"SIM-{Guid.NewGuid():N}";

        _logger.LogInformation(
            "Gateway simulado aprovou a cobrança da fatura {IdentificadorDaFatura} com a transação {IdentificadorDaTransacao}.",
            requisicaoDeCobranca.IdentificadorDaFatura,
            identificadorDaTransacao);

        return Task.FromResult(
            Resultado<RetornoDaCobranca>.Sucesso(
                RetornoDaCobranca.Aprovado(identificadorDaTransacao)));
    }
}
