using GestorAssinaturas.Dominio.Comum;
using GestorAssinaturas.Dominio.Repositorios;
using GestorAssinaturas.Dominio.Servicos;
using Microsoft.Extensions.Logging;

namespace GestorAssinaturas.Aplicacao.Assinaturas;

public sealed record TrocarPlanoEntrada(
    Guid IdentificadorDaAssinatura,
    Guid IdentificadorDoNovoPlano);

public sealed class TrocarPlanoServicoDeAplicacao
{
    private readonly IUnidadeDeTrabalho _unidadeDeTrabalho;
    private readonly ServicoTrocaDePlano _servicoTrocaDePlano;
    private readonly ILogger<TrocarPlanoServicoDeAplicacao> _registradorDeLog;

    public TrocarPlanoServicoDeAplicacao(
        IUnidadeDeTrabalho unidadeDeTrabalho,
        ServicoTrocaDePlano servicoTrocaDePlano,
        ILogger<TrocarPlanoServicoDeAplicacao> registradorDeLog)
    {
        ArgumentNullException.ThrowIfNull(unidadeDeTrabalho);
        ArgumentNullException.ThrowIfNull(servicoTrocaDePlano);
        ArgumentNullException.ThrowIfNull(registradorDeLog);

        _unidadeDeTrabalho = unidadeDeTrabalho;
        _servicoTrocaDePlano = servicoTrocaDePlano;
        _registradorDeLog = registradorDeLog;
    }

    public async Task<Resultado> ExecutarAsync(
        TrocarPlanoEntrada entrada,
        CancellationToken tokenDeCancelamento = default)
    {
        if (entrada is null)
        {
            return Resultado.Falha("Os dados de entrada da troca de plano são obrigatórios.");
        }

        _registradorDeLog.LogInformation(
            "Iniciando troca de plano da assinatura {IdentificadorDaAssinatura} para o plano {IdentificadorDoNovoPlano}.",
            entrada.IdentificadorDaAssinatura,
            entrada.IdentificadorDoNovoPlano);

        var assinatura = await _unidadeDeTrabalho.Assinaturas.ObterPorIdentificadorAsync(
            entrada.IdentificadorDaAssinatura,
            tokenDeCancelamento);

        if (assinatura is null)
        {
            _registradorDeLog.LogWarning(
                "Troca de plano rejeitada: assinatura {IdentificadorDaAssinatura} não encontrada.",
                entrada.IdentificadorDaAssinatura);

            return Resultado.NaoEncontrado("Assinatura não encontrada.");
        }

        var novoPlano = await _unidadeDeTrabalho.Planos.ObterPorIdentificadorAsync(
            entrada.IdentificadorDoNovoPlano,
            tokenDeCancelamento);

        if (novoPlano is null)
        {
            _registradorDeLog.LogWarning(
                "Troca de plano rejeitada: plano {IdentificadorDoNovoPlano} não encontrado.",
                entrada.IdentificadorDoNovoPlano);

            return Resultado.NaoEncontrado("Plano não encontrado.");
        }

        var faturasDaAssinatura = await _unidadeDeTrabalho.Faturas.ListarPorAssinaturaAsync(
            assinatura.Identificador,
            tokenDeCancelamento);

        var resultadoDaTroca = _servicoTrocaDePlano.TrocarPlano(assinatura, novoPlano, faturasDaAssinatura);

        if (resultadoDaTroca.EhFalha)
        {
            _registradorDeLog.LogWarning(
                "Troca de plano da assinatura {IdentificadorDaAssinatura} rejeitada pelo domínio: {Erros}.",
                entrada.IdentificadorDaAssinatura,
                string.Join("; ", resultadoDaTroca.Erros!));

            return Resultado.FalhaAPartirDe(resultadoDaTroca);
        }

        await _unidadeDeTrabalho.SalvarAlteracoesAsync(tokenDeCancelamento);

        _registradorDeLog.LogInformation(
            "Assinatura {IdentificadorDaAssinatura} trocada para o plano {IdentificadorDoNovoPlano}.",
            assinatura.Identificador,
            assinatura.IdentificadorDoPlano);

        return Resultado.Sucesso();
    }
}
