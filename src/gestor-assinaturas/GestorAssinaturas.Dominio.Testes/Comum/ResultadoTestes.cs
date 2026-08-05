using GestorAssinaturas.Dominio.Comum;
using Xunit;

namespace GestorAssinaturas.Dominio.Testes.Comum;

public class ResultadoTestes
{
    [Fact]
    public void DeveCriarResultadoDeSucessoSemErros()
    {
        var resultado = Resultado.Sucesso();

        Assert.True(resultado.EhSucesso);
        Assert.False(resultado.EhFalha);
        Assert.Null(resultado.Erros);
    }

    [Fact]
    public void DeveCriarResultadoDeFalhaComUmaMensagem()
    {
        var resultado = Resultado.Falha("Mensagem de erro.");

        Assert.True(resultado.EhFalha);
        Assert.Equal(new[] { "Mensagem de erro." }, resultado.Erros);
    }

    [Fact]
    public void DeveRetornarSucessoQuandoACondicaoDeViolacaoEhFalsa()
    {
        var resultado = Resultado.FalhaQuando(false, "Mensagem de erro.");

        Assert.True(resultado.EhSucesso);
    }

    [Fact]
    public void DeveRetornarFalhaQuandoACondicaoDeViolacaoEhVerdadeira()
    {
        var resultado = Resultado.FalhaQuando(true, "Mensagem de erro.");

        Assert.True(resultado.EhFalha);
    }

    [Fact]
    public void DeveCombinarResultadosAcumulandoTodosOsErros()
    {
        var resultado = Resultado.Combinar(
            Resultado.Sucesso(),
            Resultado.Falha("Primeiro erro."),
            Resultado.Falha("Segundo erro."));

        Assert.True(resultado.EhFalha);
        Assert.Equal(2, resultado.Erros!.Count());
    }

    [Fact]
    public void DeveCombinarResultadosDeSucessoRetornandoSucesso()
    {
        var resultado = Resultado.Combinar(Resultado.Sucesso(), Resultado.Sucesso());

        Assert.True(resultado.EhSucesso);
    }

    [Fact]
    public void DeveExporAInstanciaDeUmResultadoDeSucesso()
    {
        var resultado = Resultado<string>.Sucesso("valor");

        Assert.True(resultado.EhSucesso);
        Assert.Equal("valor", resultado.Instancia);
    }

    [Fact]
    public void DeveAceitarSucessoComInstanciaNulaComoContratoDeNaoEncontrado()
    {
        var resultado = Resultado<string>.Sucesso();

        Assert.True(resultado.EhSucesso);
        Assert.Null(resultado.Instancia);
    }

    [Fact]
    public void DeveImpedirAcessoAInstanciaDeUmResultadoComFalha()
    {
        var resultado = Resultado<string>.Falha("Mensagem de erro.");

        var excecao = Assert.Throws<InvalidOperationException>(() => resultado.Instancia);

        Assert.Contains("resultado com falha", excecao.Message);
    }

    [Fact]
    public void DeveConverterUmResultadoComFalhaPreservandoOsErros()
    {
        var resultado = Resultado<string>.Falha("Mensagem de erro.");

        var resultadoConvertido = resultado.ComFalha();

        Assert.True(resultadoConvertido.EhFalha);
        Assert.Equal(resultado.Erros, resultadoConvertido.Erros);
    }

    [Fact]
    public void DeveCapturarExcecaoAoTentarExecutarUmaFuncao()
    {
        var resultado = Resultado<string>.Tentar(() => throw new InvalidOperationException("Falha inesperada."));

        Assert.True(resultado.EhFalha);
        Assert.Contains(resultado.Erros!, erro => erro.Contains("Falha inesperada."));
    }

    [Fact]
    public void DeveRetornarSucessoAoTentarExecutarUmaFuncaoSemErro()
    {
        var resultado = Resultado<string>.Tentar(() => "valor");

        Assert.True(resultado.EhSucesso);
        Assert.Equal("valor", resultado.Instancia);
    }

    [Fact]
    public async Task DeveCapturarExcecaoAoTentarExecutarUmaFuncaoAssincrona()
    {
        var resultado = await Resultado<string>.TentarAsync(() => throw new InvalidOperationException("Falha assíncrona."));

        Assert.True(resultado.EhFalha);
        Assert.Contains(resultado.Erros!, erro => erro.Contains("Falha assíncrona."));
    }

    [Fact]
    public void DeveCombinarResultadosTipadosRetornandoTodasAsInstancias()
    {
        var resultado = Resultado<int>.Combinar(
            Resultado<int>.Sucesso(1),
            Resultado<int>.Sucesso(2));

        Assert.True(resultado.EhSucesso);
        Assert.Equal(new[] { 1, 2 }, resultado.Instancia);
    }

    [Fact]
    public void DeveCombinarResultadosTipadosAcumulandoOsErros()
    {
        var resultado = Resultado<int>.Combinar(
            Resultado<int>.Sucesso(1),
            Resultado<int>.Falha("Primeiro erro."),
            Resultado<int>.Falha("Segundo erro."));

        Assert.True(resultado.EhFalha);
        Assert.Equal(2, resultado.Erros!.Count());
    }

    [Fact]
    public async Task DeveCombinarResultadosTipadosDeFormaAssincrona()
    {
        var resultado = await Resultado<int>.CombinarAsync(
            Task.FromResult(Resultado<int>.Sucesso(1)),
            Task.FromResult(Resultado<int>.Sucesso(2)));

        Assert.True(resultado.EhSucesso);
        Assert.Equal(new[] { 1, 2 }, resultado.Instancia);
    }

    [Fact]
    public void DeveCalcularTotalDePaginasDoResultadoPaginado()
    {
        var resultadoPaginado = new ResultadoPaginado<string>(
            new[] { "primeiro", "segundo" },
            NumeroPagina: 1,
            TamanhoPagina: 10,
            TotalRegistros: 25);

        Assert.Equal(3, resultadoPaginado.TotalPaginas);
    }

    [Fact]
    public void DeveRetornarZeroPaginasQuandoNaoHaRegistros()
    {
        var resultadoPaginado = new ResultadoPaginado<string>(
            Array.Empty<string>(),
            NumeroPagina: 1,
            TamanhoPagina: 10,
            TotalRegistros: 0);

        Assert.Equal(0, resultadoPaginado.TotalPaginas);
    }
}
