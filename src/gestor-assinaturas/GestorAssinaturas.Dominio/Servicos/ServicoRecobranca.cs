using GestorAssinaturas.Dominio.Assinaturas;
using GestorAssinaturas.Dominio.Comum;
using GestorAssinaturas.Dominio.Faturas;

namespace GestorAssinaturas.Dominio.Servicos;

public sealed class ServicoRecobranca
{
    public Resultado<Fatura> EmitirFaturaDeRecobranca(
        Assinatura assinatura,
        IEnumerable<Fatura> faturasDaAssinatura,
        Guid identificadorDaFatura,
        DateOnly dataDeVencimento)
    {
        if (assinatura is null)
        {
            return Resultado<Fatura>.Falha("A assinatura é obrigatória para a recobrança.");
        }

        if (faturasDaAssinatura is null)
        {
            return Resultado<Fatura>.Falha("A coleção de faturas da assinatura é obrigatória para a recobrança.");
        }

        var possuiFaturaEmAberto = faturasDaAssinatura
            .Where(fatura => fatura.IdentificadorDaAssinatura == assinatura.Identificador)
            .Any(fatura => fatura.EstaAberta());

        if (possuiFaturaEmAberto)
        {
            return Resultado<Fatura>.Falha(
                "A assinatura já possui uma fatura em aberto aguardando pagamento; não é possível emitir outra recobrança.");
        }

        return assinatura.EmitirFaturaDeRecobranca(identificadorDaFatura, dataDeVencimento);
    }
}
