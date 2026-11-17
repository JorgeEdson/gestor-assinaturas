using GestorAssinaturas.Aplicacao.Assinaturas;
using Microsoft.AspNetCore.Mvc;

namespace GestorAssinaturas.Api.Controladores;

[Route("api/faturas")]
public sealed class FaturasController : ControladorDaApi
{
    private readonly RegistrarPagamentoServicoDeAplicacao _registrarPagamento;

    public FaturasController(RegistrarPagamentoServicoDeAplicacao registrarPagamento)
    {
        ArgumentNullException.ThrowIfNull(registrarPagamento);

        _registrarPagamento = registrarPagamento;
    }

    [HttpPost("{identificador:guid}/pagamento")]
    public async Task<IActionResult> RegistrarPagamentoAsync(
        Guid identificador,
        CancellationToken tokenDeCancelamento)
    {
        var entrada = new RegistrarPagamentoEntrada(identificador);

        var resultado = await _registrarPagamento.ExecutarAsync(entrada, tokenDeCancelamento);

        if (resultado.EhFalha)
        {
            return ConverterFalha(resultado);
        }

        return Ok(new { situacao = resultado.Instancia });
    }
}
