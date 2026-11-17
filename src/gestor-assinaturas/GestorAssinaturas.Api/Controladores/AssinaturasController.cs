using GestorAssinaturas.Aplicacao.Assinaturas;
using GestorAssinaturas.Dominio.Assinaturas;
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
    private readonly CriarAssinaturaServicoDeAplicacao _criarAssinatura;
    private readonly AtivarAssinaturaServicoDeAplicacao _ativarAssinatura;
    private readonly TrocarPlanoServicoDeAplicacao _trocarPlano;
    private readonly CancelarAssinaturaServicoDeAplicacao _cancelarAssinatura;
    private readonly RecobrarAssinaturaInadimplenteServicoDeAplicacao _recobrarAssinaturaInadimplente;

    public AssinaturasController(
        CriarAssinaturaServicoDeAplicacao criarAssinatura,
        AtivarAssinaturaServicoDeAplicacao ativarAssinatura,
        TrocarPlanoServicoDeAplicacao trocarPlano,
        CancelarAssinaturaServicoDeAplicacao cancelarAssinatura,
        RecobrarAssinaturaInadimplenteServicoDeAplicacao recobrarAssinaturaInadimplente)
    {
        ArgumentNullException.ThrowIfNull(criarAssinatura);
        ArgumentNullException.ThrowIfNull(ativarAssinatura);
        ArgumentNullException.ThrowIfNull(trocarPlano);
        ArgumentNullException.ThrowIfNull(cancelarAssinatura);
        ArgumentNullException.ThrowIfNull(recobrarAssinaturaInadimplente);

        _criarAssinatura = criarAssinatura;
        _ativarAssinatura = ativarAssinatura;
        _trocarPlano = trocarPlano;
        _cancelarAssinatura = cancelarAssinatura;
        _recobrarAssinaturaInadimplente = recobrarAssinaturaInadimplente;
    }

    [HttpPost]
    public async Task<IActionResult> CriarAsync(
        [FromBody] CriarAssinaturaRequisicao requisicao,
        CancellationToken tokenDeCancelamento)
    {
        var entrada = new CriarAssinaturaEntrada(
            requisicao.IdentificadorDoCliente,
            requisicao.IdentificadorDoPlano);

        var resultado = await _criarAssinatura.ExecutarAsync(entrada, tokenDeCancelamento);

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
        CancellationToken tokenDeCancelamento)
    {
        var entrada = new AtivarAssinaturaEntrada(identificador);

        var resultado = await _ativarAssinatura.ExecutarAsync(entrada, tokenDeCancelamento);

        if (resultado.EhFalha)
        {
            return ConverterFalha(resultado);
        }

        return Ok(new { identificadorDaPrimeiraFatura = resultado.Instancia });
    }

    [HttpPost("{identificador:guid}/recobranca")]
    public async Task<IActionResult> RecobrarAsync(
        Guid identificador,
        CancellationToken tokenDeCancelamento)
    {
        var entrada = new RecobrarAssinaturaInadimplenteEntrada(identificador);

        var resultado = await _recobrarAssinaturaInadimplente.ExecutarAsync(entrada, tokenDeCancelamento);

        if (resultado.EhFalha)
        {
            return ConverterFalha(resultado);
        }

        return Ok(new { identificadorDaFaturaDeRecobranca = resultado.Instancia });
    }

    [HttpPost("{identificador:guid}/troca-de-plano")]
    public async Task<IActionResult> TrocarPlanoAsync(
        Guid identificador,
        [FromBody] TrocarPlanoRequisicao requisicao,
        CancellationToken tokenDeCancelamento)
    {
        var entrada = new TrocarPlanoEntrada(identificador, requisicao.IdentificadorDoNovoPlano);

        var resultado = await _trocarPlano.ExecutarAsync(entrada, tokenDeCancelamento);

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
        CancellationToken tokenDeCancelamento)
    {
        var entrada = new CancelarAssinaturaEntrada(identificador, requisicao.Modalidade);

        var resultado = await _cancelarAssinatura.ExecutarAsync(entrada, tokenDeCancelamento);

        if (resultado.EhFalha)
        {
            return ConverterFalha(resultado);
        }

        return NoContent();
    }
}
