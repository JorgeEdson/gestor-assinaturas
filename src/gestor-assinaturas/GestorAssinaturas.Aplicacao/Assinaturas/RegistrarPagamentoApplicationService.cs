using GestorAssinaturas.Aplicacao.Portas.Pagamentos;
using GestorAssinaturas.Aplicacao.Portas.Persistencia;
using GestorAssinaturas.Dominio.Assinaturas;
using GestorAssinaturas.Dominio.Comum;
using Microsoft.Extensions.Logging;

namespace GestorAssinaturas.Aplicacao.Assinaturas;

public sealed record RegistrarPagamentoEntrada(
    Guid IdentificadorDaFatura);

public sealed class RegistrarPagamentoApplicationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IGatewayPagamento _gatewayPagamento;
    private readonly ServicoReativacao _servicoReativacao;
    private readonly ServicoInadimplencia _servicoInadimplencia;
    private readonly ILogger<RegistrarPagamentoApplicationService> _logger;

    public RegistrarPagamentoApplicationService(
        IUnitOfWork unitOfWork,
        IGatewayPagamento gatewayPagamento,
        ServicoReativacao servicoReativacao,
        ServicoInadimplencia servicoInadimplencia,
        ILogger<RegistrarPagamentoApplicationService> logger)
    {
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(gatewayPagamento);
        ArgumentNullException.ThrowIfNull(servicoReativacao);
        ArgumentNullException.ThrowIfNull(servicoInadimplencia);
        ArgumentNullException.ThrowIfNull(logger);

        _unitOfWork = unitOfWork;
        _gatewayPagamento = gatewayPagamento;
        _servicoReativacao = servicoReativacao;
        _servicoInadimplencia = servicoInadimplencia;
        _logger = logger;
    }

    public async Task<Resultado<SituacaoDoPagamento>> ExecutarAsync(
        RegistrarPagamentoEntrada comando,
        CancellationToken cancellationToken = default)
    {
        if (comando is null)
        {
            return Resultado<SituacaoDoPagamento>.Falha("O comando de registro de pagamento é obrigatório.");
        }

        _logger.LogInformation(
            "Iniciando registro de pagamento da fatura {IdentificadorDaFatura}.",
            comando.IdentificadorDaFatura);

        var resultadoDaFatura = await _unitOfWork.Faturas.ObterPorIdentificadorAsync(
            comando.IdentificadorDaFatura,
            cancellationToken);

        if (resultadoDaFatura.EhFalha)
        {
            return Resultado<SituacaoDoPagamento>.Falha(resultadoDaFatura.Erros!);
        }

        if (resultadoDaFatura.Instancia is null)
        {
            _logger.LogWarning(
                "Registro de pagamento rejeitado: fatura {IdentificadorDaFatura} não encontrada.",
                comando.IdentificadorDaFatura);

            return Resultado<SituacaoDoPagamento>.Falha("Fatura não encontrada.");
        }

        var fatura = resultadoDaFatura.Instancia;

        if (!fatura.EstaAberta())
        {
            _logger.LogWarning(
                "Registro de pagamento rejeitado: fatura {IdentificadorDaFatura} não está em aberto.",
                comando.IdentificadorDaFatura);

            return Resultado<SituacaoDoPagamento>.Falha("Somente uma fatura em aberto pode ser paga.");
        }

        var resultadoDaAssinatura = await _unitOfWork.Assinaturas.ObterPorIdentificadorAsync(
            fatura.IdentificadorDaAssinatura,
            cancellationToken);

        if (resultadoDaAssinatura.EhFalha)
        {
            return Resultado<SituacaoDoPagamento>.Falha(resultadoDaAssinatura.Erros!);
        }

        if (resultadoDaAssinatura.Instancia is null)
        {
            _logger.LogWarning(
                "Registro de pagamento rejeitado: assinatura {IdentificadorDaAssinatura} não encontrada.",
                fatura.IdentificadorDaAssinatura);

            return Resultado<SituacaoDoPagamento>.Falha("Assinatura não encontrada.");
        }

        var assinatura = resultadoDaAssinatura.Instancia;

        var requisicaoDeCobranca = new RequisicaoDeCobranca(
            fatura.Identificador,
            assinatura.Identificador,
            assinatura.IdentificadorDoCliente,
            fatura.Valor);

        var resultadoDaCobranca = await _gatewayPagamento.ProcessarCobrancaAsync(requisicaoDeCobranca, cancellationToken);

        if (resultadoDaCobranca.EhFalha)
        {
            _logger.LogWarning(
                "Registro de pagamento da fatura {IdentificadorDaFatura} interrompido por falha no gateway: {Erros}.",
                comando.IdentificadorDaFatura,
                string.Join("; ", resultadoDaCobranca.Erros!));

            return Resultado<SituacaoDoPagamento>.Falha(resultadoDaCobranca.Erros!);
        }

        var retornoDaCobranca = resultadoDaCobranca.Instancia;

        var resultadoDoRegistro = retornoDaCobranca.FoiAprovado
            ? _servicoReativacao.RegistrarPagamentoAprovado(assinatura, fatura)
            : _servicoInadimplencia.RegistrarPagamentoRecusado(assinatura, fatura);

        if (resultadoDoRegistro.EhFalha)
        {
            _logger.LogWarning(
                "Registro de pagamento da fatura {IdentificadorDaFatura} rejeitado pelo domínio: {Erros}.",
                comando.IdentificadorDaFatura,
                string.Join("; ", resultadoDoRegistro.Erros!));

            return Resultado<SituacaoDoPagamento>.Falha(resultadoDoRegistro.Erros!);
        }

        var resultadoDoSalvamento = await _unitOfWork.SalvarAlteracoesAsync(cancellationToken);

        if (resultadoDoSalvamento.EhFalha)
        {
            _logger.LogWarning(
                "Falha ao persistir o registro de pagamento da fatura {IdentificadorDaFatura}: {Erros}.",
                comando.IdentificadorDaFatura,
                string.Join("; ", resultadoDoSalvamento.Erros!));

            return Resultado<SituacaoDoPagamento>.Falha(resultadoDoSalvamento.Erros!);
        }

        _logger.LogInformation(
            "Pagamento da fatura {IdentificadorDaFatura} registrado com situação {SituacaoDoPagamento}. Assinatura {IdentificadorDaAssinatura} com status {StatusDaAssinatura}.",
            fatura.Identificador,
            retornoDaCobranca.Situacao,
            assinatura.Identificador,
            assinatura.Status);

        return Resultado<SituacaoDoPagamento>.Sucesso(retornoDaCobranca.Situacao);
    }
}
