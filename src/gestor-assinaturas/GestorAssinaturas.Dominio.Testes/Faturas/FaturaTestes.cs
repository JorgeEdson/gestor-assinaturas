using GestorAssinaturas.Dominio.Faturas;
using GestorAssinaturas.Dominio.ObjetosDeValor;
using Xunit;

namespace GestorAssinaturas.Dominio.Testes.Faturas;

public class FaturaTestes
{
    private static readonly Guid IdentificadorDaFatura = Guid.NewGuid();
    private static readonly Guid IdentificadorDaAssinatura = Guid.NewGuid();
    private static readonly DateOnly DataDeVencimento = new(2026, 9, 5);

    private static Dinheiro ValorValido => Dinheiro.Criar(49.90m, "BRL").Instancia;

    private static Fatura FaturaEmAberto()
    {
        return Fatura.Emitir(IdentificadorDaFatura, IdentificadorDaAssinatura, ValorValido, DataDeVencimento).Instancia;
    }

    [Fact]
    public void DeveEmitirFaturaEmAbertoComOsDadosInformados()
    {
        var resultado = Fatura.Emitir(IdentificadorDaFatura, IdentificadorDaAssinatura, ValorValido, DataDeVencimento);

        Assert.True(resultado.EhSucesso);

        var fatura = resultado.Instancia;

        Assert.Equal(IdentificadorDaFatura, fatura.Identificador);
        Assert.Equal(IdentificadorDaAssinatura, fatura.IdentificadorDaAssinatura);
        Assert.Equal(ValorValido, fatura.Valor);
        Assert.Equal(DataDeVencimento, fatura.DataDeVencimento);
        Assert.Equal(StatusFatura.Aberta, fatura.Status);
    }

    [Fact]
    public void DeveRetornarFalhaParaIdentificadorDaFaturaVazio()
    {
        var resultado = Fatura.Emitir(Guid.Empty, IdentificadorDaAssinatura, ValorValido, DataDeVencimento);

        Assert.True(resultado.EhFalha);
    }

    [Fact]
    public void DeveRetornarFalhaParaFaturaSemAssinaturaVinculada()
    {
        var resultado = Fatura.Emitir(IdentificadorDaFatura, Guid.Empty, ValorValido, DataDeVencimento);

        Assert.True(resultado.EhFalha);
        Assert.Contains(resultado.Erros!, erro => erro.Contains("assinatura"));
    }

    [Fact]
    public void DeveRetornarFalhaParaValorZerado()
    {
        var resultado = Fatura.Emitir(
            IdentificadorDaFatura,
            IdentificadorDaAssinatura,
            Dinheiro.Zero("BRL").Instancia,
            DataDeVencimento);

        Assert.True(resultado.EhFalha);
        Assert.Contains(resultado.Erros!, erro => erro.Contains("maior que zero"));
    }

    [Fact]
    public void DeveMarcarFaturaEmAbertoComoPaga()
    {
        var fatura = FaturaEmAberto();

        var resultado = fatura.MarcarComoPaga();

        Assert.True(resultado.EhSucesso);
        Assert.Equal(StatusFatura.Paga, fatura.Status);
        Assert.True(fatura.EstaPaga());
    }

    [Fact]
    public void DeveMarcarFaturaEmAbertoComoFalha()
    {
        var fatura = FaturaEmAberto();

        var resultado = fatura.MarcarComoFalha();

        Assert.True(resultado.EhSucesso);
        Assert.Equal(StatusFatura.Falha, fatura.Status);
    }

    [Fact]
    public void DeveRetornarFalhaAoMarcarComoPagaUmaFaturaQueNaoEstaEmAberto()
    {
        var fatura = FaturaEmAberto();
        fatura.MarcarComoPaga();

        var resultado = fatura.MarcarComoPaga();

        Assert.True(resultado.EhFalha);
    }

    [Fact]
    public void DeveRetornarFalhaAoMarcarComoFalhaUmaFaturaJaPaga()
    {
        var fatura = FaturaEmAberto();
        fatura.MarcarComoPaga();

        var resultado = fatura.MarcarComoFalha();

        Assert.True(resultado.EhFalha);
    }

    [Fact]
    public void DeveAtualizarOValorDeUmaFaturaEmAberto()
    {
        var fatura = FaturaEmAberto();
        var novoValor = Dinheiro.Criar(99.90m, "BRL").Instancia;

        var resultado = fatura.AtualizarValor(novoValor);

        Assert.True(resultado.EhSucesso);
        Assert.Equal(novoValor, fatura.Valor);
    }

    [Fact]
    public void DeveRetornarFalhaAoAtualizarOValorDeUmaFaturaQueNaoEstaEmAberto()
    {
        var fatura = FaturaEmAberto();
        fatura.MarcarComoPaga();
        var novoValor = Dinheiro.Criar(99.90m, "BRL").Instancia;

        var resultado = fatura.AtualizarValor(novoValor);

        Assert.True(resultado.EhFalha);
    }

    [Fact]
    public void DeveRetornarFalhaAoAtualizarParaUmValorZerado()
    {
        var fatura = FaturaEmAberto();

        var resultado = fatura.AtualizarValor(Dinheiro.Zero("BRL").Instancia);

        Assert.True(resultado.EhFalha);
    }
}
