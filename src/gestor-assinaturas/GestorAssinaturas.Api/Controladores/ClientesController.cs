using GestorAssinaturas.Aplicacao.Clientes;
using Microsoft.AspNetCore.Mvc;

namespace GestorAssinaturas.Api.Controladores;

public sealed record CadastrarClienteRequisicao(
    string Nome,
    string Email);

[Route("api/clientes")]
public sealed class ClientesController : ControladorDaApi
{
    private readonly CadastrarClienteServicoDeAplicacao _cadastrarCliente;

    public ClientesController(CadastrarClienteServicoDeAplicacao cadastrarCliente)
    {
        ArgumentNullException.ThrowIfNull(cadastrarCliente);

        _cadastrarCliente = cadastrarCliente;
    }

    [HttpPost]
    public async Task<IActionResult> CadastrarAsync(
        [FromBody] CadastrarClienteRequisicao requisicao,
        CancellationToken tokenDeCancelamento)
    {
        var entrada = new CadastrarClienteEntrada(requisicao.Nome, requisicao.Email);

        var resultado = await _cadastrarCliente.ExecutarAsync(entrada, tokenDeCancelamento);

        if (resultado.EhFalha)
        {
            return ConverterFalha(resultado);
        }

        return Created(
            $"/api/clientes/{resultado.Instancia}",
            new { identificador = resultado.Instancia });
    }
}
