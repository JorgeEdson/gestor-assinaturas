using GestorAssinaturas.Aplicacao.Portas;
using GestorAssinaturas.Dominio.Comum;
using GestorAssinaturas.Dominio.Repositorios;
using Microsoft.Extensions.Logging;

namespace GestorAssinaturas.Aplicacao.Assinaturas;

public sealed record AtivarAssinaturaEntrada(
    Guid IdentificadorDaAssinatura);

public sealed class AtivarAssinaturaServicoDeAplicacao
{
    private readonly IUnidadeDeTrabalho _unidadeDeTrabalho;
    private readonly IRelogioDoSistema _relogioDoSistema;
    private readonly ILogger<AtivarAssinaturaServicoDeAplicacao> _registradorDeLog;

    public AtivarAssinaturaServicoDeAplicacao(
        IUnidadeDeTrabalho unidadeDeTrabalho,
        IRelogioDoSistema relogioDoSistema,
        ILogger<AtivarAssinaturaServicoDeAplicacao> registradorDeLog)
    {
        ArgumentNullException.ThrowIfNull(unidadeDeTrabalho);
        ArgumentNullException.ThrowIfNull(relogioDoSistema);
        ArgumentNullException.ThrowIfNull(registradorDeLog);

        _unidadeDeTrabalho = unidadeDeTrabalho;
        _relogioDoSistema = relogioDoSistema;
        _registradorDeLog = registradorDeLog;
    }

    public async Task<Resultado<Guid>> ExecutarAsync(
        AtivarAssinaturaEntrada entrada,
        CancellationToken tokenDeCancelamento = default)
    {
        if (entrada is null)
        {
            return Resultado<Guid>.Falha("Os dados de entrada da ativação de assinatura são obrigatórios.");
        }

        _registradorDeLog.LogInformation(
            "Iniciando ativação da assinatura {IdentificadorDaAssinatura}.",
            entrada.IdentificadorDaAssinatura);

        var assinatura = await _unidadeDeTrabalho.Assinaturas.ObterPorIdentificadorAsync(
            entrada.IdentificadorDaAssinatura,
            tokenDeCancelamento);

        if (assinatura is null)
        {
            _registradorDeLog.LogWarning(
                "Ativação rejeitada: assinatura {IdentificadorDaAssinatura} não encontrada.",
                entrada.IdentificadorDaAssinatura);

            return Resultado<Guid>.NaoEncontrado("Assinatura não encontrada.");
        }

        var dataDeAtivacao = _relogioDoSistema.ObterDataAtual();

        var resultadoDaAtivacao = assinatura.Ativar(dataDeAtivacao);

        if (resultadoDaAtivacao.EhFalha)
        {
            _registradorDeLog.LogWarning(
                "Ativação da assinatura {IdentificadorDaAssinatura} rejeitada pelo domínio: {Erros}.",
                entrada.IdentificadorDaAssinatura,
                string.Join("; ", resultadoDaAtivacao.Erros!));

            return Resultado<Guid>.FalhaAPartirDe(resultadoDaAtivacao);
        }

        var resultadoDaFatura = assinatura.GerarFaturaDeCobranca(Guid.NewGuid(), dataDeAtivacao);

        if (resultadoDaFatura.EhFalha)
        {
            _registradorDeLog.LogWarning(
                "Ativação da assinatura {IdentificadorDaAssinatura} rejeitada na geração da primeira fatura: {Erros}.",
                entrada.IdentificadorDaAssinatura,
                string.Join("; ", resultadoDaFatura.Erros!));

            return Resultado<Guid>.FalhaAPartirDe(resultadoDaFatura);
        }

        var primeiraFatura = resultadoDaFatura.Instancia;

        await _unidadeDeTrabalho.Faturas.AdicionarAsync(primeiraFatura, tokenDeCancelamento);
        await _unidadeDeTrabalho.SalvarAlteracoesAsync(tokenDeCancelamento);

        _registradorDeLog.LogInformation(
            "Assinatura {IdentificadorDaAssinatura} ativada e primeira fatura {IdentificadorDaFatura} gerada.",
            assinatura.Identificador,
            primeiraFatura.Identificador);

        return Resultado<Guid>.Sucesso(primeiraFatura.Identificador);
    }
}
