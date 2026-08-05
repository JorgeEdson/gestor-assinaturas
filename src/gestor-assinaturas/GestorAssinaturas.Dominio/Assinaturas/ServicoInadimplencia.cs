using GestorAssinaturas.Dominio.Comum;
using GestorAssinaturas.Dominio.Faturas;

namespace GestorAssinaturas.Dominio.Assinaturas;

public sealed class ServicoInadimplencia
{
    public Resultado RegistrarPagamentoRecusado(Assinatura assinatura, Fatura fatura)
    {
        var resultadoDaValidacao = ValidarVinculo(assinatura, fatura);

        if (resultadoDaValidacao.EhFalha)
        {
            return resultadoDaValidacao;
        }

        if (assinatura.EstaCancelada())
        {
            return Resultado.Falha("Uma assinatura cancelada não aceita novo pagamento.");
        }

        var resultadoDaRecusa = fatura.MarcarComoFalha();

        if (resultadoDaRecusa.EhFalha)
        {
            return resultadoDaRecusa;
        }

        if (assinatura.EstaAtiva())
        {
            return assinatura.RegistrarInadimplencia();
        }

        return Resultado.Sucesso();
    }

    private static Resultado ValidarVinculo(Assinatura assinatura, Fatura fatura)
    {
        if (assinatura is null)
        {
            return Resultado.Falha("A assinatura é obrigatória para o registro do pagamento.");
        }

        if (fatura is null)
        {
            return Resultado.Falha("A fatura é obrigatória para o registro do pagamento.");
        }

        return Resultado.FalhaQuando(
            fatura.IdentificadorDaAssinatura != assinatura.Identificador,
            "A fatura informada não pertence à assinatura.");
    }
}
