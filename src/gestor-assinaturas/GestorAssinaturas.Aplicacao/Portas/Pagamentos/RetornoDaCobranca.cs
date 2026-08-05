namespace GestorAssinaturas.Aplicacao.Portas.Pagamentos;

public sealed record RetornoDaCobranca(
    SituacaoDoPagamento Situacao,
    string? IdentificadorDaTransacao,
    string? MotivoDaRecusa)
{
    public bool FoiAprovado => Situacao == SituacaoDoPagamento.Aprovado;

    public bool FoiRecusado => Situacao == SituacaoDoPagamento.Recusado;

    public static RetornoDaCobranca Aprovado(string identificadorDaTransacao)
    {
        return new RetornoDaCobranca(SituacaoDoPagamento.Aprovado, identificadorDaTransacao, MotivoDaRecusa: null);
    }

    public static RetornoDaCobranca Recusado(string motivoDaRecusa)
    {
        return new RetornoDaCobranca(SituacaoDoPagamento.Recusado, IdentificadorDaTransacao: null, motivoDaRecusa);
    }
}
