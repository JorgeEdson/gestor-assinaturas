using GestorAssinaturas.Aplicacao.Assinaturas;
using Microsoft.AspNetCore.Mvc;

namespace GestorAssinaturas.Api.Controladores;

public sealed record CriarAssinaturaRequisicao(
    Guid IdentificadorDoCliente,
    Guid IdentificadorDoPlano);

public sealed record TrocarPlanoRequisicao(
    Guid IdentificadorDoNovoPlano);

public sealed record CancelarAssinaturaRequisicao(
    ModalidadeDeCancelamento Modalidade);

[Route("api/assinaturas")]
public sealed class AssinaturasController : ControladorDaApi
{
    private readonly CriarAssinaturaApplicationService _criarAssinatura;
    private readonly AtivarAssinaturaApplicationService _ativarAssinatura;
    private readonly TrocarPlanoApplicationService _trocarPlano;
    private readonly CancelarAssinaturaApplicationService _cancelarAssinatura;

    public AssinaturasController(
        CriarAssinaturaApplicationService criarAssinatura,
        AtivarAssinaturaApplicationService ativarAssinatura,
        TrocarPlanoApplicationService trocarPlano,
        CancelarAssinaturaApplicationService cancelarAssinatura)
    {
        ArgumentNullException.ThrowIfNull(criarAssinatura);
        ArgumentNullException.ThrowIfNull(ativarAssinatura);
        ArgumentNullException.ThrowIfNull(trocarPlano);
        ArgumentNullException.ThrowIfNull(cancelarAssinatura);

        _criarAssinatura = criarAssinatura;
        _ativarAssinatura = ativarAssinatura;
        _trocarPlano = trocarPlano;
        _cancelarAssinatura = cancelarAssinatura;
    }

    [HttpPost]
    public async Task<IActionResult> CriarAsync(
        [FromBody] CriarAssinaturaRequisicao requisicao,
        CancellationToken cancellationToken)
    {
        var entrada = new CriarAssinaturaEntrada(
            requisicao.IdentificadorDoCliente,
            requisicao.IdentificadorDoPlano);

        var resultado = await _criarAssinatura.ExecutarAsync(entrada, cancellationToken);

        if (resultado.EhFalha)
        {
            return ConverterFalha(resultado);
        }

        return Created(
            $"/api/assinaturas/{resultado.Instancia}",
            new { identificador = resultado.Instancia });
    }

    [HttpPost("{identificador:guid}/ativacao")]
    public async Task<IActionResult> AtivarAsync(
        Guid identificador,
        CancellationToken cancellationToken)
    {
        var entrada = new AtivarAssinaturaEntrada(identificador);

        var resultado = await _ativarAssinatura.ExecutarAsync(entrada, cancellationToken);

        if (resultado.EhFalha)
        {
            return ConverterFalha(resultado);
        }

        return Ok(new { identificadorDaPrimeiraFatura = resultado.Instancia });
    }

    [HttpPost("{identificador:guid}/troca-de-plano")]
    public async Task<IActionResult> TrocarPlanoAsync(
        Guid identificador,
        [FromBody] TrocarPlanoRequisicao requisicao,
        CancellationToken cancellationToken)
    {
        var entrada = new TrocarPlanoEntrada(identificador, requisicao.IdentificadorDoNovoPlano);

        var resultado = await _trocarPlano.ExecutarAsync(entrada, cancellationToken);

        if (resultado.EhFalha)
        {
            return ConverterFalha(resultado);
        }

        return NoContent();
    }

    [HttpPost("{identificador:guid}/cancelamento")]
    public async Task<IActionResult> CancelarAsync(
        Guid identificador,
        [FromBody] CancelarAssinaturaRequisicao requisicao,
        CancellationToken cancellationToken)
    {
        var entrada = new CancelarAssinaturaEntrada(identificador, requisicao.Modalidade);

        var resultado = await _cancelarAssinatura.ExecutarAsync(entrada, cancellationToken);

        if (resultado.EhFalha)
        {
            return ConverterFalha(resultado);
        }

        return NoContent();
    }
}
