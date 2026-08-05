using GestorAssinaturas.Dominio.ObjetosDeValor;
using GestorAssinaturas.Dominio.Planos;
using Xunit;

namespace GestorAssinaturas.Dominio.Testes.Planos;

public class PlanoTestes
{
    private static readonly Guid IdentificadorDoPlano = Guid.NewGuid();

    private static Dinheiro PrecoValido => Dinheiro.Criar(49.90m, "BRL").Instancia;

    [Fact]
    public void DeveCadastrarPlanoComTodosOsDadosInformados()
    {
        var resultado = Plano.Criar(
            IdentificadorDoPlano,
            "Plano Profissional",
            PrecoValido,
            CicloDeCobranca.Mensal,
            periodoDeTrialEmDias: 14);

        Assert.True(resultado.EhSucesso);

        var plano = resultado.Instancia;

        Assert.Equal(IdentificadorDoPlano, plano.Identificador);
        Assert.Equal("Plano Profissional", plano.Nome);
        Assert.Equal(PrecoValido, plano.Preco);
        Assert.Equal(CicloDeCobranca.Mensal, plano.CicloDeCobranca);
        Assert.Equal(14, plano.PeriodoDeTrialEmDias);
    }

    [Fact]
    public void DeveRemoverEspacosEmBrancoDoNomeDoPlano()
    {
        var resultado = Plano.Criar(
            IdentificadorDoPlano,
            "  Plano Essencial  ",
            PrecoValido,
            CicloDeCobranca.Anual,
            periodoDeTrialEmDias: 0);

        Assert.True(resultado.EhSucesso);
        Assert.Equal("Plano Essencial", resultado.Instancia.Nome);
    }

    [Fact]
    public void DeveCadastrarPlanoSemPeriodoDeTrial()
    {
        var resultado = Plano.Criar(
            IdentificadorDoPlano,
            "Plano Essencial",
            PrecoValido,
            CicloDeCobranca.Mensal,
            periodoDeTrialEmDias: 0);

        Assert.True(resultado.EhSucesso);
        Assert.False(resultado.Instancia.PossuiPeriodoDeTrial());
    }

    [Fact]
    public void DeveIdentificarPlanoComPeriodoDeTrial()
    {
        var resultado = Plano.Criar(
            IdentificadorDoPlano,
            "Plano Profissional",
            PrecoValido,
            CicloDeCobranca.Mensal,
            periodoDeTrialEmDias: 7);

        Assert.True(resultado.EhSucesso);
        Assert.True(resultado.Instancia.PossuiPeriodoDeTrial());
    }

