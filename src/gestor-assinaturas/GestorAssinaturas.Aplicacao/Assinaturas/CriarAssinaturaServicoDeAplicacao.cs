using GestorAssinaturas.Aplicacao.Portas;
using GestorAssinaturas.Dominio.Assinaturas;
using GestorAssinaturas.Dominio.Comum;
using GestorAssinaturas.Dominio.Repositorios;
using Microsoft.Extensions.Logging;

namespace GestorAssinaturas.Aplicacao.Assinaturas;

public sealed record CriarAssinaturaEntrada(
    Guid IdentificadorDoCliente,
    Guid IdentificadorDoPlano);

public sealed class CriarAssinaturaServicoDeAplicacao
{
    private readonly IUnidadeDeTrabalho _unidadeDeTrabalho;
    private readonly IRelogioDoSistema _relogioDoSistema;
    private readonly ILogger<CriarAssinaturaServicoDeAplicacao> _registradorDeLog;

    public CriarAssinaturaServicoDeAplicacao(
        IUnidadeDeTrabalho unidadeDeTrabalho,
        IRelogioDoSistema relogioDoSistema,
        ILogger<CriarAssinaturaServicoDeAplicacao> registradorDeLog)
    {
        ArgumentNullException.ThrowIfNull(unidadeDeTrabalho);
        ArgumentNullException.ThrowIfNull(relogioDoSistema);
        ArgumentNullException.ThrowIfNull(registradorDeLog);

        _unidadeDeTrabalho = unidadeDeTrabalho;
        _relogioDoSistema = relogioDoSistema;
        _registradorDeLog = registradorDeLog;
    }

    public async Task<Resultado<Guid>> ExecutarAsync(
        CriarAssinaturaEntrada entrada,
        CancellationToken tokenDeCancelamento = default)
    {
        if (entrada is null)
        {
            return Resultado<Guid>.Falha("Os dados de entrada da criação de assinatura são obrigatórios.");
        }

        _registradorDeLog.LogInformation(
            "Iniciando criação de assinatura para o cliente {IdentificadorDoCliente} no plano {IdentificadorDoPlano}.",
            entrada.IdentificadorDoCliente,
            entrada.IdentificadorDoPlano);

        var cliente = await _unidadeDeTrabalho.Clientes.ObterPorIdentificadorAsync(
            entrada.IdentificadorDoCliente,
            tokenDeCancelamento);

        if (cliente is null)
        {
            _registradorDeLog.LogWarning(
                "Criação de assinatura rejeitada: cliente {IdentificadorDoCliente} não encontrado.",
                entrada.IdentificadorDoCliente);

            return Resultado<Guid>.NaoEncontrado("Cliente não encontrado.");
        }

        var plano = await _unidadeDeTrabalho.Planos.ObterPorIdentificadorAsync(
            entrada.IdentificadorDoPlano,
            tokenDeCancelamento);

        if (plano is null)
        {
            _registradorDeLog.LogWarning(
                "Criação de assinatura rejeitada: plano {IdentificadorDoPlano} não encontrado.",
                entrada.IdentificadorDoPlano);

            return Resultado<Guid>.NaoEncontrado("Plano não encontrado.");
        }

        var dataDeInicio = _relogioDoSistema.ObterDataAtual();

        var resultadoDaAssinatura = Assinatura.Criar(
            Guid.NewGuid(),
            cliente.Identificador,
            plano,
            dataDeInicio);

        if (resultadoDaAssinatura.EhFalha)
        {
            _registradorDeLog.LogWarning(
                "Criação de assinatura rejeitada pelas invariantes de domínio: {Erros}.",
                string.Join("; ", resultadoDaAssinatura.Erros!));

            return Resultado<Guid>.FalhaAPartirDe(resultadoDaAssinatura);
        }

        var assinatura = resultadoDaAssinatura.Instancia;

        await _unidadeDeTrabalho.Assinaturas.AdicionarAsync(assinatura, tokenDeCancelamento);

        if (assinatura.PrecisaDeCobrancaImediata())
        {
            var resultadoDaFatura = assinatura.GerarFaturaDeCobranca(Guid.NewGuid(), dataDeInicio);

            if (resultadoDaFatura.EhFalha)
            {
                _registradorDeLog.LogWarning(
                    "Criação de assinatura rejeitada na geração da cobrança imediata: {Erros}.",
                    string.Join("; ", resultadoDaFatura.Erros!));

                return Resultado<Guid>.FalhaAPartirDe(resultadoDaFatura);
            }

            await _unidadeDeTrabalho.Faturas.AdicionarAsync(resultadoDaFatura.Instancia, tokenDeCancelamento);
        }

        await _unidadeDeTrabalho.SalvarAlteracoesAsync(tokenDeCancelamento);

        _registradorDeLog.LogInformation(
            "Assinatura {IdentificadorDaAssinatura} criada com status {StatusDaAssinatura}.",
            assinatura.Identificador,
            assinatura.Status);

        return Resultado<Guid>.Sucesso(assinatura.Identificador);
    }
}
