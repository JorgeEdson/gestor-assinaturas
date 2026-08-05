using GestorAssinaturas.Aplicacao.Assinaturas;
using GestorAssinaturas.Aplicacao.Testes.Dobras;
using GestorAssinaturas.Dominio.Assinaturas;
using GestorAssinaturas.Dominio.ObjetosDeValor;
using GestorAssinaturas.Dominio.Planos;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GestorAssinaturas.Aplicacao.Testes.Assinaturas;

public class CancelarAssinaturaServicoDeAplicacaoTestes
{
    private static readonly DateOnly DataDeInicio = new(2026, 8, 5);

    private readonly UnitOfWorkEmMemoria _unitOfWork = new();

    private CancelarAssinaturaApplicationService CriarServico()
    {
        return new CancelarAssinaturaApplicationService(
            _unitOfWork,
            NullLogger<CancelarAssinaturaApplicationService>.Instance);
    }

    private static Plano CriarPlanoMensalSemTrial()
    {
        return Plano.Criar(
            Guid.NewGuid(),
            "Plano Essencial",
            Dinheiro.Criar(49.90m, "BRL").Instancia,
            CicloDeCobranca.Mensal,
            periodoDeTrialEmDias: 0).Instancia;
    }

    private Assinatura SemearAssinaturaAtiva()
    {
        var assinatura = Assinatura.Criar(Guid.NewGuid(), Guid.NewGuid(), CriarPlanoMensalSemTrial(), DataDeInicio).Instancia;

        _unitOfWork.AssinaturaEmMemoria.Semear(assinatura);

        return assinatura;
    }

    [Fact]
    public async Task DeveCancelarImediatamenteUmaAssinaturaAtiva()
    {
        var assinatura = SemearAssinaturaAtiva();
        var servico = CriarServico();

        var resultado = await servico.ExecutarAsync(
            new CancelarAssinaturaEntrada(assinatura.Identificador, ModalidadeDeCancelamento.Imediato));

        Assert.True(resultado.EhSucesso);
        Assert.True(assinatura.EstaCancelada());
        Assert.Equal(1, _unitOfWork.QuantidadeDeSalvamentos);
    }

    [Fact]
    public async Task DeveAgendarCancelamentoAoFimDoPeriodoMantendoAAssinaturaAtiva()
    {
        var assinatura = SemearAssinaturaAtiva();
        var servico = CriarServico();

        var resultado = await servico.ExecutarAsync(
            new CancelarAssinaturaEntrada(assinatura.Identificador, ModalidadeDeCancelamento.AoFimDoPeriodoVigente));

        Assert.True(resultado.EhSucesso);
        Assert.True(assinatura.EstaAtiva());
        Assert.True(assinatura.PossuiCancelamentoAgendado);
        Assert.Equal(new DateOnly(2026, 9, 5), assinatura.DataDeCancelamentoAgendado);
        Assert.Equal(1, _unitOfWork.QuantidadeDeSalvamentos);
    }

    [Fact]
    public async Task DeveRetornarFalhaQuandoAssinaturaNaoEncontrada()
    {
        var servico = CriarServico();

        var resultado = await servico.ExecutarAsync(
            new CancelarAssinaturaEntrada(Guid.NewGuid(), ModalidadeDeCancelamento.Imediato));

        Assert.True(resultado.EhFalha);
        Assert.Equal(0, _unitOfWork.QuantidadeDeSalvamentos);
    }

    [Fact]
    public async Task DeveRetornarFalhaQuandoODominioRejeitaOCancelamentoImediato()
    {
        var assinatura = SemearAssinaturaAtiva();
        assinatura.RegistrarInadimplencia();
        var servico = CriarServico();

        var resultado = await servico.ExecutarAsync(
            new CancelarAssinaturaEntrada(assinatura.Identificador, ModalidadeDeCancelamento.Imediato));

        Assert.True(resultado.EhFalha);
        Assert.True(assinatura.EstaInadimplente());
        Assert.Equal(0, _unitOfWork.QuantidadeDeSalvamentos);
    }

    [Fact]
    public async Task DeveRetornarFalhaQuandoODominioRejeitaOAgendamento()
    {
        var assinatura = SemearAssinaturaAtiva();
        assinatura.RegistrarInadimplencia();
        var servico = CriarServico();

        var resultado = await servico.ExecutarAsync(
            new CancelarAssinaturaEntrada(assinatura.Identificador, ModalidadeDeCancelamento.AoFimDoPeriodoVigente));

        Assert.True(resultado.EhFalha);
        Assert.False(assinatura.PossuiCancelamentoAgendado);
        Assert.Equal(0, _unitOfWork.QuantidadeDeSalvamentos);
    }

    [Fact]
    public async Task DeveRetornarFalhaParaComandoNulo()
    {
        var servico = CriarServico();

        var resultado = await servico.ExecutarAsync(null!);

        Assert.True(resultado.EhFalha);
    }
}
