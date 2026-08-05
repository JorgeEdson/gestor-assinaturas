using GestorAssinaturas.Aplicacao.Portas;
using GestorAssinaturas.Aplicacao.Portas.Persistencia;
using GestorAssinaturas.Dominio.Assinaturas;
using GestorAssinaturas.Dominio.Comum;
using Microsoft.Extensions.Logging;

namespace GestorAssinaturas.Aplicacao.Assinaturas;

public sealed record CriarAssinaturaEntrada(
    Guid IdentificadorDoCliente,
    Guid IdentificadorDoPlano);

public sealed class CriarAssinaturaApplicationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRelogioDoSistema _relogioDoSistema;
    private readonly ILogger<CriarAssinaturaApplicationService> _logger;

    public CriarAssinaturaApplicationService(
        IUnitOfWork unitOfWork,
        IRelogioDoSistema relogioDoSistema,
        ILogger<CriarAssinaturaApplicationService> logger)
    {
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(relogioDoSistema);
        ArgumentNullException.ThrowIfNull(logger);

        _unitOfWork = unitOfWork;
        _relogioDoSistema = relogioDoSistema;
        _logger = logger;
    }

    public async Task<Resultado<Guid>> ExecutarAsync(
        CriarAssinaturaEntrada comando,
        CancellationToken cancellationToken = default)
    {
        if (comando is null)
        {
            return Resultado<Guid>.Falha("O comando de criação de assinatura é obrigatório.");
        }

        _logger.LogInformation(
            "Iniciando criação de assinatura para o cliente {IdentificadorDoCliente} no plano {IdentificadorDoPlano}.",
            comando.IdentificadorDoCliente,
            comando.IdentificadorDoPlano);

        var resultadoDoCliente = await _unitOfWork.Clientes.ObterPorIdentificadorAsync(
            comando.IdentificadorDoCliente,
            cancellationToken);

        if (resultadoDoCliente.EhFalha)
        {
            return Resultado<Guid>.Falha(resultadoDoCliente.Erros!);
        }

        if (resultadoDoCliente.Instancia is null)
        {
            _logger.LogWarning(
                "Criação de assinatura rejeitada: cliente {IdentificadorDoCliente} não encontrado.",
                comando.IdentificadorDoCliente);

            return Resultado<Guid>.Falha("Cliente não encontrado.");
        }

        var resultadoDoPlano = await _unitOfWork.Planos.ObterPorIdentificadorAsync(
            comando.IdentificadorDoPlano,
            cancellationToken);

        if (resultadoDoPlano.EhFalha)
        {
            return Resultado<Guid>.Falha(resultadoDoPlano.Erros!);
        }

        if (resultadoDoPlano.Instancia is null)
        {
            _logger.LogWarning(
                "Criação de assinatura rejeitada: plano {IdentificadorDoPlano} não encontrado.",
                comando.IdentificadorDoPlano);

            return Resultado<Guid>.Falha("Plano não encontrado.");
        }

        var dataDeInicio = _relogioDoSistema.ObterDataAtual();

        var resultadoDaAssinatura = Assinatura.Criar(
            Guid.NewGuid(),
            resultadoDoCliente.Instancia.Identificador,
            resultadoDoPlano.Instancia,
            dataDeInicio);

        if (resultadoDaAssinatura.EhFalha)
        {
            _logger.LogWarning(
                "Criação de assinatura rejeitada pelas invariantes de domínio: {Erros}.",
                string.Join("; ", resultadoDaAssinatura.Erros!));

            return Resultado<Guid>.Falha(resultadoDaAssinatura.Erros!);
        }

        var assinatura = resultadoDaAssinatura.Instancia;

        await _unitOfWork.Assinaturas.AdicionarAsync(assinatura, cancellationToken);

        if (assinatura.PrecisaDeCobrancaImediata())
        {
            var resultadoDaFatura = assinatura.GerarFaturaDeCobranca(Guid.NewGuid(), dataDeInicio);

            if (resultadoDaFatura.EhFalha)
            {
                _logger.LogWarning(
                    "Criação de assinatura rejeitada na geração da cobrança imediata: {Erros}.",
                    string.Join("; ", resultadoDaFatura.Erros!));

                return Resultado<Guid>.Falha(resultadoDaFatura.Erros!);
            }

            await _unitOfWork.Faturas.AdicionarAsync(resultadoDaFatura.Instancia, cancellationToken);
        }

        var resultadoDoSalvamento = await _unitOfWork.SalvarAlteracoesAsync(cancellationToken);

        if (resultadoDoSalvamento.EhFalha)
        {
            _logger.LogWarning(
                "Falha ao persistir a criação da assinatura {IdentificadorDaAssinatura}: {Erros}.",
                assinatura.Identificador,
                string.Join("; ", resultadoDoSalvamento.Erros!));

            return Resultado<Guid>.Falha(resultadoDoSalvamento.Erros!);
        }

        _logger.LogInformation(
            "Assinatura {IdentificadorDaAssinatura} criada com status {StatusDaAssinatura}.",
            assinatura.Identificador,
            assinatura.Status);

        return Resultado<Guid>.Sucesso(assinatura.Identificador);
    }
}
