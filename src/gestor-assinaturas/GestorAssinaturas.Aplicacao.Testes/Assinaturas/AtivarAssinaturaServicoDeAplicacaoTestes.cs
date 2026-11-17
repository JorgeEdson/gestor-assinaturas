using GestorAssinaturas.Aplicacao.Assinaturas;
using GestorAssinaturas.Aplicacao.Testes.Dobras;
using GestorAssinaturas.Dominio.Assinaturas;
using GestorAssinaturas.Dominio.ObjetosDeValor;
using GestorAssinaturas.Dominio.Planos;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GestorAssinaturas.Aplicacao.Testes.Assinaturas;

public class AtivarAssinaturaServicoDeAplicacaoTestes
{
    private static readonly DateOnly DataAtual = new(2026, 8, 5);

    private readonly UnidadeDeTrabalhoEmMemoria _unidadeDeTrabalho = new();

    private AtivarAssinaturaServicoDeAplicacao CriarServico()
    {
        return new AtivarAssinaturaServicoDeAplicacao(
            _unidadeDeTrabalho,
            new RelogioFixo(DataAtual),
            NullLogger<AtivarAssinaturaServicoDeAplicacao>.Instance);
    }

    private static Plano CriarPlano(int periodoDeTrialEmDias)
    {
        return Plano.Criar(
            Guid.NewGuid(),
            "Plano Profissional",
            Dinheiro.Criar(99.90m, "BRL").Instancia,
            CicloDeCobranca.Mensal,
            periodoDeTrialEmDias).Instancia;
    }

    private Assinatura SemearAssinatura(int periodoDeTrialEmDias)
    {
        var assinatura = Assinatura.Criar(
            Guid.NewGuid(),
            Guid.NewGuid(),
            CriarPlano(periodoDeTrialEmDias),
            DataAtual).Instancia;

        _unidadeDeTrabalho.AssinaturaEmMemoria.Semear(assinatura);

        return assinatura;
    }

    [Fact]
    public async Task DeveAtivarAssinaturaEmTrialEGerarPrimeiraFatura()
    {
        var assinatura = SemearAssinatura(periodoDeTrialEmDias: 14);
        var servico = CriarServico();

        var resultado = await servico.ExecutarAsync(
            new AtivarAssinaturaEntrada(assinatura.Identificador));

        Assert.True(resultado.EhSucesso);
        Assert.True(assinatura.EstaAtiva());
        Assert.Equal(DataAtual, assinatura.DataDeInicioDaCobranca);

        var fatura = Assert.Single(_unidadeDeTrabalho.FaturaEmMemoria.Faturas);
        Assert.Equal(assinatura.Identificador, fatura.IdentificadorDaAssinatura);
        Assert.Equal(resultado.Instancia, fatura.Identificador);
        Assert.True(fatura.EstaAberta());
        Assert.Equal(1, _unidadeDeTrabalho.QuantidadeDeSalvamentos);
    }

    [Fact]
    public async Task DeveRetornarFalhaQuandoAssinaturaNaoEncontrada()
    {
        var servico = CriarServico();

        var resultado = await servico.ExecutarAsync(
            new AtivarAssinaturaEntrada(Guid.NewGuid()));

        Assert.True(resultado.EhFalha);
        Assert.True(resultado.EhRecursoNaoEncontrado);
        Assert.Empty(_unidadeDeTrabalho.FaturaEmMemoria.Faturas);
        Assert.Equal(0, _unidadeDeTrabalho.QuantidadeDeSalvamentos);
    }

    [Fact]
    public async Task DeveRetornarFalhaAoAtivarAssinaturaQueNaoEstaEmTrial()
    {
        var assinatura = SemearAssinatura(periodoDeTrialEmDias: 0);
        var servico = CriarServico();

        var resultado = await servico.ExecutarAsync(
            new AtivarAssinaturaEntrada(assinatura.Identificador));

        Assert.True(resultado.EhFalha);
        Assert.Empty(_unidadeDeTrabalho.FaturaEmMemoria.Faturas);
        Assert.Equal(0, _unidadeDeTrabalho.QuantidadeDeSalvamentos);
    }

    [Fact]
    public async Task DeveRetornarFalhaParaEntradaNula()
    {
        var servico = CriarServico();

        var resultado = await servico.ExecutarAsync(null!);

        Assert.True(resultado.EhFalha);
        Assert.Empty(_unidadeDeTrabalho.FaturaEmMemoria.Faturas);
    }
}
