using GestorAssinaturas.Aplicacao.Portas;
using GestorAssinaturas.Aplicacao.Portas.Persistencia;
using GestorAssinaturas.Dominio.Comum;
using Microsoft.Extensions.Logging;

namespace GestorAssinaturas.Aplicacao.Assinaturas;

public sealed record AtivarAssinaturaEntrada(
    Guid IdentificadorDaAssinatura);

public sealed class AtivarAssinaturaApplicationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRelogioDoSistema _relogioDoSistema;
    private readonly ILogger<AtivarAssinaturaApplicationService> _logger;

    public AtivarAssinaturaApplicationService(
        IUnitOfWork unitOfWork,
        IRelogioDoSistema relogioDoSistema,
        ILogger<AtivarAssinaturaApplicationService> logger)
    {
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(relogioDoSistema);
        ArgumentNullException.ThrowIfNull(logger);

        _unitOfWork = unitOfWork;
        _relogioDoSistema = relogioDoSistema;
        _logger = logger;
    }

    public async Task<Resultado<Guid>> ExecutarAsync(
        AtivarAssinaturaEntrada comando,
        CancellationToken cancellationToken = default)
    {
        if (comando is null)
        {
            return Resultado<Guid>.Falha("O comando de ativação de assinatura é obrigatório.");
        }

        _logger.LogInformation(
            "Iniciando ativação da assinatura {IdentificadorDaAssinatura}.",
            comando.IdentificadorDaAssinatura);

        var resultadoDaAssinatura = await _unitOfWork.Assinaturas.ObterPorIdentificadorAsync(
            comando.IdentificadorDaAssinatura,
            cancellationToken);

        if (resultadoDaAssinatura.EhFalha)
        {
            return Resultado<Guid>.Falha(resultadoDaAssinatura.Erros!);
        }

        if (resultadoDaAssinatura.Instancia is null)
        {
            _logger.LogWarning(
                "Ativação rejeitada: assinatura {IdentificadorDaAssinatura} não encontrada.",
                comando.IdentificadorDaAssinatura);

            return Resultado<Guid>.Falha("Assinatura não encontrada.");
        }

        var assinatura = resultadoDaAssinatura.Instancia;

        var resultadoDaAtivacao = assinatura.Ativar();

        if (resultadoDaAtivacao.EhFalha)
        {
            _logger.LogWarning(
                "Ativação da assinatura {IdentificadorDaAssinatura} rejeitada pelo domínio: {Erros}.",
                comando.IdentificadorDaAssinatura,
                string.Join("; ", resultadoDaAtivacao.Erros!));

            return Resultado<Guid>.Falha(resultadoDaAtivacao.Erros!);
        }

        var dataDeReferencia = _relogioDoSistema.ObterDataAtual();

        var resultadoDaFatura = assinatura.GerarFaturaDeCobranca(Guid.NewGuid(), dataDeReferencia);

        if (resultadoDaFatura.EhFalha)
        {
            _logger.LogWarning(
                "Ativação da assinatura {IdentificadorDaAssinatura} rejeitada na geração da primeira fatura: {Erros}.",
                comando.IdentificadorDaAssinatura,
                string.Join("; ", resultadoDaFatura.Erros!));

            return Resultado<Guid>.Falha(resultadoDaFatura.Erros!);
        }

        var primeiraFatura = resultadoDaFatura.Instancia;

        await _unitOfWork.Faturas.AdicionarAsync(primeiraFatura, cancellationToken);

        var resultadoDoSalvamento = await _unitOfWork.SalvarAlteracoesAsync(cancellationToken);

        if (resultadoDoSalvamento.EhFalha)
        {
            _logger.LogWarning(
                "Falha ao persistir a ativação da assinatura {IdentificadorDaAssinatura}: {Erros}.",
                comando.IdentificadorDaAssinatura,
                string.Join("; ", resultadoDoSalvamento.Erros!));

            return Resultado<Guid>.Falha(resultadoDoSalvamento.Erros!);
        }

        _logger.LogInformation(
            "Assinatura {IdentificadorDaAssinatura} ativada e primeira fatura {IdentificadorDaFatura} gerada.",
            assinatura.Identificador,
            primeiraFatura.Identificador);

        return Resultado<Guid>.Sucesso(primeiraFatura.Identificador);
    }
}
