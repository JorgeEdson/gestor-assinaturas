using GestorAssinaturas.Dominio.Comum;
using GestorAssinaturas.Dominio.Faturas;
using GestorAssinaturas.Dominio.Planos;

namespace GestorAssinaturas.Dominio.Assinaturas;

public sealed class ServicoTrocaDePlano
{
    public Resultado TrocarPlano(
        Assinatura assinatura,
        Plano novoPlano,
        IEnumerable<Fatura> faturasDaAssinatura)
    {
        if (assinatura is null)
        {
            return Resultado.Falha("A assinatura é obrigatória para a troca de plano.");
        }

        if (faturasDaAssinatura is null)
        {
            return Resultado.Falha("A coleção de faturas da assinatura é obrigatória para a troca de plano.");
        }

        var resultadoDaTroca = assinatura.TrocarPlano(novoPlano);

        if (resultadoDaTroca.EhFalha)
        {
            return resultadoDaTroca;
        }

        var faturasEmAbertoDaAssinatura = faturasDaAssinatura
            .Where(fatura => fatura.IdentificadorDaAssinatura == assinatura.Identificador)
            .Where(fatura => fatura.EstaAberta())
            .ToList();

        foreach (var faturaEmAberto in faturasEmAbertoDaAssinatura)
        {
            var resultadoDaReprecificacao = faturaEmAberto.AtualizarValor(novoPlano.Preco);

            if (resultadoDaReprecificacao.EhFalha)
            {
                return resultadoDaReprecificacao;
            }
        }

        return Resultado.Sucesso();
    }
}
