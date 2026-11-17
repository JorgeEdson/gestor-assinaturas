using GestorAssinaturas.Aplicacao.Clientes;
using GestorAssinaturas.Aplicacao.Testes.Dobras;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GestorAssinaturas.Aplicacao.Testes.Clientes;

public class CadastrarClienteServicoDeAplicacaoTestes
{
    private readonly UnidadeDeTrabalhoEmMemoria _unidadeDeTrabalho = new();

    private CadastrarClienteServicoDeAplicacao CriarServico()
    {
        return new CadastrarClienteServicoDeAplicacao(
            _unidadeDeTrabalho,
            NullLogger<CadastrarClienteServicoDeAplicacao>.Instance);
    }

    [Fact]
    public async Task DeveCadastrarClienteValidoEPersistirComSalvamento()
    {
        var servico = CriarServico();
        var entrada = new CadastrarClienteEntrada("Empresa Contratante", "contato@empresa.com");

        var resultado = await servico.ExecutarAsync(entrada);

        Assert.True(resultado.EhSucesso);
        Assert.NotEqual(Guid.Empty, resultado.Instancia);
        Assert.Single(_unidadeDeTrabalho.ClienteEmMemoria.Clientes);
        Assert.Equal(1, _unidadeDeTrabalho.QuantidadeDeSalvamentos);
    }

    [Fact]
    public async Task DevePersistirOClienteComOIdentificadorRetornado()
    {
        var servico = CriarServico();
        var entrada = new CadastrarClienteEntrada("Empresa Contratante", "contato@empresa.com");

        var resultado = await servico.ExecutarAsync(entrada);

        var clientePersistido = Assert.Single(_unidadeDeTrabalho.ClienteEmMemoria.Clientes);
        Assert.Equal(resultado.Instancia, clientePersistido.Identificador);
        Assert.Equal("contato@empresa.com", clientePersistido.Email.Endereco);
    }

    [Fact]
    public async Task DeveRetornarFalhaParaEmailInvalidoSemPersistir()
    {
        var servico = CriarServico();
        var entrada = new CadastrarClienteEntrada("Empresa Contratante", "email-invalido");

        var resultado = await servico.ExecutarAsync(entrada);

        Assert.True(resultado.EhFalha);
        Assert.Empty(_unidadeDeTrabalho.ClienteEmMemoria.Clientes);
        Assert.Equal(0, _unidadeDeTrabalho.QuantidadeDeSalvamentos);
    }

    [Fact]
    public async Task DeveRetornarFalhaParaNomeInvalidoSemPersistir()
    {
        var servico = CriarServico();
        var entrada = new CadastrarClienteEntrada("AB", "contato@empresa.com");

        var resultado = await servico.ExecutarAsync(entrada);

        Assert.True(resultado.EhFalha);
        Assert.Empty(_unidadeDeTrabalho.ClienteEmMemoria.Clientes);
        Assert.Equal(0, _unidadeDeTrabalho.QuantidadeDeSalvamentos);
    }

    [Fact]
    public async Task DeveRetornarFalhaParaEntradaNula()
    {
        var servico = CriarServico();

        var resultado = await servico.ExecutarAsync(null!);

        Assert.True(resultado.EhFalha);
        Assert.Empty(_unidadeDeTrabalho.ClienteEmMemoria.Clientes);
    }
}
