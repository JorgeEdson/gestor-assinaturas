using GestorAssinaturas.Dominio.ObjetosDeValor;
using Xunit;

namespace GestorAssinaturas.Dominio.Testes.ObjetosDeValor;

public class DinheiroTestes
{
    [Fact]
    public void DeveCriarValorMonetarioNormalizandoAMoedaParaLetrasMaiusculas()
    {
        var resultado = Dinheiro.Criar(99.90m, "brl");

        Assert.True(resultado.EhSucesso);
        Assert.Equal("BRL", resultado.Instancia.Moeda);
        Assert.Equal(99.90m, resultado.Instancia.Valor);
    }

    [Fact]
    public void DeveArredondarOValorMonetarioParaDuasCasasDecimais()
    {
        var resultado = Dinheiro.Criar(10.005m, "BRL");

        Assert.True(resultado.EhSucesso);
        Assert.Equal(10.01m, resultado.Instancia.Valor);
    }

    [Fact]
    public void DeveRetornarFalhaParaValorMonetarioNegativo()
    {
        var resultado = Dinheiro.Criar(-1m, "BRL");

        Assert.True(resultado.EhFalha);
        Assert.Contains(resultado.Erros!, erro => erro.Contains("não pode ser negativo"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("BR")]
    [InlineData("BRLL")]
    [InlineData("B1L")]
    public void DeveRetornarFalhaParaMoedaInvalida(string moedaInvalida)
    {
        var resultado = Dinheiro.Criar(10m, moedaInvalida);

        Assert.True(resultado.EhFalha);
    }

    [Fact]
    public void DeveAcumularTodosOsErrosDeCriacaoEmUmUnicoResultado()
    {
        var resultado = Dinheiro.Criar(-10m, "XX");

        Assert.True(resultado.EhFalha);
        Assert.Equal(2, resultado.Erros!.Count());
    }

    [Fact]
    public void DeveImpedirAcessoAInstanciaDeUmResultadoComFalha()
    {
        var resultado = Dinheiro.Criar(-10m, "BRL");

        Assert.Throws<InvalidOperationException>(() => resultado.Instancia);
    }

    [Fact]
    public void DeveSomarValoresMonetariosDaMesmaMoeda()
    {
        var primeiroValorMonetario = Dinheiro.Criar(100m, "BRL").Instancia;
        var segundoValorMonetario = Dinheiro.Criar(49.90m, "BRL").Instancia;

        var resultado = primeiroValorMonetario.Somar(segundoValorMonetario);

        Assert.True(resultado.EhSucesso);
        Assert.Equal(Dinheiro.Criar(149.90m, "BRL").Instancia, resultado.Instancia);
    }

    [Fact]
    public void DeveRetornarFalhaAoOperarValoresMonetariosDeMoedasDiferentes()
    {
        var valorMonetarioEmReais = Dinheiro.Criar(100m, "BRL").Instancia;
        var valorMonetarioEmDolares = Dinheiro.Criar(100m, "USD").Instancia;

        var resultado = valorMonetarioEmReais.Somar(valorMonetarioEmDolares);

        Assert.True(resultado.EhFalha);
        Assert.Contains(resultado.Erros!, erro => erro.Contains("moedas diferentes"));
    }

    [Fact]
    public void DeveRetornarFalhaQuandoASubtracaoResultaEmValorNegativo()
    {
        var primeiroValorMonetario = Dinheiro.Criar(50m, "BRL").Instancia;
        var segundoValorMonetario = Dinheiro.Criar(80m, "BRL").Instancia;

        var resultado = primeiroValorMonetario.Subtrair(segundoValorMonetario);

        Assert.True(resultado.EhFalha);
    }

    [Fact]
    public void DeveMultiplicarValorMonetarioPorQuantidadeInformada()
    {
        var valorMonetario = Dinheiro.Criar(19.90m, "BRL").Instancia;

        var resultado = valorMonetario.MultiplicarPor(12m);

        Assert.True(resultado.EhSucesso);
        Assert.Equal(Dinheiro.Criar(238.80m, "BRL").Instancia, resultado.Instancia);
    }

    [Fact]
    public void DeveRetornarFalhaParaMultiplicadorNegativo()
    {
        var valorMonetario = Dinheiro.Criar(19.90m, "BRL").Instancia;

        var resultado = valorMonetario.MultiplicarPor(-1m);

        Assert.True(resultado.EhFalha);
    }

    [Fact]
    public void DeveConsiderarIguaisDoisValoresMonetariosComMesmoValorEMesmaMoeda()
    {
        var primeiroValorMonetario = Dinheiro.Criar(10m, "BRL").Instancia;
        var segundoValorMonetario = Dinheiro.Criar(10m, "BRL").Instancia;

        Assert.Equal(primeiroValorMonetario, segundoValorMonetario);
        Assert.True(primeiroValorMonetario == segundoValorMonetario);
    }

    [Fact]
    public void DeveCompararValoresMonetariosDaMesmaMoeda()
    {
        var valorMonetarioMaior = Dinheiro.Criar(100m, "BRL").Instancia;
        var valorMonetarioMenor = Dinheiro.Criar(10m, "BRL").Instancia;

        var resultadoDaComparacaoMaior = valorMonetarioMaior.EhMaiorQue(valorMonetarioMenor);
        var resultadoDaComparacaoMenor = valorMonetarioMenor.EhMenorQue(valorMonetarioMaior);

        Assert.True(resultadoDaComparacaoMaior.EhSucesso);
        Assert.True(resultadoDaComparacaoMaior.Instancia);
        Assert.True(resultadoDaComparacaoMenor.EhSucesso);
        Assert.True(resultadoDaComparacaoMenor.Instancia);
    }

    [Fact]
    public void DeveIdentificarValorMonetarioZerado()
    {
        var resultado = Dinheiro.Zero("BRL");

        Assert.True(resultado.EhSucesso);
        Assert.True(resultado.Instancia.EhZero());
    }
}
