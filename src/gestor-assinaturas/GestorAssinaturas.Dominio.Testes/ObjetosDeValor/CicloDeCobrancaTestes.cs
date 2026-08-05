using GestorAssinaturas.Dominio.ObjetosDeValor;
using Xunit;

namespace GestorAssinaturas.Dominio.Testes.ObjetosDeValor;

public class CicloDeCobrancaTestes
{
    [Fact]
    public void DeveExporQuantidadeDeMesesDoCicloMensal()
    {
        Assert.Equal(1, CicloDeCobranca.Mensal.QuantidadeDeMeses);
    }

    [Fact]
    public void DeveExporQuantidadeDeMesesDoCicloAnual()
    {
        Assert.Equal(12, CicloDeCobranca.Anual.QuantidadeDeMeses);
    }

    [Fact]
    public void DeveCalcularProximaDataDeVencimentoParaCicloMensal()
    {
        var dataDeReferencia = new DateOnly(2026, 1, 31);

        var proximaDataDeVencimento = CicloDeCobranca.Mensal.CalcularProximaDataDeVencimento(dataDeReferencia);

        Assert.Equal(new DateOnly(2026, 2, 28), proximaDataDeVencimento);
    }

    [Fact]
    public void DeveCalcularProximaDataDeVencimentoParaCicloAnual()
    {
        var dataDeReferencia = new DateOnly(2026, 3, 10);

        var proximaDataDeVencimento = CicloDeCobranca.Anual.CalcularProximaDataDeVencimento(dataDeReferencia);

        Assert.Equal(new DateOnly(2027, 3, 10), proximaDataDeVencimento);
    }

    [Theory]
    [InlineData(TipoDeCicloDeCobranca.Mensal)]
    [InlineData(TipoDeCicloDeCobranca.Anual)]
    public void DeveObterCicloDeCobrancaAPartirDoTipo(TipoDeCicloDeCobranca tipo)
    {
        var resultado = CicloDeCobranca.APartirDoTipo(tipo);

        Assert.True(resultado.EhSucesso);
        Assert.Equal(tipo, resultado.Instancia.Tipo);
    }

    [Fact]
    public void DeveRetornarFalhaParaTipoDeCicloDeCobrancaNaoSuportado()
    {
        var tipoNaoSuportado = (TipoDeCicloDeCobranca)99;

        var resultado = CicloDeCobranca.APartirDoTipo(tipoNaoSuportado);

        Assert.True(resultado.EhFalha);
        Assert.Contains(resultado.Erros!, erro => erro.Contains("não suportado"));
    }

    [Fact]
    public void DeveConsiderarIguaisDoisCiclosDeCobrancaDoMesmoTipo()
    {
        var primeiroCicloDeCobranca = CicloDeCobranca.APartirDoTipo(TipoDeCicloDeCobranca.Mensal).Instancia;
        var segundoCicloDeCobranca = CicloDeCobranca.Mensal;

        Assert.Equal(primeiroCicloDeCobranca, segundoCicloDeCobranca);
    }
}
