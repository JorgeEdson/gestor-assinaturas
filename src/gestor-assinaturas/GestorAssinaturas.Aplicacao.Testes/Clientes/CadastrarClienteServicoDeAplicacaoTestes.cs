using GestorAssinaturas.Aplicacao.Clientes;
using GestorAssinaturas.Aplicacao.Testes.Dobras;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GestorAssinaturas.Aplicacao.Testes.Clientes;

public class CadastrarClienteServicoDeAplicacaoTestes
{
    private readonly UnitOfWorkEmMemoria _unitOfWork = new();

    private CadastrarClienteApplicationService CriarServico()
    {
        return new CadastrarClienteApplicationService(
            _unitOfWork,
            NullLogger<CadastrarClienteApplicationService>.Instance);
    }

    [Fact]
    public async Task DeveCadastrarClienteValidoEPersistirComSalvamento()
    {
        var servico = CriarServico();
        var comando = new CadastrarClienteEntrada("Empresa Contratante", "contato@empresa.com");

        var resultado = await servico.ExecutarAsync(comando);

        Assert.True(resultado.EhSucesso);
        Assert.NotEqual(Guid.Empty, resultado.Instancia);
        Assert.Single(_unitOfWork.ClienteEmMemoria.Clientes);
        Assert.Equal(1, _unitOfWork.QuantidadeDeSalvamentos);
    }

    [Fact]
    public async Task DevePersistirOClienteComOIdentificadorRetornado()
    {
        var servico = CriarServico();
        var comando = new CadastrarClienteEntrada("Empresa Contratante", "contato@empresa.com");

        var resultado = await servico.ExecutarAsync(comando);

        var clientePersistido = Assert.Single(_unitOfWork.ClienteEmMemoria.Clientes);
        Assert.Equal(resultado.Instancia, clientePersistido.Identificador);
        Assert.Equal("contato@empresa.com", clientePersistido.Email.Endereco);
    }

    [Fact]
    public async Task DeveRetornarFalhaParaEmailInvalidoSemPersistir()
    {
        var servico = CriarServico();
        var comando = new CadastrarClienteEntrada("Empresa Contratante", "email-invalido");

        var resultado = await servico.ExecutarAsync(comando);

        Assert.True(resultado.EhFalha);
        Assert.Empty(_unitOfWork.ClienteEmMemoria.Clientes);
        Assert.Equal(0, _unitOfWork.QuantidadeDeSalvamentos);
    }

    [Fact]
    public async Task DeveRetornarFalhaParaNomeInvalidoSemPersistir()
    {
        var servico = CriarServico();
        var comando = new CadastrarClienteEntrada("AB", "contato@empresa.com");

        var resultado = await servico.ExecutarAsync(comando);

        Assert.True(resultado.EhFalha);
        Assert.Empty(_unitOfWork.ClienteEmMemoria.Clientes);
        Assert.Equal(0, _unitOfWork.QuantidadeDeSalvamentos);
    }

    [Fact]
    public async Task DeveRetornarFalhaParaComandoNulo()
    {
        var servico = CriarServico();

        var resultado = await servico.ExecutarAsync(null!);

        Assert.True(resultado.EhFalha);
        Assert.Empty(_unitOfWork.ClienteEmMemoria.Clientes);
    }
}
