using GestorAssinaturas.Dominio.Comum;
using Microsoft.AspNetCore.Mvc;

namespace GestorAssinaturas.Api.Controladores;

public sealed record RespostaDeErro(IEnumerable<string> Erros);

[ApiController]
public abstract class ControladorDaApi : ControllerBase
{
    protected IActionResult ConverterFalha(Resultado resultado)
    {
        var erros = resultado.Erros!.ToList();

        if (erros.Any(erro => erro.Contains("não encontrad", StringComparison.OrdinalIgnoreCase)))
        {
            return NotFound(new RespostaDeErro(erros));
        }

        return UnprocessableEntity(new RespostaDeErro(erros));
    }
}
