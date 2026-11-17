using GestorAssinaturas.Dominio.Comum;
using Microsoft.AspNetCore.Mvc;

namespace GestorAssinaturas.Api.Controladores;

public sealed record RespostaDeErro(IEnumerable<string> Erros);

[ApiController]
public abstract class ControladorDaApi : ControllerBase
{
    protected IActionResult ConverterFalha(Resultado resultado)
    {
        var respostaDeErro = new RespostaDeErro(resultado.Erros!.ToList());

        return resultado.TipoDeFalha switch
        {
            TipoDeFalha.RecursoNaoEncontrado => NotFound(respostaDeErro),
            _ => UnprocessableEntity(respostaDeErro)
        };
    }
}
