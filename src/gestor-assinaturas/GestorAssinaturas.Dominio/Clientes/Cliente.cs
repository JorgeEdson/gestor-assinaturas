using GestorAssinaturas.Dominio.Comum;
using GestorAssinaturas.Dominio.ObjetosDeValor;

namespace GestorAssinaturas.Dominio.Clientes;

public sealed class Cliente : Entidade
{
    public const int QuantidadeMinimaDeCaracteresDoNome = 3;
    public const int QuantidadeMaximaDeCaracteresDoNome = 150;

    private Cliente(Guid identificador, string nome, Email email)
        : base(identificador)
    {
        Nome = nome;
        Email = email;
    }

    public string Nome { get; }

    public Email Email { get; }

    public static Resultado<Cliente> Criar(Guid identificador, string nome, Email email)
    {
        var resultadoDaValidacao = Resultado.Combinar(
            ValidarIdentificador(identificador),
            ValidarNome(nome),
            ValidarEmail(email));

        if (resultadoDaValidacao.EhFalha)
        {
            return Resultado<Cliente>.Falha(resultadoDaValidacao.Erros!);
        }

        var cliente = new Cliente(identificador, nome.Trim(), email);

        return Resultado<Cliente>.Sucesso(cliente);
    }

    public override string ToString()
    {
        return $"{Nome} <{Email}>";
    }

    private static Resultado ValidarNome(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
        {
            return Resultado.Falha("O nome do cliente é obrigatório.");
        }

        var nomeNormalizado = nome.Trim();

        if (nomeNormalizado.Length < QuantidadeMinimaDeCaracteresDoNome)
        {
            return Resultado.Falha(
                $"O nome do cliente deve possuir ao menos {QuantidadeMinimaDeCaracteresDoNome} caracteres.");
        }

        if (nomeNormalizado.Length > QuantidadeMaximaDeCaracteresDoNome)
        {
            return Resultado.Falha(
                $"O nome do cliente deve possuir no máximo {QuantidadeMaximaDeCaracteresDoNome} caracteres.");
        }

        return Resultado.Sucesso();
    }

    private static Resultado ValidarEmail(Email email)
    {
        return Resultado.FalhaQuando(
            email is null,
            "O e-mail de contato do cliente é obrigatório.");
    }
}
