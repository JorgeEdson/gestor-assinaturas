using GestorAssinaturas.Aplicacao.Clientes;
using Microsoft.AspNetCore.Mvc;

namespace GestorAssinaturas.Api.Controladores;

public sealed record CadastrarClienteRequisicao(
    string Nome,
    string Email);

[Route("api/clientes")]
public sealed class ClientesController : ControladorDaApi
{
    private readonly CadastrarClienteApplicationService _cadastrarCliente;

    public ClientesController(CadastrarClienteApplicationService cadastrarCliente)
    {
        ArgumentNullException.ThrowIfNull(cadastrarCliente);

        _cadastrarCliente = cadastrarCliente;
    }

    [HttpPost]
    public async Task<IActionResult> CadastrarAsync(
        [FromBody] CadastrarClienteRequisicao requisicao,
        CancellationToken cancellationToken)
    {
        var entrada = new CadastrarClienteEntrada(requisicao.Nome, requisicao.Email);

        var resultado = await _cadastrarCliente.ExecutarAsync(entrada, cancellationToken);

        if (resultado.EhFalha)
        {
            return ConverterFalha(resultado);
        }

        return Created(
            $"/api/clientes/{resultado.Instancia}",
            new { identificador = resultado.Instancia });
    }
}
