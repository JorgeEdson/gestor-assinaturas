using GestorAssinaturas.Aplicacao.Portas.Persistencia;
using GestorAssinaturas.Dominio.Comum;
using GestorAssinaturas.Dominio.ObjetosDeValor;
using GestorAssinaturas.Dominio.Planos;
using Microsoft.Extensions.Logging;

namespace GestorAssinaturas.Aplicacao.Planos;

public sealed record CadastrarPlanoEntrada(
    string Nome,
    decimal Valor,
    string Moeda,
    TipoDeCicloDeCobranca CicloDeCobranca,
    int PeriodoDeTrialEmDias);

public sealed class CadastrarPlanoApplicationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CadastrarPlanoApplicationService> _logger;

    public CadastrarPlanoApplicationService(
        IUnitOfWork unitOfWork,
        ILogger<CadastrarPlanoApplicationService> logger)
    {
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(logger);

        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Resultado<Guid>> ExecutarAsync(
        CadastrarPlanoEntrada comando,
        CancellationToken cancellationToken = default)
    {
        if (comando is null)
        {
            return Resultado<Guid>.Falha("O comando de cadastro de plano é obrigatório.");
        }

        _logger.LogInformation(
            "Iniciando cadastro de plano {NomeDoPlano} com ciclo {CicloDeCobranca}.",
            comando.Nome,
            comando.CicloDeCobranca);

        var resultadoDoPreco = Dinheiro.Criar(comando.Valor, comando.Moeda);
        var resultadoDoCicloDeCobranca = CicloDeCobranca.APartirDoTipo(comando.CicloDeCobranca);

        var resultadoDosObjetosDeValor = Resultado.Combinar(resultadoDoPreco, resultadoDoCicloDeCobranca);

        if (resultadoDosObjetosDeValor.EhFalha)
        {
            _logger.LogWarning(
                "Cadastro de plano rejeitado na montagem dos objetos de valor: {Erros}.",
                string.Join("; ", resultadoDosObjetosDeValor.Erros!));

            return Resultado<Guid>.Falha(resultadoDosObjetosDeValor.Erros!);
        }

        var identificadorDoPlano = Guid.NewGuid();

        var resultadoDoPlano = Plano.Criar(
            identificadorDoPlano,
            comando.Nome,
            resultadoDoPreco.Instancia,
            resultadoDoCicloDeCobranca.Instancia,
            comando.PeriodoDeTrialEmDias);

        if (resultadoDoPlano.EhFalha)
        {
            _logger.LogWarning(
                "Cadastro de plano rejeitado pelas invariantes de domínio: {Erros}.",
                string.Join("; ", resultadoDoPlano.Erros!));

            return Resultado<Guid>.Falha(resultadoDoPlano.Erros!);
        }

        await _unitOfWork.Planos.AdicionarAsync(resultadoDoPlano.Instancia, cancellationToken);

        var resultadoDoSalvamento = await _unitOfWork.SalvarAlteracoesAsync(cancellationToken);

        if (resultadoDoSalvamento.EhFalha)
        {
            _logger.LogWarning(
                "Falha ao persistir o cadastro do plano {IdentificadorDoPlano}: {Erros}.",
                identificadorDoPlano,
                string.Join("; ", resultadoDoSalvamento.Erros!));

            return Resultado<Guid>.Falha(resultadoDoSalvamento.Erros!);
        }

        _logger.LogInformation("Plano {IdentificadorDoPlano} cadastrado com sucesso.", identificadorDoPlano);

        return Resultado<Guid>.Sucesso(identificadorDoPlano);
    }
}
