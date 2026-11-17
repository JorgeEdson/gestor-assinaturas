using GestorAssinaturas.Aplicacao.Portas;
using GestorAssinaturas.Dominio.Assinaturas;
using GestorAssinaturas.Dominio.Comum;
using GestorAssinaturas.Dominio.Repositorios;
using Microsoft.Extensions.Logging;

namespace GestorAssinaturas.Aplicacao.Assinaturas;

public sealed record CancelarAssinaturaEntrada(
    Guid IdentificadorDaAssinatura,
    ModalidadeDeCancelamento Modalidade);

public sealed class CancelarAssinaturaServicoDeAplicacao
{
    private readonly IUnidadeDeTrabalho _unidadeDeTrabalho;
    private readonly IRelogioDoSistema _relogioDoSistema;
    private readonly ILogger<CancelarAssinaturaServicoDeAplicacao> _registradorDeLog;

    public CancelarAssinaturaServicoDeAplicacao(
        IUnidadeDeTrabalho unidadeDeTrabalho,
        IRelogioDoSistema relogioDoSistema,
        ILogger<CancelarAssinaturaServicoDeAplicacao> registradorDeLog)
    {
        ArgumentNullException.ThrowIfNull(unidadeDeTrabalho);
        ArgumentNullException.ThrowIfNull(relogioDoSistema);
        ArgumentNullException.ThrowIfNull(registradorDeLog);

        _unidadeDeTrabalho = unidadeDeTrabalho;
        _relogioDoSistema = relogioDoSistema;
        _registradorDeLog = registradorDeLog;
    }

    public async Task<Resultado> ExecutarAsync(
        CancelarAssinaturaEntrada entrada,
        CancellationToken tokenDeCancelamento = default)
    {
        if (entrada is null)
        {
            return Resultado.Falha("Os dados de entrada do cancelamento de assinatura são obrigatórios.");
        }

        _registradorDeLog.LogInformation(
            "Iniciando cancelamento da assinatura {IdentificadorDaAssinatura} na modalidade {ModalidadeDeCancelamento}.",
            entrada.IdentificadorDaAssinatura,
            entrada.Modalidade);

        var assinatura = await _unidadeDeTrabalho.Assinaturas.ObterPorIdentificadorAsync(
            entrada.IdentificadorDaAssinatura,
            tokenDeCancelamento);

        if (assinatura is null)
        {
            _registradorDeLog.LogWarning(
                "Cancelamento rejeitado: assinatura {IdentificadorDaAssinatura} não encontrada.",
                entrada.IdentificadorDaAssinatura);

            return Resultado.NaoEncontrado("Assinatura não encontrada.");
        }

        var resultadoDoCancelamento = assinatura.Cancelar(
            entrada.Modalidade,
            _relogioDoSistema.ObterDataAtual());

        if (resultadoDoCancelamento.EhFalha)
        {
            _registradorDeLog.LogWarning(
                "Cancelamento da assinatura {IdentificadorDaAssinatura} rejeitado pelo domínio: {Erros}.",
                entrada.IdentificadorDaAssinatura,
                string.Join("; ", resultadoDoCancelamento.Erros!));

            return Resultado.FalhaAPartirDe(resultadoDoCancelamento);
        }

        await _unidadeDeTrabalho.SalvarAlteracoesAsync(tokenDeCancelamento);

        _registradorDeLog.LogInformation(
            "Cancelamento da assinatura {IdentificadorDaAssinatura} registrado na modalidade {ModalidadeDeCancelamento}. Status atual {StatusDaAssinatura}.",
            assinatura.Identificador,
            entrada.Modalidade,
            assinatura.Status);

        return Resultado.Sucesso();
    }
}
