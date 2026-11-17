using GestorAssinaturas.Dominio.Assinaturas;
using GestorAssinaturas.Dominio.Faturas;
using GestorAssinaturas.Dominio.ObjetosDeValor;
using GestorAssinaturas.Dominio.Planos;
using GestorAssinaturas.Dominio.Servicos;
using Xunit;

namespace GestorAssinaturas.Dominio.Testes.Servicos;

public class ServicoRecobrancaTestes
{
    private static readonly DateOnly DataDeInicio = new(2026, 8, 5);
    private static readonly DateOnly DataDaRecobranca = new(2026, 8, 12);

    private static Plano PlanoSemTrial => Plano.Criar(
        Guid.NewGuid(),
        "Plano Cobranca Recusada",
        Dinheiro.Criar(49.99m, "BRL").Instancia,
        CicloDeCobranca.Mensal,
        periodoDeTrialEmDias: 0).Instancia;

    private static (Assinatura Assinatura, Fatura FaturaRecusada) CriarAssinaturaInadimplente()
    {
        var assinatura = Assinatura.Criar(Guid.NewGuid(), Guid.NewGuid(), PlanoSemTrial, DataDeInicio).Instancia;
        var faturaRecusada = assinatura.GerarFaturaDeCobranca(Guid.NewGuid(), DataDeInicio).Instancia;

        new ServicoInadimplencia().RegistrarPagamentoRecusado(assinatura, faturaRecusada);

        return (assinatura, faturaRecusada);
    }

    [Fact]
    public void DeveEmitirFaturaDeRecobrancaQuandoNaoHaFaturaEmAberto()
    {
        var (assinatura, faturaRecusada) = CriarAssinaturaInadimplente();
        var servico = new ServicoRecobranca();

        var resultado = servico.EmitirFaturaDeRecobranca(assinatura, new[] { faturaRecusada }, Guid.NewGuid(), DataDaRecobranca);

        Assert.True(resultado.EhSucesso);
        Assert.True(resultado.Instancia.EstaAberta());
        Assert.Equal(assinatura.Identificador, resultado.Instancia.IdentificadorDaAssinatura);
    }

    [Fact]
    public void DeveRejeitarRecobrancaQuandoJaExisteFaturaEmAberto()
    {
        var (assinatura, faturaRecusada) = CriarAssinaturaInadimplente();
        var faturaEmAberto = assinatura.EmitirFaturaDeRecobranca(Guid.NewGuid(), DataDaRecobranca).Instancia;
        var servico = new ServicoRecobranca();

        var resultado = servico.EmitirFaturaDeRecobranca(
            assinatura,
            new[] { faturaRecusada, faturaEmAberto },
            Guid.NewGuid(),
            DataDaRecobranca);

        Assert.True(resultado.EhFalha);
        Assert.Contains(resultado.Erros!, erro => erro.Contains("em aberto"));
    }

    [Fact]
    public void DeveFecharOCicloDeInadimplenciaQuandoARecobrancaEhPaga()
    {
        var (assinatura, faturaRecusada) = CriarAssinaturaInadimplente();
        var faturaDeRecobranca = new ServicoRecobranca()
            .EmitirFaturaDeRecobranca(assinatura, new[] { faturaRecusada }, Guid.NewGuid(), DataDaRecobranca)
            .Instancia;

        var resultado = new ServicoReativacao().RegistrarPagamentoAprovado(assinatura, faturaDeRecobranca);

        Assert.True(resultado.EhSucesso);
        Assert.True(faturaDeRecobranca.EstaPaga());
        Assert.True(assinatura.EstaAtiva());
    }

    [Fact]
    public void DeveRejeitarRecobrancaDeAssinaturaQueNaoEstaInadimplente()
    {
        var assinatura = Assinatura.Criar(Guid.NewGuid(), Guid.NewGuid(), PlanoSemTrial, DataDeInicio).Instancia;
        var servico = new ServicoRecobranca();

        var resultado = servico.EmitirFaturaDeRecobranca(assinatura, Array.Empty<Fatura>(), Guid.NewGuid(), DataDaRecobranca);

        Assert.True(resultado.EhFalha);
    }

    [Fact]
    public void DeveRetornarFalhaQuandoAAssinaturaNaoEhInformada()
    {
        var servico = new ServicoRecobranca();

        var resultado = servico.EmitirFaturaDeRecobranca(null!, Array.Empty<Fatura>(), Guid.NewGuid(), DataDaRecobranca);

        Assert.True(resultado.EhFalha);
    }

    [Fact]
    public void DeveRetornarFalhaQuandoAColecaoDeFaturasNaoEhInformada()
    {
        var (assinatura, _) = CriarAssinaturaInadimplente();
        var servico = new ServicoRecobranca();

        var resultado = servico.EmitirFaturaDeRecobranca(assinatura, null!, Guid.NewGuid(), DataDaRecobranca);

        Assert.True(resultado.EhFalha);
    }
}
