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
        Assert.Equal(TipoDeFalha.Nenhuma, resultado.TipoDeFalha);
    }

    [Fact]
    public void DeveCriarResultadoDeFalhaComUmaMensagemComoViolacaoDeRegraDeNegocio()
    {
        var resultado = Resultado.Falha("Mensagem de erro.");

        Assert.True(resultado.EhFalha);
        Assert.Equal(new[] { "Mensagem de erro." }, resultado.Erros);
        Assert.Equal(TipoDeFalha.ViolacaoDeRegraDeNegocio, resultado.TipoDeFalha);
        Assert.False(resultado.EhRecursoNaoEncontrado);
    }

    [Fact]
    public void DeveCriarResultadoDeRecursoNaoEncontrado()
    {
        var resultado = Resultado.NaoEncontrado("Assinatura não encontrada.");

        Assert.True(resultado.EhFalha);
        Assert.True(resultado.EhRecursoNaoEncontrado);
        Assert.Equal(TipoDeFalha.RecursoNaoEncontrado, resultado.TipoDeFalha);
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
    public void DeveImpedirAcessoAInstanciaDeUmResultadoComFalha()
    {
        var resultado = Resultado<string>.Falha("Mensagem de erro.");

        var excecao = Assert.Throws<InvalidOperationException>(() => resultado.Instancia);

        Assert.Contains("resultado com falha", excecao.Message);
    }

    [Fact]
    public void DevePropagarAFalhaPreservandoOsErrosEOTipoDeFalha()
    {
        var resultadoDeOrigem = Resultado<string>.NaoEncontrado("Plano não encontrado.");

        var resultadoPropagado = Resultado<Guid>.FalhaAPartirDe(resultadoDeOrigem);

        Assert.True(resultadoPropagado.EhFalha);
        Assert.True(resultadoPropagado.EhRecursoNaoEncontrado);
        Assert.Equal(resultadoDeOrigem.Erros, resultadoPropagado.Erros);
    }

    [Fact]
    public void DevePropagarAFalhaDeUmResultadoTipadoParaUmResultadoSemInstancia()
    {
        var resultadoDeOrigem = Resultado<int>.Falha("Mensagem de erro.");

        var resultadoPropagado = Resultado.FalhaAPartirDe(resultadoDeOrigem);

        Assert.True(resultadoPropagado.EhFalha);
        Assert.Equal(TipoDeFalha.ViolacaoDeRegraDeNegocio, resultadoPropagado.TipoDeFalha);
    }

    [Fact]
    public void DeveImpedirAPropagacaoDeUmResultadoDeSucesso()
    {
        var resultadoDeSucesso = Resultado.Sucesso();

        Assert.Throws<InvalidOperationException>(() => Resultado<Guid>.FalhaAPartirDe(resultadoDeSucesso));
    }
}
