using GestorAssinaturas.Dominio.Comum;
using GestorAssinaturas.Dominio.ObjetosDeValor;

namespace GestorAssinaturas.Dominio.Planos;

public sealed class Plano : Entidade
{
    public const int QuantidadeMinimaDeCaracteresDoNome = 3;
    public const int QuantidadeMaximaDeCaracteresDoNome = 100;
    public const int QuantidadeMaximaDeDiasDePeriodoDeTrial = 365;

    private Plano(
        Guid identificador,
        string nome,
        Dinheiro preco,
        CicloDeCobranca cicloDeCobranca,
        int periodoDeTrialEmDias)
        : base(identificador)
    {
        Nome = nome;
        Preco = preco;
        CicloDeCobranca = cicloDeCobranca;
        PeriodoDeTrialEmDias = periodoDeTrialEmDias;
    }

    public string Nome { get; }

    public Dinheiro Preco { get; }

    public CicloDeCobranca CicloDeCobranca { get; }

    public int PeriodoDeTrialEmDias { get; }

    public static Resultado<Plano> Criar(
        Guid identificador,
        string nome,
        Dinheiro preco,
        CicloDeCobranca cicloDeCobranca,
        int periodoDeTrialEmDias)
    {
        var resultadoDaValidacao = Resultado.Combinar(
            ValidarIdentificador(identificador),
            ValidarNome(nome),
            ValidarPreco(preco),
            ValidarCicloDeCobranca(cicloDeCobranca),
            ValidarPeriodoDeTrial(periodoDeTrialEmDias));

        if (resultadoDaValidacao.EhFalha)
        {
            return Resultado<Plano>.Falha(resultadoDaValidacao.Erros!);
        }

        var plano = new Plano(
            identificador,
            nome.Trim(),
            preco,
            cicloDeCobranca,
            periodoDeTrialEmDias);

        return Resultado<Plano>.Sucesso(plano);
    }

    public bool PossuiPeriodoDeTrial()
    {
        return PeriodoDeTrialEmDias > 0;
    }

    public Resultado<DateOnly> CalcularDataDeTerminoDoPeriodoDeTrial(DateOnly dataDeInicioDoPeriodoDeTrial)
    {
        if (!PossuiPeriodoDeTrial())
        {
            return Resultado<DateOnly>.Falha(
                "Não é possível calcular o término do período de trial em um plano sem período de trial.");
        }

        return Resultado<DateOnly>.Sucesso(dataDeInicioDoPeriodoDeTrial.AddDays(PeriodoDeTrialEmDias));
    }

    public DateOnly CalcularDataDeVencimentoDoProximoCiclo(DateOnly dataDeReferencia)
    {
        return CicloDeCobranca.CalcularProximaDataDeVencimento(dataDeReferencia);
    }

    public override string ToString()
    {
        return $"{Nome} ({Preco} / {CicloDeCobranca})";
    }

    private static Resultado ValidarNome(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
        {
            return Resultado.Falha("O nome do plano é obrigatório.");
        }

        var nomeNormalizado = nome.Trim();

        if (nomeNormalizado.Length < QuantidadeMinimaDeCaracteresDoNome)
        {
            return Resultado.Falha(
                $"O nome do plano deve possuir ao menos {QuantidadeMinimaDeCaracteresDoNome} caracteres.");
        }

        if (nomeNormalizado.Length > QuantidadeMaximaDeCaracteresDoNome)
        {
            return Resultado.Falha(
                $"O nome do plano deve possuir no máximo {QuantidadeMaximaDeCaracteresDoNome} caracteres.");
        }

        return Resultado.Sucesso();
    }

    private static Resultado ValidarPreco(Dinheiro preco)
    {
        if (preco is null)
        {
            return Resultado.Falha("O preço do plano é obrigatório.");
        }

        return Resultado.FalhaQuando(
            preco.EhZero(),
            "O preço do plano deve ser maior que zero.");
    }

    private static Resultado ValidarCicloDeCobranca(CicloDeCobranca cicloDeCobranca)
    {
        return Resultado.FalhaQuando(
            cicloDeCobranca is null,
            "O ciclo de cobrança do plano é obrigatório.");
    }

    private static Resultado ValidarPeriodoDeTrial(int periodoDeTrialEmDias)
    {
        if (periodoDeTrialEmDias < 0)
        {
            return Resultado.Falha("O período de trial do plano não pode ser negativo.");
        }

        return Resultado.FalhaQuando(
            periodoDeTrialEmDias > QuantidadeMaximaDeDiasDePeriodoDeTrial,
            $"O período de trial do plano não pode ultrapassar {QuantidadeMaximaDeDiasDePeriodoDeTrial} dias.");
    }
}
