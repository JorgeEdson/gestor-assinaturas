using System.Collections.Concurrent;
using GestorAssinaturas.Aplicacao.Portas.Pagamentos;
using GestorAssinaturas.Dominio.Comum;
using Microsoft.Extensions.Logging;

namespace GestorAssinaturas.Infraestrutura.Pagamentos;

public sealed class GatewayPagamentoSimulado : IGatewayPagamento
{
    public const decimal CentavosQueSimulamCartaoRecusado = 0.99m;

    private readonly ConcurrentDictionary<Guid, bool> _assinaturasComCartaoJaRecusado = new();
    private readonly ILogger<GatewayPagamentoSimulado> _registradorDeLog;

    public GatewayPagamentoSimulado(ILogger<GatewayPagamentoSimulado> registradorDeLog)
    {
        ArgumentNullException.ThrowIfNull(registradorDeLog);

        _registradorDeLog = registradorDeLog;
    }

    public Task<Resultado<RetornoDaCobranca>> ProcessarCobrancaAsync(
        RequisicaoDeCobranca requisicaoDeCobranca,
        CancellationToken tokenDeCancelamento = default)
    {
        if (requisicaoDeCobranca is null)
        {
            return Task.FromResult(
                Resultado<RetornoDaCobranca>.Falha("A requisição de cobrança é obrigatória."));
        }

        if (DeveSimularCartaoRecusado(requisicaoDeCobranca))
        {
            _registradorDeLog.LogInformation(
                "Gateway simulado recusou a cobrança da fatura {IdentificadorDaFatura}.",
                requisicaoDeCobranca.IdentificadorDaFatura);

            return Task.FromResult(
                Resultado<RetornoDaCobranca>.Sucesso(
                    RetornoDaCobranca.Recusado("Cobrança recusada pelo emissor do cartão.")));
        }

        var identificadorDaTransacao = $"SIM-{Guid.NewGuid():N}";

        _registradorDeLog.LogInformation(
            "Gateway simulado aprovou a cobrança da fatura {IdentificadorDaFatura} com a transação {IdentificadorDaTransacao}.",
            requisicaoDeCobranca.IdentificadorDaFatura,
            identificadorDaTransacao);

        return Task.FromResult(
            Resultado<RetornoDaCobranca>.Sucesso(
                RetornoDaCobranca.Aprovado(identificadorDaTransacao)));
    }

    private bool DeveSimularCartaoRecusado(RequisicaoDeCobranca requisicaoDeCobranca)
    {
        var centavosDoValor = requisicaoDeCobranca.Valor.Valor % 1m;

        if (centavosDoValor != CentavosQueSimulamCartaoRecusado)
        {
            return false;
        }

        return _assinaturasComCartaoJaRecusado.TryAdd(requisicaoDeCobranca.IdentificadorDaAssinatura, true);
    }
}
