using GestorAssinaturas.Aplicacao.Portas.Pagamentos;
using GestorAssinaturas.Dominio.Comum;

namespace GestorAssinaturas.Aplicacao.Testes.Dobras;

public sealed class GatewayPagamentoFalso : IGatewayPagamento
{
    private readonly Resultado<RetornoDaCobranca> _retornoConfigurado;

    private GatewayPagamentoFalso(Resultado<RetornoDaCobranca> retornoConfigurado)
    {
        _retornoConfigurado = retornoConfigurado;
    }

    public RequisicaoDeCobranca? UltimaRequisicao { get; private set; }

    public bool FoiAcionado => UltimaRequisicao is not null;

    public static GatewayPagamentoFalso QueAprova()
    {
        return new GatewayPagamentoFalso(
            Resultado<RetornoDaCobranca>.Sucesso(RetornoDaCobranca.Aprovado("TRANSACAO-APROVADA")));
    }

    public static GatewayPagamentoFalso QueRecusa()
    {
        return new GatewayPagamentoFalso(
            Resultado<RetornoDaCobranca>.Sucesso(RetornoDaCobranca.Recusado("Saldo insuficiente.")));
    }

    public static GatewayPagamentoFalso QueFalha()
    {
        return new GatewayPagamentoFalso(
            Resultado<RetornoDaCobranca>.Falha("Gateway de pagamento indisponível."));
    }

    public Task<Resultado<RetornoDaCobranca>> ProcessarCobrancaAsync(
        RequisicaoDeCobranca requisicaoDeCobranca,
        CancellationToken cancellationToken = default)
    {
        UltimaRequisicao = requisicaoDeCobranca;

        return Task.FromResult(_retornoConfigurado);
    }
}
