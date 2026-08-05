using GestorAssinaturas.Aplicacao.Portas.Persistencia;
using GestorAssinaturas.Dominio.Clientes;
using GestorAssinaturas.Dominio.Comum;
using GestorAssinaturas.Dominio.ObjetosDeValor;
using Microsoft.Extensions.Logging;

namespace GestorAssinaturas.Aplicacao.Clientes;

public sealed record CadastrarClienteEntrada(
    string Nome,
    string Email);

public sealed class CadastrarClienteApplicationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CadastrarClienteApplicationService> _logger;

    public CadastrarClienteApplicationService(
        IUnitOfWork unitOfWork,
        ILogger<CadastrarClienteApplicationService> logger)
    {
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(logger);

        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Resultado<Guid>> ExecutarAsync(
        CadastrarClienteEntrada comando,
        CancellationToken cancellationToken = default)
    {
        if (comando is null)
        {
            return Resultado<Guid>.Falha("O comando de cadastro de cliente é obrigatório.");
        }

        _logger.LogInformation("Iniciando cadastro de cliente {NomeDoCliente}.", comando.Nome);

        var resultadoDoEmail = Email.Criar(comando.Email);

        if (resultadoDoEmail.EhFalha)
        {
            _logger.LogWarning(
                "Cadastro de cliente rejeitado na validação do e-mail de contato: {Erros}.",
                string.Join("; ", resultadoDoEmail.Erros!));

            return Resultado<Guid>.Falha(resultadoDoEmail.Erros!);
        }

        var identificadorDoCliente = Guid.NewGuid();

        var resultadoDoCliente = Cliente.Criar(identificadorDoCliente, comando.Nome, resultadoDoEmail.Instancia);

        if (resultadoDoCliente.EhFalha)
        {
            _logger.LogWarning(
                "Cadastro de cliente rejeitado pelas invariantes de domínio: {Erros}.",
                string.Join("; ", resultadoDoCliente.Erros!));

            return Resultado<Guid>.Falha(resultadoDoCliente.Erros!);
        }

        await _unitOfWork.Clientes.AdicionarAsync(resultadoDoCliente.Instancia, cancellationToken);

        var resultadoDoSalvamento = await _unitOfWork.SalvarAlteracoesAsync(cancellationToken);

        if (resultadoDoSalvamento.EhFalha)
        {
            _logger.LogWarning(
                "Falha ao persistir o cadastro do cliente {IdentificadorDoCliente}: {Erros}.",
                identificadorDoCliente,
                string.Join("; ", resultadoDoSalvamento.Erros!));

            return Resultado<Guid>.Falha(resultadoDoSalvamento.Erros!);
        }

        _logger.LogInformation("Cliente {IdentificadorDoCliente} cadastrado com sucesso.", identificadorDoCliente);

        return Resultado<Guid>.Sucesso(identificadorDoCliente);
    }
}
