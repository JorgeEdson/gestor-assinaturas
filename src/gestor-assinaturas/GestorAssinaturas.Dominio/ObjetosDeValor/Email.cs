using System.Text.RegularExpressions;
using GestorAssinaturas.Dominio.Comum;

namespace GestorAssinaturas.Dominio.ObjetosDeValor;

public sealed partial class Email : ObjetoDeValor
{
    public const int QuantidadeMaximaDeCaracteres = 254;

    private Email(string endereco)
    {
        Endereco = endereco;
    }

    public string Endereco { get; }

    public static Resultado<Email> Criar(string endereco)
    {
        if (string.IsNullOrWhiteSpace(endereco))
        {
            return Resultado<Email>.Falha("O endereço de e-mail é obrigatório.");
        }

        var enderecoNormalizado = endereco.Trim().ToLowerInvariant();

        if (enderecoNormalizado.Length > QuantidadeMaximaDeCaracteres)
        {
            return Resultado<Email>.Falha(
                $"O endereço de e-mail deve possuir no máximo {QuantidadeMaximaDeCaracteres} caracteres.");
        }

        if (!ObterExpressaoDeValidacao().IsMatch(enderecoNormalizado))
        {
            return Resultado<Email>.Falha("O endereço de e-mail informado é inválido.");
        }

        return Resultado<Email>.Sucesso(new Email(enderecoNormalizado));
    }

    public override string ToString()
    {
        return Endereco;
    }

    protected override IEnumerable<object?> ObterComponentesDeIgualdade()
    {
        yield return Endereco;
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex ObterExpressaoDeValidacao();
}
