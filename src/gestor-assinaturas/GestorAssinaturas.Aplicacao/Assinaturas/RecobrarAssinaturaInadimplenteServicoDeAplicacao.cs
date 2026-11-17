using GestorAssinaturas.Aplicacao.Portas;
using GestorAssinaturas.Dominio.Comum;
using GestorAssinaturas.Dominio.Repositorios;
using GestorAssinaturas.Dominio.Servicos;
using Microsoft.Extensions.Logging;

namespace GestorAssinaturas.Aplicacao.Assinaturas;

public sealed record RecobrarAssinaturaInadimplenteEntrada(
    Guid IdentificadorDaAssinatura);

public sealed class RecobrarAssinaturaInadimplenteServicoDeAplicacao
{
    private readonly IUnidadeDeTrabalho _unidadeDeTrabalho;
    private readonly IRelogioDoSistema _relogioDoSistema;
    private readonly ServicoRecobranca _servicoRecobranca;
    private readonly ILogger<RecobrarAssinaturaInadimplenteServicoDeAplicacao> _registradorDeLog;

    public RecobrarAssinaturaInadimplenteServicoDeAplicacao(
        IUnidadeDeTrabalho unidadeDeTrabalho,
        IRelogioDoSistema relogioDoSistema,
        ServicoRecobranca servicoRecobranca,
        ILogger<RecobrarAssinaturaInadimplenteServicoDeAplicacao> registradorDeLog)
    {
        ArgumentNullException.ThrowIfNull(unidadeDeTrabalho);
        ArgumentNullException.ThrowIfNull(relogioDoSistema);
        ArgumentNullException.ThrowIfNull(servicoRecobranca);
        ArgumentNullException.ThrowIfNull(registradorDeLog);

        _unidadeDeTrabalho = unidadeDeTrabalho;
        _relogioDoSistema = relogioDoSistema;
        _servicoRecobranca = servicoRecobranca;
        _registradorDeLog = registradorDeLog;
    }

    public async Task<Resultado<Guid>> ExecutarAsync(
        RecobrarAssinaturaInadimplenteEntrada entrada,
        CancellationToken tokenDeCancelamento = default)
    {
        if (entrada is null)
        {
            return Resultado<Guid>.Falha("Os dados de entrada da recobrança são obrigatórios.");
        }

        _registradorDeLog.LogInformation(
            "Iniciando recobrança da assinatura {IdentificadorDaAssinatura}.",
            entrada.IdentificadorDaAssinatura);

        var assinatura = await _unidadeDeTrabalho.Assinaturas.ObterPorIdentificadorAsync(
            entrada.IdentificadorDaAssinatura,
            tokenDeCancelamento);

        if (assinatura is null)
        {
            _registradorDeLog.LogWarning(
                "Recobrança rejeitada: assinatura {IdentificadorDaAssinatura} não encontrada.",
                entrada.IdentificadorDaAssinatura);

            return Resultado<Guid>.NaoEncontrado("Assinatura não encontrada.");
        }

        var faturasDaAssinatura = await _unidadeDeTrabalho.Faturas.ListarPorAssinaturaAsync(
            assinatura.Identificador,
            tokenDeCancelamento);

        var resultadoDaRecobranca = _servicoRecobranca.EmitirFaturaDeRecobranca(
            assinatura,
            faturasDaAssinatura,
            Guid.NewGuid(),
            _relogioDoSistema.ObterDataAtual());

        if (resultadoDaRecobranca.EhFalha)
        {
            _registradorDeLog.LogWarning(
                "Recobrança da assinatura {IdentificadorDaAssinatura} rejeitada pelo domínio: {Erros}.",
                entrada.IdentificadorDaAssinatura,
                string.Join("; ", resultadoDaRecobranca.Erros!));

            return Resultado<Guid>.FalhaAPartirDe(resultadoDaRecobranca);
        }

        var faturaDeRecobranca = resultadoDaRecobranca.Instancia;

        await _unidadeDeTrabalho.Faturas.AdicionarAsync(faturaDeRecobranca, tokenDeCancelamento);
        await _unidadeDeTrabalho.SalvarAlteracoesAsync(tokenDeCancelamento);

        _registradorDeLog.LogInformation(
            "Fatura de recobrança {IdentificadorDaFatura} emitida para a assinatura {IdentificadorDaAssinatura}.",
            faturaDeRecobranca.Identificador,
            assinatura.Identificador);

        return Resultado<Guid>.Sucesso(faturaDeRecobranca.Identificador);
    }
}