    [Fact]
    public void DeveRetornarFalhaParaIdentificadorVazio()
    {
        var resultado = Plano.Criar(
            Guid.Empty,
            "Plano Profissional",
            PrecoValido,
            CicloDeCobranca.Mensal,
            periodoDeTrialEmDias: 0);

        Assert.True(resultado.EhFalha);
        Assert.Contains(resultado.Erros!, erro => erro.Contains("identificador"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("AB")]
    public void DeveRetornarFalhaParaNomeInvalido(string nomeInvalido)
    {
        var resultado = Plano.Criar(
            IdentificadorDoPlano,
            nomeInvalido,
            PrecoValido,
            CicloDeCobranca.Mensal,
            periodoDeTrialEmDias: 0);

        Assert.True(resultado.EhFalha);
    }

    [Fact]
    public void DeveRetornarFalhaParaNomeAcimaDoLimiteDeCaracteres()
    {
        var nomeExcessivamenteLongo = new string('A', Plano.QuantidadeMaximaDeCaracteresDoNome + 1);

        var resultado = Plano.Criar(
            IdentificadorDoPlano,
            nomeExcessivamenteLongo,
            PrecoValido,
            CicloDeCobranca.Mensal,
            periodoDeTrialEmDias: 0);

        Assert.True(resultado.EhFalha);
    }

    [Fact]
    public void DeveRetornarFalhaParaPrecoZerado()
    {
        var resultado = Plano.Criar(
            IdentificadorDoPlano,
            "Plano Gratuito",
            Dinheiro.Zero("BRL").Instancia,
            CicloDeCobranca.Mensal,
            periodoDeTrialEmDias: 0);

        Assert.True(resultado.EhFalha);
        Assert.Contains(resultado.Erros!, erro => erro.Contains("maior que zero"));
    }

    [Fact]
    public void DeveRetornarFalhaParaPeriodoDeTrialNegativo()
    {
        var resultado = Plano.Criar(
            IdentificadorDoPlano,
            "Plano Profissional",
            PrecoValido,
            CicloDeCobranca.Mensal,
            periodoDeTrialEmDias: -1);

        Assert.True(resultado.EhFalha);
    }

    [Fact]
    public void DeveRetornarFalhaParaPeriodoDeTrialAcimaDoLimitePermitido()
    {
        var resultado = Plano.Criar(
            IdentificadorDoPlano,
            "Plano Profissional",
            PrecoValido,
            CicloDeCobranca.Mensal,
            periodoDeTrialEmDias: Plano.QuantidadeMaximaDeDiasDePeriodoDeTrial + 1);

        Assert.True(resultado.EhFalha);
    }

    [Fact]
    public void DeveAcumularTodosOsErrosDeValidacaoEmUmUnicoResultado()
    {
        var resultado = Plano.Criar(
            Guid.Empty,
            string.Empty,
            Dinheiro.Zero("BRL").Instancia,
            CicloDeCobranca.Mensal,
            periodoDeTrialEmDias: -1);

        Assert.True(resultado.EhFalha);
        Assert.Equal(4, resultado.Erros!.Count());
    }

    [Fact]
    public void DeveCalcularDataDeTerminoDoPeriodoDeTrial()
    {
        var plano = Plano.Criar(
            IdentificadorDoPlano,
            "Plano Profissional",
            PrecoValido,
            CicloDeCobranca.Mensal,
            periodoDeTrialEmDias: 14).Instancia;

        var resultado = plano.CalcularDataDeTerminoDoPeriodoDeTrial(new DateOnly(2026, 8, 5));

        Assert.True(resultado.EhSucesso);
        Assert.Equal(new DateOnly(2026, 8, 19), resultado.Instancia);
    }

    [Fact]
    public void DeveRetornarFalhaAoCalcularTerminoDeTrialEmPlanoSemPeriodoDeTrial()
    {
        var plano = Plano.Criar(
            IdentificadorDoPlano,
            "Plano Essencial",
            PrecoValido,
            CicloDeCobranca.Mensal,
            periodoDeTrialEmDias: 0).Instancia;

        var resultado = plano.CalcularDataDeTerminoDoPeriodoDeTrial(new DateOnly(2026, 8, 5));

        Assert.True(resultado.EhFalha);
    }

    [Fact]
    public void DeveCalcularDataDeVencimentoDoProximoCicloConformeOCicloDoPlano()
    {
        var plano = Plano.Criar(
            IdentificadorDoPlano,
            "Plano Anual",
            PrecoValido,
            CicloDeCobranca.Anual,
            periodoDeTrialEmDias: 0).Instancia;

        var dataDeVencimento = plano.CalcularDataDeVencimentoDoProximoCiclo(new DateOnly(2026, 8, 5));

        Assert.Equal(new DateOnly(2027, 8, 5), dataDeVencimento);
    }

    [Fact]
    public void DeveConsiderarIguaisDoisPlanosComOMesmoIdentificador()
    {
        var primeiroPlano = Plano.Criar(
            IdentificadorDoPlano,
            "Plano Profissional",
            PrecoValido,
            CicloDeCobranca.Mensal,
            periodoDeTrialEmDias: 0).Instancia;

        var segundoPlano = Plano.Criar(
            IdentificadorDoPlano,
            "Plano Profissional Renomeado",
            Dinheiro.Criar(99m, "BRL").Instancia,
            CicloDeCobranca.Anual,
            periodoDeTrialEmDias: 30).Instancia;

        Assert.Equal(primeiroPlano, segundoPlano);
    }
}
