using GestorAssinaturas.Dominio.Assinaturas;
using GestorAssinaturas.Dominio.Comum;
using GestorAssinaturas.Dominio.Faturas;

namespace GestorAssinaturas.Dominio.Servicos;

public sealed class ServicoReativacao
{
    public Resultado RegistrarPagamentoAprovado(Assinatura assinatura, Fatura fatura)
    {
        if (assinatura is null)
        {
            return Resultado.Falha("A assinatura é obrigatória para o registro do pagamento.");
        }

        var resultadoDaVerificacao = assinatura.VerificarSePodeReceberPagamentoDa(fatura);

        if (resultadoDaVerificacao.EhFalha)
        {
            return resultadoDaVerificacao;
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
}
