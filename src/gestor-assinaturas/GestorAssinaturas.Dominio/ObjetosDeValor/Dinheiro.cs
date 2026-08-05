using System.Globalization;
using GestorAssinaturas.Dominio.Comum;

namespace GestorAssinaturas.Dominio.ObjetosDeValor;

public sealed class Dinheiro : ObjetoDeValor
{
    public const int QuantidadeDeCasasDecimais = 2;
    public const int QuantidadeDeCaracteresDaMoeda = 3;

    private Dinheiro(decimal valor, string moeda)
    {
        Valor = valor;
        Moeda = moeda;
    }

    public decimal Valor { get; }

    public string Moeda { get; }

    public static Resultado<Dinheiro> Criar(decimal valor, string moeda)
    {
        var resultadoDaValidacaoDaMoeda = ValidarMoeda(moeda);

        var resultadoDaValidacao = Resultado.Combinar(
            resultadoDaValidacaoDaMoeda,
            ValidarValor(valor));

        if (resultadoDaValidacao.EhFalha)
        {
            return Resultado<Dinheiro>.Falha(resultadoDaValidacao.Erros!);
        }

        var moedaNormalizada = NormalizarMoeda(moeda);
        var valorArredondado = decimal.Round(valor, QuantidadeDeCasasDecimais, MidpointRounding.AwayFromZero);

        return Resultado<Dinheiro>.Sucesso(new Dinheiro(valorArredondado, moedaNormalizada));
    }

    public static Resultado<Dinheiro> Zero(string moeda)
    {
        return Criar(decimal.Zero, moeda);
    }

    public Resultado<Dinheiro> Somar(Dinheiro outroValorMonetario)
    {
        var resultadoDaValidacao = ValidarCompatibilidadeDeMoeda(outroValorMonetario);

        if (resultadoDaValidacao.EhFalha)
        {
            return Resultado<Dinheiro>.Falha(resultadoDaValidacao.Erros!);
        }

        return Criar(Valor + outroValorMonetario.Valor, Moeda);
    }

    public Resultado<Dinheiro> Subtrair(Dinheiro outroValorMonetario)
    {
        var resultadoDaValidacao = ValidarCompatibilidadeDeMoeda(outroValorMonetario);

        if (resultadoDaValidacao.EhFalha)
        {
            return Resultado<Dinheiro>.Falha(resultadoDaValidacao.Erros!);
        }

        if (outroValorMonetario.Valor > Valor)
        {
            return Resultado<Dinheiro>.Falha("A subtração resultaria em um valor monetário negativo.");
        }

        return Criar(Valor - outroValorMonetario.Valor, Moeda);
    }

    public Resultado<Dinheiro> MultiplicarPor(decimal multiplicador)
    {
        if (multiplicador < decimal.Zero)
        {
            return Resultado<Dinheiro>.Falha("O multiplicador de um valor monetário não pode ser negativo.");
        }

        return Criar(Valor * multiplicador, Moeda);
    }

    public Resultado<bool> EhMaiorQue(Dinheiro outroValorMonetario)
    {
        var resultadoDaValidacao = ValidarCompatibilidadeDeMoeda(outroValorMonetario);

        if (resultadoDaValidacao.EhFalha)
        {
            return Resultado<bool>.Falha(resultadoDaValidacao.Erros!);
        }

        return Resultado<bool>.Sucesso(Valor > outroValorMonetario.Valor);
    }

    public Resultado<bool> EhMenorQue(Dinheiro outroValorMonetario)
    {
        var resultadoDaValidacao = ValidarCompatibilidadeDeMoeda(outroValorMonetario);

        if (resultadoDaValidacao.EhFalha)
        {
            return Resultado<bool>.Falha(resultadoDaValidacao.Erros!);
        }

        return Resultado<bool>.Sucesso(Valor < outroValorMonetario.Valor);
    }

    public bool EhZero()
    {
        return Valor == decimal.Zero;
    }

    public bool PossuiMesmaMoedaQue(Dinheiro outroValorMonetario)
    {
        return outroValorMonetario is not null && Moeda == outroValorMonetario.Moeda;
    }

    public override string ToString()
    {
        return $"{Moeda} {Valor.ToString($"F{QuantidadeDeCasasDecimais}", CultureInfo.InvariantCulture)}";
    }

    protected override IEnumerable<object?> ObterComponentesDeIgualdade()
    {
        yield return Valor;
        yield return Moeda;
    }

    private static string NormalizarMoeda(string moeda)
    {
        return moeda.Trim().ToUpperInvariant();
    }

    private static Resultado ValidarMoeda(string moeda)
    {
        if (string.IsNullOrWhiteSpace(moeda))
        {
            return Resultado.Falha("A moeda é obrigatória para a criação de um valor monetário.");
        }

        var moedaNormalizada = NormalizarMoeda(moeda);

        if (moedaNormalizada.Length != QuantidadeDeCaracteresDaMoeda)
        {
            return Resultado.Falha(
                $"A moeda deve possuir exatamente {QuantidadeDeCaracteresDaMoeda} caracteres no padrão ISO 4217.");
        }

        if (!moedaNormalizada.All(char.IsLetter))
        {
            return Resultado.Falha("A moeda deve conter apenas letras no padrão ISO 4217.");
        }

        return Resultado.Sucesso();
    }

    private static Resultado ValidarValor(decimal valor)
    {
        return Resultado.FalhaQuando(
            valor < decimal.Zero,
            "Um valor monetário não pode ser negativo.");
    }

    private Resultado ValidarCompatibilidadeDeMoeda(Dinheiro outroValorMonetario)
    {
        if (outroValorMonetario is null)
        {
            return Resultado.Falha("O valor monetário informado para a operação é obrigatório.");
        }

        return Resultado.FalhaQuando(
            !PossuiMesmaMoedaQue(outroValorMonetario),
            $"Não é possível operar valores monetários de moedas diferentes: {Moeda} e {outroValorMonetario.Moeda}.");
    }
}
