using GestorAssinaturas.Aplicacao.Planos;
using GestorAssinaturas.Aplicacao.Testes.Dobras;
using GestorAssinaturas.Dominio.ObjetosDeValor;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GestorAssinaturas.Aplicacao.Testes.Planos;

public class CadastrarPlanoServicoDeAplicacaoTestes
{
    private readonly UnitOfWorkEmMemoria _unitOfWork = new();

    private CadastrarPlanoApplicationService CriarServico()
    {
        return new CadastrarPlanoApplicationService(
            _unitOfWork,
            NullLogger<CadastrarPlanoApplicationService>.Instance);
    }

    [Fact]
    public async Task DeveCadastrarPlanoValidoEPersistirComSalvamento()
    {
        var servico = CriarServico();
        var comando = new CadastrarPlanoEntrada(
            "Plano Profissional",
            99.90m,
            "BRL",
            TipoDeCicloDeCobranca.Mensal,
            PeriodoDeTrialEmDias: 14);

        var resultado = await servico.ExecutarAsync(comando);

        Assert.True(resultado.EhSucesso);
        Assert.NotEqual(Guid.Empty, resultado.Instancia);
        Assert.Single(_unitOfWork.PlanoEmMemoria.Planos);
        Assert.Equal(1, _unitOfWork.QuantidadeDeSalvamentos);
    }

    [Fact]
    public async Task DevePersistirOPlanoComOIdentificadorRetornado()
    {
        var servico = CriarServico();
        var comando = new CadastrarPlanoEntrada(
            "Plano Essencial",
            49.90m,
            "BRL",
            TipoDeCicloDeCobranca.Anual,
            PeriodoDeTrialEmDias: 0);

        var resultado = await servico.ExecutarAsync(comando);

        var planoPersistido = Assert.Single(_unitOfWork.PlanoEmMemoria.Planos);
        Assert.Equal(resultado.Instancia, planoPersistido.Identificador);
        Assert.Equal("Plano Essencial", planoPersistido.Nome);
    }

    [Fact]
    public async Task DeveRetornarFalhaParaMoedaInvalidaSemPersistir()
    {
        var servico = CriarServico();
        var comando = new CadastrarPlanoEntrada(
            "Plano Profissional",
            99.90m,
            "MOEDA_INVALIDA",
            TipoDeCicloDeCobranca.Mensal,
            PeriodoDeTrialEmDias: 0);

        var resultado = await servico.ExecutarAsync(comando);

        Assert.True(resultado.EhFalha);
        Assert.Empty(_unitOfWork.PlanoEmMemoria.Planos);
        Assert.Equal(0, _unitOfWork.QuantidadeDeSalvamentos);
    }

    [Fact]
    public async Task DeveRetornarFalhaParaPrecoZeradoSemPersistir()
    {
        var servico = CriarServico();
        var comando = new CadastrarPlanoEntrada(
            "Plano Gratuito",
            0m,
            "BRL",
            TipoDeCicloDeCobranca.Mensal,
            PeriodoDeTrialEmDias: 0);

        var resultado = await servico.ExecutarAsync(comando);

        Assert.True(resultado.EhFalha);
        Assert.Empty(_unitOfWork.PlanoEmMemoria.Planos);
        Assert.Equal(0, _unitOfWork.QuantidadeDeSalvamentos);
    }

    [Fact]
    public async Task DeveRetornarFalhaParaNomeInvalidoSemPersistir()
    {
        var servico = CriarServico();
        var comando = new CadastrarPlanoEntrada(
            "AB",
            49.90m,
            "BRL",
            TipoDeCicloDeCobranca.Mensal,
            PeriodoDeTrialEmDias: 0);

        var resultado = await servico.ExecutarAsync(comando);

        Assert.True(resultado.EhFalha);
        Assert.Empty(_unitOfWork.PlanoEmMemoria.Planos);
    }

    [Fact]
    public async Task DeveRetornarFalhaParaComandoNulo()
    {
        var servico = CriarServico();

        var resultado = await servico.ExecutarAsync(null!);

        Assert.True(resultado.EhFalha);
        Assert.Empty(_unitOfWork.PlanoEmMemoria.Planos);
    }
}
