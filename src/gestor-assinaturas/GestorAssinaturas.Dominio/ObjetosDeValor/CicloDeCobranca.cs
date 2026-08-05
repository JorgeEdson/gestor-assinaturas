using GestorAssinaturas.Dominio.Comum;

namespace GestorAssinaturas.Dominio.ObjetosDeValor;

public sealed class CicloDeCobranca : ObjetoDeValor
{
    private const int QuantidadeDeMesesDoCicloMensal = 1;
    private const int QuantidadeDeMesesDoCicloAnual = 12;

    private CicloDeCobranca(TipoDeCicloDeCobranca tipo, int quantidadeDeMeses)
    {
        Tipo = tipo;
        QuantidadeDeMeses = quantidadeDeMeses;
    }

    public static CicloDeCobranca Mensal { get; } =
        new(TipoDeCicloDeCobranca.Mensal, QuantidadeDeMesesDoCicloMensal);

    public static CicloDeCobranca Anual { get; } =
        new(TipoDeCicloDeCobranca.Anual, QuantidadeDeMesesDoCicloAnual);

    public TipoDeCicloDeCobranca Tipo { get; }

    public int QuantidadeDeMeses { get; }

    public static Resultado<CicloDeCobranca> APartirDoTipo(TipoDeCicloDeCobranca tipo)
    {
        return tipo switch
        {
            TipoDeCicloDeCobranca.Mensal => Resultado<CicloDeCobranca>.Sucesso(Mensal),
            TipoDeCicloDeCobranca.Anual => Resultado<CicloDeCobranca>.Sucesso(Anual),
            _ => Resultado<CicloDeCobranca>.Falha($"Ciclo de cobrança não suportado: {tipo}.")
        };
    }

    public DateOnly CalcularProximaDataDeVencimento(DateOnly dataDeReferencia)
    {
        return dataDeReferencia.AddMonths(QuantidadeDeMeses);
    }

    public override string ToString()
    {
        return Tipo.ToString();
    }

    protected override IEnumerable<object?> ObterComponentesDeIgualdade()
    {
        yield return Tipo;
    }
}
