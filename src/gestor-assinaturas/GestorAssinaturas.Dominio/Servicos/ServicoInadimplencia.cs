using GestorAssinaturas.Dominio.Assinaturas;
using GestorAssinaturas.Dominio.Comum;
using GestorAssinaturas.Dominio.Faturas;

namespace GestorAssinaturas.Dominio.Servicos;

public sealed class ServicoInadimplencia
{
    public Resultado RegistrarPagamentoRecusado(Assinatura assinatura, Fatura fatura)
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
}
