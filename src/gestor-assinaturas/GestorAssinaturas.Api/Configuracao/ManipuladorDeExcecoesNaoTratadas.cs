using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace GestorAssinaturas.Api.Configuracao;

public sealed class ManipuladorDeExcecoesNaoTratadas : IExceptionHandler
{
    private readonly IProblemDetailsService _servicoDeDetalhesDoProblema;
    private readonly ILogger<ManipuladorDeExcecoesNaoTratadas> _registradorDeLog;

    public ManipuladorDeExcecoesNaoTratadas(
        IProblemDetailsService servicoDeDetalhesDoProblema,
        ILogger<ManipuladorDeExcecoesNaoTratadas> registradorDeLog)
    {
        ArgumentNullException.ThrowIfNull(servicoDeDetalhesDoProblema);
        ArgumentNullException.ThrowIfNull(registradorDeLog);

        _servicoDeDetalhesDoProblema = servicoDeDetalhesDoProblema;
        _registradorDeLog = registradorDeLog;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext contextoHttp,
        Exception excecao,
        CancellationToken tokenDeCancelamento)
    {
        _registradorDeLog.LogError(
            excecao,
            "Falha técnica não tratada ao processar {MetodoHttp} {CaminhoDaRequisicao}.",
            contextoHttp.Request.Method,
            contextoHttp.Request.Path);

        contextoHttp.Response.StatusCode = StatusCodes.Status500InternalServerError;

        return await _servicoDeDetalhesDoProblema.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = contextoHttp,
            Exception = excecao,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Ocorreu uma falha técnica ao processar a requisição.",
                Detail = "A falha foi registrada. Tente novamente em instantes."
            }
        });
    }
}
