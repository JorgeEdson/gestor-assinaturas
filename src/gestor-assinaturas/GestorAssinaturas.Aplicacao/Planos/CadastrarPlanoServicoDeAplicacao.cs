using GestorAssinaturas.Dominio.Comum;
using GestorAssinaturas.Dominio.ObjetosDeValor;
using GestorAssinaturas.Dominio.Planos;
using GestorAssinaturas.Dominio.Repositorios;
using Microsoft.Extensions.Logging;

namespace GestorAssinaturas.Aplicacao.Planos;

public sealed record CadastrarPlanoEntrada(
    string Nome,
    decimal Valor,
    string Moeda,
    TipoDeCicloDeCobranca CicloDeCobranca,
    int PeriodoDeTrialEmDias);

public sealed class CadastrarPlanoServicoDeAplicacao
{
    private readonly IUnidadeDeTrabalho _unidadeDeTrabalho;
    private readonly ILogger<CadastrarPlanoServicoDeAplicacao> _registradorDeLog;

    public CadastrarPlanoServicoDeAplicacao(
        IUnidadeDeTrabalho unidadeDeTrabalho,
        ILogger<CadastrarPlanoServicoDeAplicacao> registradorDeLog)
    {
        ArgumentNullException.ThrowIfNull(unidadeDeTrabalho);
        ArgumentNullException.ThrowIfNull(registradorDeLog);

        _unidadeDeTrabalho = unidadeDeTrabalho;
        _registradorDeLog = registradorDeLog;
    }

    public async Task<Resultado<Guid>> ExecutarAsync(
        CadastrarPlanoEntrada entrada,
        CancellationToken tokenDeCancelamento = default)
    {
        if (entrada is null)
        {
            return Resultado<Guid>.Falha("Os dados de entrada do cadastro de plano são obrigatórios.");
        }

        _registradorDeLog.LogInformation(
            "Iniciando cadastro de plano com ciclo {CicloDeCobranca} e trial de {PeriodoDeTrialEmDias} dias.",
            entrada.CicloDeCobranca,
            entrada.PeriodoDeTrialEmDias);

        var resultadoDoPreco = Dinheiro.Criar(entrada.Valor, entrada.Moeda);
        var resultadoDoCicloDeCobranca = CicloDeCobranca.APartirDoTipo(entrada.CicloDeCobranca);

        var resultadoDosObjetosDeValor = Resultado.Combinar(resultadoDoPreco, resultadoDoCicloDeCobranca);

        if (resultadoDosObjetosDeValor.EhFalha)
        {
            _registradorDeLog.LogWarning(
                "Cadastro de plano rejeitado na montagem dos objetos de valor: {Erros}.",
                string.Join("; ", resultadoDosObjetosDeValor.Erros!));

            return Resultado<Guid>.FalhaAPartirDe(resultadoDosObjetosDeValor);
        }

        var identificadorDoPlano = Guid.NewGuid();

        var resultadoDoPlano = Plano.Criar(
            identificadorDoPlano,
            entrada.Nome,
            resultadoDoPreco.Instancia,
            resultadoDoCicloDeCobranca.Instancia,
            entrada.PeriodoDeTrialEmDias);

        if (resultadoDoPlano.EhFalha)
        {
            _registradorDeLog.LogWarning(
                "Cadastro de plano rejeitado pelas invariantes de domínio: {Erros}.",
                string.Join("; ", resultadoDoPlano.Erros!));

            return Resultado<Guid>.FalhaAPartirDe(resultadoDoPlano);
        }

        await _unidadeDeTrabalho.Planos.AdicionarAsync(resultadoDoPlano.Instancia, tokenDeCancelamento);
        await _unidadeDeTrabalho.SalvarAlteracoesAsync(tokenDeCancelamento);

        _registradorDeLog.LogInformation("Plano {IdentificadorDoPlano} cadastrado com sucesso.", identificadorDoPlano);

        return Resultado<Guid>.Sucesso(identificadorDoPlano);
    }
}
