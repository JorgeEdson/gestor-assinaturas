using GestorAssinaturas.Aplicacao.Assinaturas;
using GestorAssinaturas.Aplicacao.Testes.Dobras;
using GestorAssinaturas.Dominio.Clientes;
using GestorAssinaturas.Dominio.ObjetosDeValor;
using GestorAssinaturas.Dominio.Planos;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GestorAssinaturas.Aplicacao.Testes.Assinaturas;

public class CriarAssinaturaServicoDeAplicacaoTestes
{
    private static readonly DateOnly DataAtual = new(2026, 8, 5);

    private readonly UnitOfWorkEmMemoria _unitOfWork = new();

    private CriarAssinaturaApplicationService CriarServico()
    {
        return new CriarAssinaturaApplicationService(
            _unitOfWork,
            new RelogioFixo(DataAtual),
            NullLogger<CriarAssinaturaApplicationService>.Instance);
    }

    private Cliente SemearCliente()
    {
        var cliente = Cliente.Criar(
            Guid.NewGuid(),
            "Empresa Contratante",
            Email.Criar("contato@empresa.com").Instancia).Instancia;

        _unitOfWork.ClienteEmMemoria.AdicionarAsync(cliente).GetAwaiter().GetResult();

        return cliente;
    }

    private Plano SemearPlano(int periodoDeTrialEmDias)
    {
        var plano = Plano.Criar(
            Guid.NewGuid(),
            "Plano Profissional",
            Dinheiro.Criar(99.90m, "BRL").Instancia,
            CicloDeCobranca.Mensal,
            periodoDeTrialEmDias).Instancia;

        _unitOfWork.PlanoEmMemoria.AdicionarAsync(plano).GetAwaiter().GetResult();

        return plano;
    }

    [Fact]
    public async Task DeveCriarAssinaturaEmTrialSemGerarFatura()
    {
        var cliente = SemearCliente();
        var plano = SemearPlano(periodoDeTrialEmDias: 14);
        var servico = CriarServico();

        var resultado = await servico.ExecutarAsync(
            new CriarAssinaturaEntrada(cliente.Identificador, plano.Identificador));

        Assert.True(resultado.EhSucesso);

        var assinatura = Assert.Single(_unitOfWork.AssinaturaEmMemoria.Assinaturas);
        Assert.True(assinatura.EstaEmTrial());
        Assert.Empty(_unitOfWork.FaturaEmMemoria.Faturas);
        Assert.Equal(1, _unitOfWork.QuantidadeDeSalvamentos);
    }

    [Fact]
    public async Task DeveCriarAssinaturaAtivaComCobrancaImediataQuandoPlanoNaoTemTrial()
    {
        var cliente = SemearCliente();
        var plano = SemearPlano(periodoDeTrialEmDias: 0);
        var servico = CriarServico();

        var resultado = await servico.ExecutarAsync(
            new CriarAssinaturaEntrada(cliente.Identificador, plano.Identificador));

        Assert.True(resultado.EhSucesso);

        var assinatura = Assert.Single(_unitOfWork.AssinaturaEmMemoria.Assinaturas);
        Assert.True(assinatura.EstaAtiva());

        var fatura = Assert.Single(_unitOfWork.FaturaEmMemoria.Faturas);
        Assert.Equal(assinatura.Identificador, fatura.IdentificadorDaAssinatura);
        Assert.Equal(plano.Preco, fatura.Valor);
        Assert.True(fatura.EstaAberta());
        Assert.Equal(1, _unitOfWork.QuantidadeDeSalvamentos);
    }

    [Fact]
    public async Task DeveRetornarFalhaQuandoClienteNaoEncontrado()
    {
        var plano = SemearPlano(periodoDeTrialEmDias: 0);
        var servico = CriarServico();

        var resultado = await servico.ExecutarAsync(
            new CriarAssinaturaEntrada(Guid.NewGuid(), plano.Identificador));

        Assert.True(resultado.EhFalha);
        Assert.Empty(_unitOfWork.AssinaturaEmMemoria.Assinaturas);
        Assert.Equal(0, _unitOfWork.QuantidadeDeSalvamentos);
    }

    [Fact]
    public async Task DeveRetornarFalhaQuandoPlanoNaoEncontrado()
    {
        var cliente = SemearCliente();
        var servico = CriarServico();

        var resultado = await servico.ExecutarAsync(
            new CriarAssinaturaEntrada(cliente.Identificador, Guid.NewGuid()));

        Assert.True(resultado.EhFalha);
        Assert.Empty(_unitOfWork.AssinaturaEmMemoria.Assinaturas);
        Assert.Equal(0, _unitOfWork.QuantidadeDeSalvamentos);
    }

    [Fact]
    public async Task DeveRetornarFalhaParaComandoNulo()
    {
        var servico = CriarServico();

        var resultado = await servico.ExecutarAsync(null!);

        Assert.True(resultado.EhFalha);
        Assert.Empty(_unitOfWork.AssinaturaEmMemoria.Assinaturas);
    }
}
