using GestorAssinaturas.Dominio.ObjetosDeValor;
using Xunit;

namespace GestorAssinaturas.Dominio.Testes.ObjetosDeValor;

public class EmailTestes
{
    [Fact]
    public void DeveCriarEmailNormalizandoParaLetrasMinusculas()
    {
        var resultado = Email.Criar("  Contato@Empresa.COM  ");

        Assert.True(resultado.EhSucesso);
        Assert.Equal("contato@empresa.com", resultado.Instancia.Endereco);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("sem-arroba")]
    [InlineData("sem@dominio")]
    [InlineData("@empresa.com")]
    [InlineData("contato@@empresa.com")]
    [InlineData("contato empresa@teste.com")]
    public void DeveRetornarFalhaParaEmailInvalido(string emailInvalido)
    {
        var resultado = Email.Criar(emailInvalido);

        Assert.True(resultado.EhFalha);
    }

    [Fact]
    public void DeveRetornarFalhaParaEmailAcimaDoLimiteDeCaracteres()
    {
        var parteLocalExcessiva = new string('a', Email.QuantidadeMaximaDeCaracteres);
        var emailExcessivamenteLongo = $"{parteLocalExcessiva}@empresa.com";

        var resultado = Email.Criar(emailExcessivamenteLongo);

        Assert.True(resultado.EhFalha);
    }

    [Fact]
    public void DeveConsiderarIguaisDoisEmailsComOMesmoEndereco()
    {
        var primeiroEmail = Email.Criar("contato@empresa.com").Instancia;
        var segundoEmail = Email.Criar("CONTATO@empresa.com").Instancia;

        Assert.Equal(primeiroEmail, segundoEmail);
        Assert.True(primeiroEmail == segundoEmail);
    }
}
