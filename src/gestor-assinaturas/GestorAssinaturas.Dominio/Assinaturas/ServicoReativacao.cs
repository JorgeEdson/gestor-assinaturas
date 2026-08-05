using GestorAssinaturas.Dominio.Comum;
using GestorAssinaturas.Dominio.Faturas;

namespace GestorAssinaturas.Dominio.Assinaturas;

public sealed class ServicoReativacao
{
    public Resultado RegistrarPagamentoAprovado(Assinatura assinatura, Fatura fatura)
    {
        var resultadoDaValidacao = ValidarVinculo(assinatura, fatura);

        if (resultadoDaValidacao.EhFalha)
        {
            return resultadoDaValidacao;
        }

        var resultadoDoPagamento = fatura.MarcarComoPaga();

        if (resultadoDoPagamento.EhFalha)
        {
            return resultadoDoPagamento;
        }

        if (assinatura.EstaInadimplente())
        {
            return assinatura.Reativar();
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
