using GestorAssinaturas.Aplicacao.Portas.Pagamentos;
using GestorAssinaturas.Dominio.Comum;
using GestorAssinaturas.Dominio.Repositorios;
using GestorAssinaturas.Dominio.Servicos;
using Microsoft.Extensions.Logging;

namespace GestorAssinaturas.Aplicacao.Assinaturas;

public sealed record RegistrarPagamentoEntrada(
    Guid IdentificadorDaFatura);

public sealed class RegistrarPagamentoServicoDeAplicacao
{
    private readonly IUnidadeDeTrabalho _unidadeDeTrabalho;
    private readonly IGatewayPagamento _gatewayPagamento;
    private readonly ServicoReativacao _servicoReativacao;
    private readonly ServicoInadimplencia _servicoInadimplencia;
    private readonly ILogger<RegistrarPagamentoServicoDeAplicacao> _registradorDeLog;

    public RegistrarPagamentoServicoDeAplicacao(
        IUnidadeDeTrabalho unidadeDeTrabalho,
        IGatewayPagamento gatewayPagamento,
        ServicoReativacao servicoReativacao,
        ServicoInadimplencia servicoInadimplencia,
        ILogger<RegistrarPagamentoServicoDeAplicacao> registradorDeLog)
    {
        ArgumentNullException.ThrowIfNull(unidadeDeTrabalho);
        ArgumentNullException.ThrowIfNull(gatewayPagamento);
        ArgumentNullException.ThrowIfNull(servicoReativacao);
        ArgumentNullException.ThrowIfNull(servicoInadimplencia);
        ArgumentNullException.ThrowIfNull(registradorDeLog);

        _unidadeDeTrabalho = unidadeDeTrabalho;
        _gatewayPagamento = gatewayPagamento;
        _servicoReativacao = servicoReativacao;
        _servicoInadimplencia = servicoInadimplencia;
        _registradorDeLog = registradorDeLog;
    }

    public async Task<Resultado<SituacaoDoPagamento>> ExecutarAsync(
        RegistrarPagamentoEntrada entrada,
        CancellationToken tokenDeCancelamento = default)
    {
        if (entrada is null)
        {
            return Resultado<SituacaoDoPagamento>.Falha("Os dados de entrada do registro de pagamento são obrigatórios.");
        }

        _registradorDeLog.LogInformation(
            "Iniciando registro de pagamento da fatura {IdentificadorDaFatura}.",
            entrada.IdentificadorDaFatura);

        var fatura = await _unidadeDeTrabalho.Faturas.ObterPorIdentificadorAsync(
            entrada.IdentificadorDaFatura,
            tokenDeCancelamento);

        if (fatura is null)
        {
            _registradorDeLog.LogWarning(
                "Registro de pagamento rejeitado: fatura {IdentificadorDaFatura} não encontrada.",
                entrada.IdentificadorDaFatura);

            return Resultado<SituacaoDoPagamento>.NaoEncontrado("Fatura não encontrada.");
        }

        var assinatura = await _unidadeDeTrabalho.Assinaturas.ObterPorIdentificadorAsync(
            fatura.IdentificadorDaAssinatura,
            tokenDeCancelamento);

        if (assinatura is null)
        {
            _registradorDeLog.LogWarning(
                "Registro de pagamento rejeitado: assinatura {IdentificadorDaAssinatura} não encontrada.",
                fatura.IdentificadorDaAssinatura);

            return Resultado<SituacaoDoPagamento>.NaoEncontrado("Assinatura não encontrada.");
        }

        var resultadoDaVerificacao = assinatura.VerificarSePodeReceberPagamentoDa(fatura);

        if (resultadoDaVerificacao.EhFalha)
        {
            _registradorDeLog.LogWarning(
                "Registro de pagamento da fatura {IdentificadorDaFatura} rejeitado pelo domínio antes de acionar o gateway: {Erros}.",
                entrada.IdentificadorDaFatura,
                string.Join("; ", resultadoDaVerificacao.Erros!));

            return Resultado<SituacaoDoPagamento>.FalhaAPartirDe(resultadoDaVerificacao);
        }

        var requisicaoDeCobranca = new RequisicaoDeCobranca(
            fatura.Identificador,
            assinatura.Identificador,
            assinatura.IdentificadorDoCliente,
            fatura.Valor);

        var resultadoDaCobranca = await _gatewayPagamento.ProcessarCobrancaAsync(requisicaoDeCobranca, tokenDeCancelamento);

        if (resultadoDaCobranca.EhFalha)
        {
            _registradorDeLog.LogWarning(
                "Registro de pagamento da fatura {IdentificadorDaFatura} interrompido por falha no gateway: {Erros}.",
                entrada.IdentificadorDaFatura,
                string.Join("; ", resultadoDaCobranca.Erros!));

            return Resultado<SituacaoDoPagamento>.FalhaAPartirDe(resultadoDaCobranca);
        }

        var retornoDaCobranca = resultadoDaCobranca.Instancia;

        var resultadoDoRegistro = retornoDaCobranca.FoiAprovado
            ? _servicoReativacao.RegistrarPagamentoAprovado(assinatura, fatura)
            : _servicoInadimplencia.RegistrarPagamentoRecusado(assinatura, fatura);

        if (resultadoDoRegistro.EhFalha)
        {
            _registradorDeLog.LogWarning(
                "Registro de pagamento da fatura {IdentificadorDaFatura} rejeitado pelo domínio: {Erros}.",
                entrada.IdentificadorDaFatura,
                string.Join("; ", resultadoDoRegistro.Erros!));

            return Resultado<SituacaoDoPagamento>.FalhaAPartirDe(resultadoDoRegistro);
        }

        await _unidadeDeTrabalho.SalvarAlteracoesAsync(tokenDeCancelamento);

        _registradorDeLog.LogInformation(
            "Pagamento da fatura {IdentificadorDaFatura} registrado com situação {SituacaoDoPagamento}. Assinatura {IdentificadorDaAssinatura} com status {StatusDaAssinatura}.",
            fatura.Identificador,
            retornoDaCobranca.Situacao,
            assinatura.Identificador,
            assinatura.Status);

        return Resultado<SituacaoDoPagamento>.Sucesso(retornoDaCobranca.Situacao);
    }
}
