using GestorAssinaturas.Dominio.Comum;
using GestorAssinaturas.Dominio.ObjetosDeValor;

namespace GestorAssinaturas.Dominio.Faturas;

public sealed class Fatura : Entidade
{
    private Fatura(
        Guid identificador,
        Guid identificadorDaAssinatura,
        Dinheiro valor,
        DateOnly dataDeVencimento,
        StatusFatura status)
        : base(identificador)
    {
        IdentificadorDaAssinatura = identificadorDaAssinatura;
        Valor = valor;
        DataDeVencimento = dataDeVencimento;
        Status = status;
    }

    public Guid IdentificadorDaAssinatura { get; }

    public Dinheiro Valor { get; private set; }

    public DateOnly DataDeVencimento { get; }

    public StatusFatura Status { get; private set; }

    public static Resultado<Fatura> Emitir(
        Guid identificador,
        Guid identificadorDaAssinatura,
        Dinheiro valor,
        DateOnly dataDeVencimento)
    {
        var resultadoDaValidacao = Resultado.Combinar(
            ValidarIdentificador(identificador),
            ValidarIdentificadorDaAssinatura(identificadorDaAssinatura),
            ValidarValor(valor));

        if (resultadoDaValidacao.EhFalha)
        {
            return Resultado<Fatura>.Falha(resultadoDaValidacao.Erros!);
        }

        var fatura = new Fatura(
            identificador,
            identificadorDaAssinatura,
            valor,
            dataDeVencimento,
            StatusFatura.Aberta);

        return Resultado<Fatura>.Sucesso(fatura);
    }

    public bool EstaAberta()
    {
        return Status == StatusFatura.Aberta;
    }

    public bool EstaPaga()
    {
        return Status == StatusFatura.Paga;
    }

    public Resultado MarcarComoPaga()
    {
        if (!EstaAberta())
        {
            return Resultado.Falha(
                $"Apenas uma fatura em aberto pode ser marcada como paga. Status atual: {Status}.");
        }

        Status = StatusFatura.Paga;

        return Resultado.Sucesso();
    }

    public Resultado MarcarComoFalha()
    {
        if (!EstaAberta())
        {
            return Resultado.Falha(
                $"Apenas uma fatura em aberto pode ser marcada como falha. Status atual: {Status}.");
        }

        Status = StatusFatura.Falha;

        return Resultado.Sucesso();
    }

    public Resultado AtualizarValor(Dinheiro novoValor)
    {
        var resultadoDaValidacao = ValidarValor(novoValor);

        if (resultadoDaValidacao.EhFalha)
        {
            return resultadoDaValidacao;
        }

        if (!EstaAberta())
        {
            return Resultado.Falha(
                $"Apenas uma fatura em aberto pode ter o valor atualizado. Status atual: {Status}.");
        }

        Valor = novoValor;

        return Resultado.Sucesso();
    }

    public override string ToString()
    {
        return $"Fatura {Identificador} - {Valor} ({Status})";
    }

    private static Resultado ValidarIdentificadorDaAssinatura(Guid identificadorDaAssinatura)
    {
        return Resultado.FalhaQuando(
            identificadorDaAssinatura == Guid.Empty,
            "A fatura deve estar vinculada a uma assinatura válida.");
    }

    private static Resultado ValidarValor(Dinheiro valor)
    {
        if (valor is null)
        {
            return Resultado.Falha("O valor da fatura é obrigatório.");
        }

        return Resultado.FalhaQuando(
            valor.EhZero(),
            "O valor da fatura deve ser maior que zero.");
    }
}
