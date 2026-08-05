using GestorAssinaturas.Aplicacao.Assinaturas;
using GestorAssinaturas.Aplicacao.Portas.Pagamentos;
using GestorAssinaturas.Aplicacao.Testes.Dobras;
using GestorAssinaturas.Dominio.Assinaturas;
using GestorAssinaturas.Dominio.Faturas;
using GestorAssinaturas.Dominio.ObjetosDeValor;
using GestorAssinaturas.Dominio.Planos;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GestorAssinaturas.Aplicacao.Testes.Assinaturas;

public class RegistrarPagamentoServicoDeAplicacaoTestes
{
    private static readonly DateOnly DataDeReferencia = new(2026, 8, 5);

    private readonly UnitOfWorkEmMemoria _unitOfWork = new();

    private RegistrarPagamentoApplicationService CriarServico(GatewayPagamentoFalso gateway)
    {
        return new RegistrarPagamentoApplicationService(
            _unitOfWork,
            gateway,
            new ServicoReativacao(),
            new ServicoInadimplencia(),
            NullLogger<RegistrarPagamentoApplicationService>.Instance);
    }

    private static Plano CriarPlano()
    {
        return Plano.Criar(
            Guid.NewGuid(),
            "Plano Essencial",
            Dinheiro.Criar(49.90m, "BRL").Instancia,
            CicloDeCobranca.Mensal,
            periodoDeTrialEmDias: 0).Instancia;
    }

    private Assinatura SemearAssinatura()
    {
        var assinatura = Assinatura.Criar(Guid.NewGuid(), Guid.NewGuid(), CriarPlano(), DataDeReferencia).Instancia;

        _unitOfWork.AssinaturaEmMemoria.Semear(assinatura);

        return assinatura;
    }

    private Fatura SemearFaturaEmAberto(Assinatura assinatura)
    {
        var fatura = Fatura.Emitir(
            Guid.NewGuid(),
            assinatura.Identificador,
            Dinheiro.Criar(49.90m, "BRL").Instancia,
            DataDeReferencia).Instancia;

        _unitOfWork.FaturaEmMemoria.Semear(fatura);

        return fatura;
    }

    [Fact]
    public async Task DeveReativarAssinaturaInadimplenteQuandoOPagamentoEhAprovado()
    {
        var assinatura = SemearAssinatura();
        assinatura.RegistrarInadimplencia();
        var fatura = SemearFaturaEmAberto(assinatura);
        var gateway = GatewayPagamentoFalso.QueAprova();
        var servico = CriarServico(gateway);

        var resultado = await servico.ExecutarAsync(new RegistrarPagamentoEntrada(fatura.Identificador));

        Assert.True(resultado.EhSucesso);
        Assert.Equal(SituacaoDoPagamento.Aprovado, resultado.Instancia);
        Assert.True(fatura.EstaPaga());
        Assert.True(assinatura.EstaAtiva());
        Assert.Equal(1, _unitOfWork.QuantidadeDeSalvamentos);
    }

    [Fact]
    public async Task DeveMarcarFaturaComoPagaSemAlterarAssinaturaAtiva()
    {
        var assinatura = SemearAssinatura();
        var fatura = SemearFaturaEmAberto(assinatura);
        var gateway = GatewayPagamentoFalso.QueAprova();
        var servico = CriarServico(gateway);

        var resultado = await servico.ExecutarAsync(new RegistrarPagamentoEntrada(fatura.Identificador));

        Assert.True(resultado.EhSucesso);
        Assert.True(fatura.EstaPaga());
        Assert.True(assinatura.EstaAtiva());
    }

    [Fact]
    public async Task DeveMoverAssinaturaParaInadimplenteQuandoOPagamentoEhRecusado()
    {
        var assinatura = SemearAssinatura();
        var fatura = SemearFaturaEmAberto(assinatura);
        var gateway = GatewayPagamentoFalso.QueRecusa();
        var servico = CriarServico(gateway);

        var resultado = await servico.ExecutarAsync(new RegistrarPagamentoEntrada(fatura.Identificador));

        Assert.True(resultado.EhSucesso);
        Assert.Equal(SituacaoDoPagamento.Recusado, resultado.Instancia);
        Assert.Equal(StatusFatura.Falha, fatura.Status);
        Assert.True(assinatura.EstaInadimplente());
        Assert.Equal(1, _unitOfWork.QuantidadeDeSalvamentos);
    }

    [Fact]
    public async Task DeveRetornarFalhaEManterEstadoQuandoOGatewayFalha()
    {
        var assinatura = SemearAssinatura();
        var fatura = SemearFaturaEmAberto(assinatura);
        var gateway = GatewayPagamentoFalso.QueFalha();
        var servico = CriarServico(gateway);

        var resultado = await servico.ExecutarAsync(new RegistrarPagamentoEntrada(fatura.Identificador));

        Assert.True(resultado.EhFalha);
        Assert.True(fatura.EstaAberta());
        Assert.True(assinatura.EstaAtiva());
        Assert.Equal(0, _unitOfWork.QuantidadeDeSalvamentos);
    }

    [Fact]
    public async Task DeveRejeitarPagamentoDeFaturaQueNaoEstaEmAbertoSemAcionarOGateway()
    {
        var assinatura = SemearAssinatura();
        var fatura = SemearFaturaEmAberto(assinatura);
        fatura.MarcarComoPaga();
        var gateway = GatewayPagamentoFalso.QueAprova();
        var servico = CriarServico(gateway);

        var resultado = await servico.ExecutarAsync(new RegistrarPagamentoEntrada(fatura.Identificador));

        Assert.True(resultado.EhFalha);
        Assert.False(gateway.FoiAcionado);
        Assert.Equal(0, _unitOfWork.QuantidadeDeSalvamentos);
    }

    [Fact]
    public async Task DeveRetornarFalhaQuandoAFaturaNaoEhEncontrada()
    {
        var gateway = GatewayPagamentoFalso.QueAprova();
        var servico = CriarServico(gateway);

        var resultado = await servico.ExecutarAsync(new RegistrarPagamentoEntrada(Guid.NewGuid()));

        Assert.True(resultado.EhFalha);
        Assert.False(gateway.FoiAcionado);
        Assert.Equal(0, _unitOfWork.QuantidadeDeSalvamentos);
    }

    [Fact]
    public async Task DeveRetornarFalhaParaComandoNulo()
    {
        var gateway = GatewayPagamentoFalso.QueAprova();
        var servico = CriarServico(gateway);

        var resultado = await servico.ExecutarAsync(null!);

        Assert.True(resultado.EhFalha);
        Assert.False(gateway.FoiAcionado);
    }
}
