using GestorAssinaturas.Dominio.Assinaturas;
using GestorAssinaturas.Dominio.Faturas;
using GestorAssinaturas.Dominio.ObjetosDeValor;
using GestorAssinaturas.Dominio.Planos;
using Xunit;

namespace GestorAssinaturas.Dominio.Testes.Assinaturas;

public class ServicoReativacaoTestes
{
    private static readonly Guid IdentificadorDaAssinatura = Guid.NewGuid();
    private static readonly Guid IdentificadorDoCliente = Guid.NewGuid();
    private static readonly DateOnly DataDeInicio = new(2026, 8, 5);
    private static readonly DateOnly DataDeVencimento = new(2026, 8, 5);

    private static Plano PlanoSemTrial => Plano.Criar(
        Guid.NewGuid(),
        "Plano Essencial",
        Dinheiro.Criar(49.90m, "BRL").Instancia,
        CicloDeCobranca.Mensal,
        periodoDeTrialEmDias: 0).Instancia;

    private static Assinatura CriarAssinaturaAtiva()
    {
        return Assinatura.Criar(IdentificadorDaAssinatura, IdentificadorDoCliente, PlanoSemTrial, DataDeInicio).Instancia;
    }

    private static Fatura EmitirFatura(Guid identificadorDaAssinatura)
    {
        return Fatura.Emitir(
            Guid.NewGuid(),
            identificadorDaAssinatura,
            Dinheiro.Criar(49.90m, "BRL").Instancia,
            DataDeVencimento).Instancia;
    }

    [Fact]
    public void DeveMarcarFaturaComoPagaEReativarAssinaturaInadimplente()
    {
        var assinatura = CriarAssinaturaAtiva();
        assinatura.RegistrarInadimplencia();
        var fatura = EmitirFatura(assinatura.Identificador);
        var servico = new ServicoReativacao();

        var resultado = servico.RegistrarPagamentoAprovado(assinatura, fatura);

        Assert.True(resultado.EhSucesso);
        Assert.True(fatura.EstaPaga());
        Assert.True(assinatura.EstaAtiva());
    }

    [Fact]
    public void DeveMarcarFaturaComoPagaSemAlterarAssinaturaJaAtiva()
    {
        var assinatura = CriarAssinaturaAtiva();
        var fatura = EmitirFatura(assinatura.Identificador);
        var servico = new ServicoReativacao();

        var resultado = servico.RegistrarPagamentoAprovado(assinatura, fatura);

        Assert.True(resultado.EhSucesso);
        Assert.True(fatura.EstaPaga());
        Assert.True(assinatura.EstaAtiva());
    }

    [Fact]
    public void DeveRetornarFalhaQuandoAFaturaNaoPertenceAAssinatura()
    {
        var assinatura = CriarAssinaturaAtiva();
        assinatura.RegistrarInadimplencia();
        var faturaDeOutraAssinatura = EmitirFatura(Guid.NewGuid());
        var servico = new ServicoReativacao();

        var resultado = servico.RegistrarPagamentoAprovado(assinatura, faturaDeOutraAssinatura);

        Assert.True(resultado.EhFalha);
        Assert.True(assinatura.EstaInadimplente());
    }

    [Fact]
    public void DeveRetornarFalhaAoRegistrarPagamentoDeFaturaQueNaoEstaEmAberto()
    {
        var assinatura = CriarAssinaturaAtiva();
        assinatura.RegistrarInadimplencia();
        var fatura = EmitirFatura(assinatura.Identificador);
        fatura.MarcarComoFalha();
        var servico = new ServicoReativacao();

        var resultado = servico.RegistrarPagamentoAprovado(assinatura, fatura);

        Assert.True(resultado.EhFalha);
        Assert.True(assinatura.EstaInadimplente());
    }

    [Fact]
    public void DeveRetornarFalhaQuandoAAssinaturaNaoEhInformada()
    {
        var fatura = EmitirFatura(IdentificadorDaAssinatura);
        var servico = new ServicoReativacao();

        var resultado = servico.RegistrarPagamentoAprovado(null!, fatura);

        Assert.True(resultado.EhFalha);
    }

    [Fact]
    public void DeveRetornarFalhaQuandoAFaturaNaoEhInformada()
    {
        var assinatura = CriarAssinaturaAtiva();
        var servico = new ServicoReativacao();

        var resultado = servico.RegistrarPagamentoAprovado(assinatura, null!);

        Assert.True(resultado.EhFalha);
    }
}
