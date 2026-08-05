using GestorAssinaturas.Aplicacao.Portas.Persistencia;
using GestorAssinaturas.Dominio.Assinaturas;
using GestorAssinaturas.Dominio.Comum;
using Microsoft.Extensions.Logging;

namespace GestorAssinaturas.Aplicacao.Assinaturas;

public sealed record TrocarPlanoEntrada(
    Guid IdentificadorDaAssinatura,
    Guid IdentificadorDoNovoPlano);

public sealed class TrocarPlanoApplicationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ServicoTrocaDePlano _servicoTrocaDePlano;
    private readonly ILogger<TrocarPlanoApplicationService> _logger;

    public TrocarPlanoApplicationService(
        IUnitOfWork unitOfWork,
        ServicoTrocaDePlano servicoTrocaDePlano,
        ILogger<TrocarPlanoApplicationService> logger)
    {
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(servicoTrocaDePlano);
        ArgumentNullException.ThrowIfNull(logger);

        _unitOfWork = unitOfWork;
        _servicoTrocaDePlano = servicoTrocaDePlano;
        _logger = logger;
    }

    public async Task<Resultado> ExecutarAsync(
        TrocarPlanoEntrada comando,
        CancellationToken cancellationToken = default)
    {
        if (comando is null)
        {
            return Resultado.Falha("O comando de troca de plano é obrigatório.");
        }

        _logger.LogInformation(
            "Iniciando troca de plano da assinatura {IdentificadorDaAssinatura} para o plano {IdentificadorDoNovoPlano}.",
            comando.IdentificadorDaAssinatura,
            comando.IdentificadorDoNovoPlano);

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
                "Troca de plano rejeitada: assinatura {IdentificadorDaAssinatura} não encontrada.",
                comando.IdentificadorDaAssinatura);

            return Resultado.Falha("Assinatura não encontrada.");
        }

        var resultadoDoNovoPlano = await _unitOfWork.Planos.ObterPorIdentificadorAsync(
            comando.IdentificadorDoNovoPlano,
            cancellationToken);

        if (resultadoDoNovoPlano.EhFalha)
        {
            return Resultado.Falha(resultadoDoNovoPlano.Erros!);
        }

        if (resultadoDoNovoPlano.Instancia is null)
        {
            _logger.LogWarning(
                "Troca de plano rejeitada: plano {IdentificadorDoNovoPlano} não encontrado.",
                comando.IdentificadorDoNovoPlano);

            return Resultado.Falha("Plano não encontrado.");
        }

        var assinatura = resultadoDaAssinatura.Instancia;

        var resultadoDasFaturas = await _unitOfWork.Faturas.ListarPorAssinaturaAsync(
            assinatura.Identificador,
            cancellationToken);

        if (resultadoDasFaturas.EhFalha)
        {
            return Resultado.Falha(resultadoDasFaturas.Erros!);
        }

        var resultadoDaTroca = _servicoTrocaDePlano.TrocarPlano(
            assinatura,
            resultadoDoNovoPlano.Instancia,
            resultadoDasFaturas.Instancia);

        if (resultadoDaTroca.EhFalha)
        {
            _logger.LogWarning(
                "Troca de plano da assinatura {IdentificadorDaAssinatura} rejeitada pelo domínio: {Erros}.",
                comando.IdentificadorDaAssinatura,
                string.Join("; ", resultadoDaTroca.Erros!));

            return Resultado.Falha(resultadoDaTroca.Erros!);
        }

        var resultadoDoSalvamento = await _unitOfWork.SalvarAlteracoesAsync(cancellationToken);

        if (resultadoDoSalvamento.EhFalha)
        {
            _logger.LogWarning(
                "Falha ao persistir a troca de plano da assinatura {IdentificadorDaAssinatura}: {Erros}.",
                comando.IdentificadorDaAssinatura,
                string.Join("; ", resultadoDoSalvamento.Erros!));

            return Resultado.Falha(resultadoDoSalvamento.Erros!);
        }

        _logger.LogInformation(
            "Assinatura {IdentificadorDaAssinatura} trocada para o plano {IdentificadorDoNovoPlano}.",
            assinatura.Identificador,
            assinatura.IdentificadorDoPlano);

        return Resultado.Sucesso();
    }
}
