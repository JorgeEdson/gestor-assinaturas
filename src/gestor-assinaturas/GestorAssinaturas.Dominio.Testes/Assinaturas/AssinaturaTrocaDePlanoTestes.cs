using GestorAssinaturas.Dominio.Assinaturas;
using GestorAssinaturas.Dominio.ObjetosDeValor;
using GestorAssinaturas.Dominio.Planos;
using Xunit;

namespace GestorAssinaturas.Dominio.Testes.Assinaturas;

public class AssinaturaTrocaDePlanoTestes
{
    private static readonly Guid IdentificadorDaAssinatura = Guid.NewGuid();
    private static readonly Guid IdentificadorDoCliente = Guid.NewGuid();
    private static readonly DateOnly DataDeInicio = new(2026, 8, 5);

    private static Plano CriarPlano(string nome, decimal valor, string moeda)
    {
        return Plano.Criar(
            Guid.NewGuid(),
            nome,
            Dinheiro.Criar(valor, moeda).Instancia,
            CicloDeCobranca.Mensal,
            periodoDeTrialEmDias: 0).Instancia;
    }

    private static Assinatura CriarAssinaturaAtiva(Plano plano)
    {
        return Assinatura.Criar(IdentificadorDaAssinatura, IdentificadorDoCliente, plano, DataDeInicio).Instancia;
    }

    [Fact]
    public void DeveTrocarOPlanoDeUmaAssinaturaAtiva()
    {
        var planoAtual = CriarPlano("Plano Essencial", 49.90m, "BRL");
        var novoPlano = CriarPlano("Plano Profissional", 99.90m, "BRL");
        var assinatura = CriarAssinaturaAtiva(planoAtual);

        var resultado = assinatura.TrocarPlano(novoPlano);

        Assert.True(resultado.EhSucesso);
        Assert.Equal(novoPlano.Identificador, assinatura.IdentificadorDoPlano);
    }

    [Fact]
    public void DeveRejeitarTrocaDePlanoParaAssinaturaEmTrial()
    {
        var planoComTrial = Plano.Criar(
            Guid.NewGuid(),
            "Plano Profissional",
            Dinheiro.Criar(99.90m, "BRL").Instancia,
            CicloDeCobranca.Mensal,
            periodoDeTrialEmDias: 14).Instancia;
        var assinatura = Assinatura.Criar(IdentificadorDaAssinatura, IdentificadorDoCliente, planoComTrial, DataDeInicio).Instancia;
        var novoPlano = CriarPlano("Plano Essencial", 49.90m, "BRL");

        var resultado = assinatura.TrocarPlano(novoPlano);

        Assert.True(resultado.EhFalha);
    }

    [Fact]
    public void DeveRejeitarTrocaParaOMesmoPlano()
    {
        var planoAtual = CriarPlano("Plano Essencial", 49.90m, "BRL");
        var assinatura = CriarAssinaturaAtiva(planoAtual);

        var resultado = assinatura.TrocarPlano(planoAtual);

        Assert.True(resultado.EhFalha);
        Assert.Contains(resultado.Erros!, erro => erro.Contains("diferente"));
    }

    [Fact]
    public void DeveRejeitarTrocaQueMudaAMoedaDaAssinatura()
    {
        var planoAtual = CriarPlano("Plano Essencial", 49.90m, "BRL");
        var novoPlano = CriarPlano("Plano Global", 30m, "USD");
        var assinatura = CriarAssinaturaAtiva(planoAtual);

        var resultado = assinatura.TrocarPlano(novoPlano);

        Assert.True(resultado.EhFalha);
        Assert.Contains(resultado.Erros!, erro => erro.Contains("moeda"));
    }

    [Fact]
    public void DeveRejeitarTrocaParaNovoPlanoNulo()
    {
        var planoAtual = CriarPlano("Plano Essencial", 49.90m, "BRL");
        var assinatura = CriarAssinaturaAtiva(planoAtual);

        var resultado = assinatura.TrocarPlano(null!);

        Assert.True(resultado.EhFalha);
    }
}
