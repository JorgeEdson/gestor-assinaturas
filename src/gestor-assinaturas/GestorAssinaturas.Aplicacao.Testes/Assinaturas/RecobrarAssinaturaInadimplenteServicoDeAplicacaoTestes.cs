using GestorAssinaturas.Aplicacao.Assinaturas;
using GestorAssinaturas.Aplicacao.Testes.Dobras;
using GestorAssinaturas.Dominio.Assinaturas;
using GestorAssinaturas.Dominio.ObjetosDeValor;
using GestorAssinaturas.Dominio.Planos;
using GestorAssinaturas.Dominio.Servicos;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GestorAssinaturas.Aplicacao.Testes.Assinaturas;

public class RecobrarAssinaturaInadimplenteServicoDeAplicacaoTestes
{
    private static readonly DateOnly DataDeInicio = new(2026, 8, 5);
    private static readonly DateOnly DataDaRecobranca = new(2026, 8, 12);

    private readonly UnidadeDeTrabalhoEmMemoria _unidadeDeTrabalho = new();

    private RecobrarAssinaturaInadimplenteServicoDeAplicacao CriarServico()
    {
        return new RecobrarAssinaturaInadimplenteServicoDeAplicacao(
            _unidadeDeTrabalho,
            new RelogioFixo(DataDaRecobranca),
            new ServicoRecobranca(),
            NullLogger<RecobrarAssinaturaInadimplenteServicoDeAplicacao>.Instance);
    }

    private static Plano CriarPlano()
    {
        return Plano.Criar(
            Guid.NewGuid(),
            "Plano Cobranca Recusada",
            Dinheiro.Criar(49.99m, "BRL").Instancia,
            CicloDeCobranca.Mensal,
            periodoDeTrialEmDias: 0).Instancia;
    }

    private Assinatura SemearAssinaturaInadimplenteComFaturaRecusada()
    {
        var assinatura = Assinatura.Criar(Guid.NewGuid(), Guid.NewGuid(), CriarPlano(), DataDeInicio).Instancia;
        var faturaRecusada = assinatura.GerarFaturaDeCobranca(Guid.NewGuid(), DataDeInicio).Instancia;

        new ServicoInadimplencia().RegistrarPagamentoRecusado(assinatura, faturaRecusada);

        _unidadeDeTrabalho.AssinaturaEmMemoria.Semear(assinatura);
        _unidadeDeTrabalho.FaturaEmMemoria.Semear(faturaRecusada);

        return assinatura;
    }

    [Fact]
    public async Task DeveEmitirFaturaDeRecobrancaEmAbertoParaAssinaturaInadimplente()
    {
        var assinatura = SemearAssinaturaInadimplenteComFaturaRecusada();
        var servico = CriarServico();

        var resultado = await servico.ExecutarAsync(
            new RecobrarAssinaturaInadimplenteEntrada(assinatura.Identificador));

        Assert.True(resultado.EhSucesso);

        var faturaDeRecobranca = Assert.Single(
            _unidadeDeTrabalho.FaturaEmMemoria.Faturas,
            fatura => fatura.Identificador == resultado.Instancia);

        Assert.True(faturaDeRecobranca.EstaAberta());
        Assert.Equal(assinatura.ValorDaAssinatura, faturaDeRecobranca.Valor);
        Assert.Equal(DataDaRecobranca, faturaDeRecobranca.DataDeVencimento);
        Assert.True(assinatura.EstaInadimplente());
        Assert.Equal(1, _unidadeDeTrabalho.QuantidadeDeSalvamentos);
    }

    [Fact]
    public async Task DeveRejeitarNovaRecobrancaQuandoJaExisteFaturaEmAberto()
    {
        var assinatura = SemearAssinaturaInadimplenteComFaturaRecusada();
        var servico = CriarServico();
        await servico.ExecutarAsync(new RecobrarAssinaturaInadimplenteEntrada(assinatura.Identificador));

        var resultado = await servico.ExecutarAsync(
            new RecobrarAssinaturaInadimplenteEntrada(assinatura.Identificador));

        Assert.True(resultado.EhFalha);
        Assert.Equal(2, _unidadeDeTrabalho.FaturaEmMemoria.Faturas.Count);
        Assert.Equal(1, _unidadeDeTrabalho.QuantidadeDeSalvamentos);
    }

    [Fact]
    public async Task DeveRejeitarRecobrancaDeAssinaturaAtiva()
    {
        var assinatura = Assinatura.Criar(Guid.NewGuid(), Guid.NewGuid(), CriarPlano(), DataDeInicio).Instancia;
        _unidadeDeTrabalho.AssinaturaEmMemoria.Semear(assinatura);
        var servico = CriarServico();

        var resultado = await servico.ExecutarAsync(
            new RecobrarAssinaturaInadimplenteEntrada(assinatura.Identificador));

        Assert.True(resultado.EhFalha);
        Assert.False(resultado.EhRecursoNaoEncontrado);
        Assert.Empty(_unidadeDeTrabalho.FaturaEmMemoria.Faturas);
        Assert.Equal(0, _unidadeDeTrabalho.QuantidadeDeSalvamentos);
    }

    [Fact]
    public async Task DeveRetornarRecursoNaoEncontradoQuandoAssinaturaNaoExiste()
    {
        var servico = CriarServico();

        var resultado = await servico.ExecutarAsync(
            new RecobrarAssinaturaInadimplenteEntrada(Guid.NewGuid()));

        Assert.True(resultado.EhFalha);
        Assert.True(resultado.EhRecursoNaoEncontrado);
        Assert.Equal(0, _unidadeDeTrabalho.QuantidadeDeSalvamentos);
    }

    [Fact]
    public async Task DeveRetornarFalhaParaEntradaNula()
    {
        var servico = CriarServico();

        var resultado = await servico.ExecutarAsync(null!);

        Assert.True(resultado.EhFalha);
    }
}
