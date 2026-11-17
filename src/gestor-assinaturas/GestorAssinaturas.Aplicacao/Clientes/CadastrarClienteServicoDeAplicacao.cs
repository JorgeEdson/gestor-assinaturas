using GestorAssinaturas.Dominio.Clientes;
using GestorAssinaturas.Dominio.Comum;
using GestorAssinaturas.Dominio.ObjetosDeValor;
using GestorAssinaturas.Dominio.Repositorios;
using Microsoft.Extensions.Logging;

namespace GestorAssinaturas.Aplicacao.Clientes;

public sealed record CadastrarClienteEntrada(
    string Nome,
    string Email);

public sealed class CadastrarClienteServicoDeAplicacao
{
    private readonly IUnidadeDeTrabalho _unidadeDeTrabalho;
    private readonly ILogger<CadastrarClienteServicoDeAplicacao> _registradorDeLog;

    public CadastrarClienteServicoDeAplicacao(
        IUnidadeDeTrabalho unidadeDeTrabalho,
        ILogger<CadastrarClienteServicoDeAplicacao> registradorDeLog)
    {
        ArgumentNullException.ThrowIfNull(unidadeDeTrabalho);
        ArgumentNullException.ThrowIfNull(registradorDeLog);

        _unidadeDeTrabalho = unidadeDeTrabalho;
        _registradorDeLog = registradorDeLog;
    }

    public async Task<Resultado<Guid>> ExecutarAsync(
        CadastrarClienteEntrada entrada,
        CancellationToken tokenDeCancelamento = default)
    {
        if (entrada is null)
        {
            return Resultado<Guid>.Falha("Os dados de entrada do cadastro de cliente são obrigatórios.");
        }

        _registradorDeLog.LogInformation("Iniciando cadastro de cliente.");

        var resultadoDoEmail = Email.Criar(entrada.Email);

        if (resultadoDoEmail.EhFalha)
        {
            _registradorDeLog.LogWarning(
                "Cadastro de cliente rejeitado na validação do e-mail de contato: {Erros}.",
                string.Join("; ", resultadoDoEmail.Erros!));

            return Resultado<Guid>.FalhaAPartirDe(resultadoDoEmail);
        }

        var identificadorDoCliente = Guid.NewGuid();

        var resultadoDoCliente = Cliente.Criar(identificadorDoCliente, entrada.Nome, resultadoDoEmail.Instancia);

        if (resultadoDoCliente.EhFalha)
        {
            _registradorDeLog.LogWarning(
                "Cadastro de cliente rejeitado pelas invariantes de domínio: {Erros}.",
                string.Join("; ", resultadoDoCliente.Erros!));

            return Resultado<Guid>.FalhaAPartirDe(resultadoDoCliente);
        }

        await _unidadeDeTrabalho.Clientes.AdicionarAsync(resultadoDoCliente.Instancia, tokenDeCancelamento);
        await _unidadeDeTrabalho.SalvarAlteracoesAsync(tokenDeCancelamento);

        _registradorDeLog.LogInformation("Cliente {IdentificadorDoCliente} cadastrado com sucesso.", identificadorDoCliente);

        return Resultado<Guid>.Sucesso(identificadorDoCliente);
    }
}
