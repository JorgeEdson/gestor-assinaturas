using GestorAssinaturas.Aplicacao.Assinaturas;
using GestorAssinaturas.Aplicacao.Testes.Dobras;
using GestorAssinaturas.Dominio.Assinaturas;
using GestorAssinaturas.Dominio.Faturas;
using GestorAssinaturas.Dominio.ObjetosDeValor;
using GestorAssinaturas.Dominio.Planos;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GestorAssinaturas.Aplicacao.Testes.Assinaturas;

public class TrocarPlanoServicoDeAplicacaoTestes
{
    private static readonly DateOnly DataDeInicio = new(2026, 8, 5);
    private static readonly Dinheiro PrecoDoPlanoAtual = Dinheiro.Criar(49.90m, "BRL").Instancia;
    private static readonly Dinheiro PrecoDoNovoPlano = Dinheiro.Criar(99.90m, "BRL").Instancia;

    private readonly UnitOfWorkEmMemoria _unitOfWork = new();

    private TrocarPlanoApplicationService CriarServico()
    {
        return new TrocarPlanoApplicationService(
            _unitOfWork,
            new ServicoTrocaDePlano(),
            NullLogger<TrocarPlanoApplicationService>.Instance);
    }

    private static Plano CriarPlano(string nome, Dinheiro preco, int periodoDeTrialEmDias)
    {
        return Plano.Criar(Guid.NewGuid(), nome, preco, CicloDeCobranca.Mensal, periodoDeTrialEmDias).Instancia;
    }

    private Assinatura SemearAssinatura(Plano plano)
    {
        var assinatura = Assinatura.Criar(Guid.NewGuid(), Guid.NewGuid(), plano, DataDeInicio).Instancia;

        _unitOfWork.AssinaturaEmMemoria.Semear(assinatura);

        return assinatura;
    }

    private Plano SemearNovoPlano()
    {
        var novoPlano = CriarPlano("Plano Profissional", PrecoDoNovoPlano, periodoDeTrialEmDias: 0);

        _unitOfWork.PlanoEmMemoria.AdicionarAsync(novoPlano).GetAwaiter().GetResult();

        return novoPlano;
    }

    private Fatura SemearFaturaEmAberto(Assinatura assinatura)
    {
        var fatura = Fatura.Emitir(Guid.NewGuid(), assinatura.Identificador, PrecoDoPlanoAtual, DataDeInicio).Instancia;

        _unitOfWork.FaturaEmMemoria.Semear(fatura);

        return fatura;
    }

    [Fact]
    public async Task DeveTrocarOPlanoEReprecificarFaturasEmAberto()
    {
        var assinatura = SemearAssinatura(CriarPlano("Plano Essencial", PrecoDoPlanoAtual, periodoDeTrialEmDias: 0));
        var novoPlano = SemearNovoPlano();
        var fatura = SemearFaturaEmAberto(assinatura);
        var servico = CriarServico();

        var resultado = await servico.ExecutarAsync(
            new TrocarPlanoEntrada(assinatura.Identificador, novoPlano.Identificador));

        Assert.True(resultado.EhSucesso);
        Assert.Equal(novoPlano.Identificador, assinatura.IdentificadorDoPlano);
        Assert.Equal(PrecoDoNovoPlano, fatura.Valor);
        Assert.Equal(1, _unitOfWork.QuantidadeDeSalvamentos);
    }

    [Fact]
    public async Task DeveRetornarFalhaQuandoAssinaturaNaoEncontrada()
    {
        var novoPlano = SemearNovoPlano();
        var servico = CriarServico();

        var resultado = await servico.ExecutarAsync(
            new TrocarPlanoEntrada(Guid.NewGuid(), novoPlano.Identificador));

        Assert.True(resultado.EhFalha);
        Assert.Equal(0, _unitOfWork.QuantidadeDeSalvamentos);
    }

    [Fact]
    public async Task DeveRetornarFalhaQuandoNovoPlanoNaoEncontrado()
    {
        var assinatura = SemearAssinatura(CriarPlano("Plano Essencial", PrecoDoPlanoAtual, periodoDeTrialEmDias: 0));
        var servico = CriarServico();

        var resultado = await servico.ExecutarAsync(
            new TrocarPlanoEntrada(assinatura.Identificador, Guid.NewGuid()));

        Assert.True(resultado.EhFalha);
        Assert.Equal(0, _unitOfWork.QuantidadeDeSalvamentos);
    }

    [Fact]
    public async Task DeveRetornarFalhaEManterFaturasQuandoODominioRejeitaATroca()
    {
        var assinatura = SemearAssinatura(CriarPlano("Plano Profissional", PrecoDoPlanoAtual, periodoDeTrialEmDias: 14));
        var novoPlano = SemearNovoPlano();
        var fatura = SemearFaturaEmAberto(assinatura);
        var servico = CriarServico();

        var resultado = await servico.ExecutarAsync(
            new TrocarPlanoEntrada(assinatura.Identificador, novoPlano.Identificador));

        Assert.True(resultado.EhFalha);
        Assert.True(assinatura.EstaEmTrial());
        Assert.Equal(PrecoDoPlanoAtual, fatura.Valor);
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
