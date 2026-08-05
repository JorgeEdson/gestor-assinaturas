using GestorAssinaturas.Dominio.Clientes;
using GestorAssinaturas.Dominio.ObjetosDeValor;
using Xunit;

namespace GestorAssinaturas.Dominio.Testes.Clientes;

public class ClienteTestes
{
    private static readonly Guid IdentificadorDoCliente = Guid.NewGuid();

    private static Email EmailValido => Email.Criar("contato@empresa.com").Instancia;

    [Fact]
    public void DeveCadastrarClienteComNomeEEmailInformados()
    {
        var resultado = Cliente.Criar(IdentificadorDoCliente, "Empresa Contratante", EmailValido);

        Assert.True(resultado.EhSucesso);

        var cliente = resultado.Instancia;

        Assert.Equal(IdentificadorDoCliente, cliente.Identificador);
        Assert.Equal("Empresa Contratante", cliente.Nome);
        Assert.Equal(EmailValido, cliente.Email);
    }

    [Fact]
    public void DeveRemoverEspacosEmBrancoDoNomeDoCliente()
    {
        var resultado = Cliente.Criar(IdentificadorDoCliente, "  Empresa Contratante  ", EmailValido);

        Assert.True(resultado.EhSucesso);
        Assert.Equal("Empresa Contratante", resultado.Instancia.Nome);
    }

    [Fact]
    public void DeveRetornarFalhaParaIdentificadorVazio()
    {
        var resultado = Cliente.Criar(Guid.Empty, "Empresa Contratante", EmailValido);

        Assert.True(resultado.EhFalha);
        Assert.Contains(resultado.Erros!, erro => erro.Contains("identificador"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("AB")]
    public void DeveRetornarFalhaParaNomeInvalido(string nomeInvalido)
    {
        var resultado = Cliente.Criar(IdentificadorDoCliente, nomeInvalido, EmailValido);

        Assert.True(resultado.EhFalha);
    }

    [Fact]
    public void DeveRetornarFalhaParaEmailNaoInformado()
    {
        var resultado = Cliente.Criar(IdentificadorDoCliente, "Empresa Contratante", null!);

        Assert.True(resultado.EhFalha);
        Assert.Contains(resultado.Erros!, erro => erro.Contains("e-mail"));
    }

    [Fact]
    public void DeveAcumularTodosOsErrosDeValidacaoEmUmUnicoResultado()
    {
        var resultado = Cliente.Criar(Guid.Empty, string.Empty, null!);

        Assert.True(resultado.EhFalha);
        Assert.Equal(3, resultado.Erros!.Count());
    }

    [Fact]
    public void DeveConsiderarIguaisDoisClientesComOMesmoIdentificador()
    {
        var primeiroCliente = Cliente.Criar(IdentificadorDoCliente, "Empresa Contratante", EmailValido).Instancia;
        var segundoCliente = Cliente.Criar(
            IdentificadorDoCliente,
            "Empresa Renomeada",
            Email.Criar("novo@empresa.com").Instancia).Instancia;

        Assert.Equal(primeiroCliente, segundoCliente);
    }
}
