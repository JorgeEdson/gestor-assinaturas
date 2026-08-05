using GestorAssinaturas.Aplicacao.Portas.Persistencia;
using GestorAssinaturas.Dominio.Assinaturas;
using GestorAssinaturas.Dominio.Comum;
using Microsoft.Extensions.Logging;

namespace GestorAssinaturas.Aplicacao.Assinaturas;

public sealed record CancelarAssinaturaEntrada(
    Guid IdentificadorDaAssinatura,
    ModalidadeDeCancelamento Modalidade);

public sealed class CancelarAssinaturaApplicationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CancelarAssinaturaApplicationService> _logger;

    public CancelarAssinaturaApplicationService(
        IUnitOfWork unitOfWork,
        ILogger<CancelarAssinaturaApplicationService> logger)
    {
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(logger);

        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Resultado> ExecutarAsync(
        CancelarAssinaturaEntrada comando,
        CancellationToken cancellationToken = default)
    {
        if (comando is null)
        {
            return Resultado.Falha("O comando de cancelamento de assinatura é obrigatório.");
        }

        _logger.LogInformation(
            "Iniciando cancelamento da assinatura {IdentificadorDaAssinatura} na modalidade {ModalidadeDeCancelamento}.",
            comando.IdentificadorDaAssinatura,
            comando.Modalidade);

        var resultadoDaAssinatura = await _unitOfWork.Assinaturas.ObterPorIdentificadorAsync(
            comando.IdentificadorDaAssinatura,
            cancellationToken);

        if (resultadoDaAssinatura.EhFalha)
        {
            return Resultado.Falha(resultadoDaAssinatura.Erros!);
        }

        if (resultadoDaAssinatura.Instancia is null)
        {
            _logger.LogWarning(
                "Cancelamento rejeitado: assinatura {IdentificadorDaAssinatura} não encontrada.",
                comando.IdentificadorDaAssinatura);

            return Resultado.Falha("Assinatura não encontrada.");
        }

        var assinatura = resultadoDaAssinatura.Instancia;

        var resultadoDoCancelamento = AplicarCancelamento(assinatura, comando.Modalidade);

        if (resultadoDoCancelamento.EhFalha)
        {
            _logger.LogWarning(
                "Cancelamento da assinatura {IdentificadorDaAssinatura} rejeitado pelo domínio: {Erros}.",
                comando.IdentificadorDaAssinatura,
                string.Join("; ", resultadoDoCancelamento.Erros!));

            return Resultado.Falha(resultadoDoCancelamento.Erros!);
        }

        var resultadoDoSalvamento = await _unitOfWork.SalvarAlteracoesAsync(cancellationToken);

        if (resultadoDoSalvamento.EhFalha)
        {
            _logger.LogWarning(
                "Falha ao persistir o cancelamento da assinatura {IdentificadorDaAssinatura}: {Erros}.",
                comando.IdentificadorDaAssinatura,
                string.Join("; ", resultadoDoSalvamento.Erros!));

            return Resultado.Falha(resultadoDoSalvamento.Erros!);
        }

        _logger.LogInformation(
            "Cancelamento da assinatura {IdentificadorDaAssinatura} registrado na modalidade {ModalidadeDeCancelamento}. Status atual {StatusDaAssinatura}.",
            assinatura.Identificador,
            comando.Modalidade,
            assinatura.Status);

        return Resultado.Sucesso();
    }

    private static Resultado AplicarCancelamento(Assinatura assinatura, ModalidadeDeCancelamento modalidade)
    {
        if (modalidade == ModalidadeDeCancelamento.Imediato)
        {
            return assinatura.CancelarImediatamente();
        }

        var dataDeFimDoPeriodoVigente = assinatura.Plano.CalcularDataDeVencimentoDoProximoCiclo(assinatura.DataDeInicio);

        return assinatura.AgendarCancelamentoAoFimDoPeriodo(dataDeFimDoPeriodoVigente);
    }
}
