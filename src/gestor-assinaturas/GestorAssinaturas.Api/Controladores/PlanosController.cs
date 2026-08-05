using GestorAssinaturas.Aplicacao.Planos;
using GestorAssinaturas.Dominio.ObjetosDeValor;
using Microsoft.AspNetCore.Mvc;

namespace GestorAssinaturas.Api.Controladores;

public sealed record CadastrarPlanoRequisicao(
    string Nome,
    decimal Valor,
    string Moeda,
    TipoDeCicloDeCobranca CicloDeCobranca,
    int PeriodoDeTrialEmDias);

[Route("api/planos")]
public sealed class PlanosController : ControladorDaApi
{
    private readonly CadastrarPlanoApplicationService _cadastrarPlano;

    public PlanosController(CadastrarPlanoApplicationService cadastrarPlano)
    {
        ArgumentNullException.ThrowIfNull(cadastrarPlano);

        _cadastrarPlano = cadastrarPlano;
    }

    [HttpPost]
    public async Task<IActionResult> CadastrarAsync(
        [FromBody] CadastrarPlanoRequisicao requisicao,
        CancellationToken cancellationToken)
    {
        var entrada = new CadastrarPlanoEntrada(
            requisicao.Nome,
            requisicao.Valor,
            requisicao.Moeda,
            requisicao.CicloDeCobranca,
            requisicao.PeriodoDeTrialEmDias);

        var resultado = await _cadastrarPlano.ExecutarAsync(entrada, cancellationToken);

        if (resultado.EhFalha)
        {
            return ConverterFalha(resultado);
        }

        return Created(
            $"/api/planos/{resultado.Instancia}",
            new { identificador = resultado.Instancia });
    }
}
