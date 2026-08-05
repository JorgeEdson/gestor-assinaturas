using GestorAssinaturas.Dominio.Comum;
using GestorAssinaturas.Dominio.Faturas;
using GestorAssinaturas.Dominio.Planos;

namespace GestorAssinaturas.Dominio.Assinaturas;

public sealed class Assinatura : Entidade
{
    private static readonly IReadOnlySet<(StatusAssinatura Origem, StatusAssinatura Destino)> TransicoesPermitidas =
        new HashSet<(StatusAssinatura, StatusAssinatura)>
        {
            (StatusAssinatura.Trial, StatusAssinatura.Ativa),
            (StatusAssinatura.Trial, StatusAssinatura.Cancelada),
            (StatusAssinatura.Ativa, StatusAssinatura.Inadimplente),
            (StatusAssinatura.Ativa, StatusAssinatura.Cancelada),
            (StatusAssinatura.Inadimplente, StatusAssinatura.Ativa),
            (StatusAssinatura.Inadimplente, StatusAssinatura.Suspensa),
            (StatusAssinatura.Suspensa, StatusAssinatura.Cancelada)
        };

    private Assinatura(
        Guid identificador,
        Guid identificadorDoCliente,
        Plano plano,
        StatusAssinatura status,
        DateOnly dataDeInicio,
        DateOnly? dataDeTerminoDoTrial)
        : base(identificador)
    {
        IdentificadorDoCliente = identificadorDoCliente;
        Plano = plano;
        Status = status;
        DataDeInicio = dataDeInicio;
        DataDeTerminoDoTrial = dataDeTerminoDoTrial;
    }

    public Guid IdentificadorDoCliente { get; }

    public Plano Plano { get; private set; }

    public StatusAssinatura Status { get; private set; }

    public DateOnly DataDeInicio { get; }

    public DateOnly? DataDeTerminoDoTrial { get; }

    public DateOnly? DataDeCancelamentoAgendado { get; private set; }

    public Guid IdentificadorDoPlano => Plano.Identificador;

    public bool PossuiCancelamentoAgendado => DataDeCancelamentoAgendado is not null;

    public static Resultado<Assinatura> Criar(
        Guid identificador,
        Guid identificadorDoCliente,
        Plano plano,
        DateOnly dataDeInicio)
    {
        var resultadoDaValidacao = Resultado.Combinar(
            ValidarIdentificador(identificador),
            ValidarIdentificadorDoCliente(identificadorDoCliente),
            ValidarPlano(plano));

        if (resultadoDaValidacao.EhFalha)
        {
            return Resultado<Assinatura>.Falha(resultadoDaValidacao.Erros!);
        }

        if (plano.PossuiPeriodoDeTrial())
        {
            var dataDeTerminoDoTrial = plano.CalcularDataDeTerminoDoPeriodoDeTrial(dataDeInicio).Instancia;

            var assinaturaEmTrial = new Assinatura(
                identificador,
                identificadorDoCliente,
                plano,
                StatusAssinatura.Trial,
                dataDeInicio,
                dataDeTerminoDoTrial);

            return Resultado<Assinatura>.Sucesso(assinaturaEmTrial);
        }

        var assinaturaAtiva = new Assinatura(
            identificador,
            identificadorDoCliente,
            plano,
            StatusAssinatura.Ativa,
            dataDeInicio,
            dataDeTerminoDoTrial: null);

        return Resultado<Assinatura>.Sucesso(assinaturaAtiva);
    }

    public bool EstaEmTrial()
    {
        return Status == StatusAssinatura.Trial;
    }

    public bool EstaAtiva()
    {
        return Status == StatusAssinatura.Ativa;
    }

    public bool EstaInadimplente()
    {
        return Status == StatusAssinatura.Inadimplente;
    }

    public bool EstaSuspensa()
    {
        return Status == StatusAssinatura.Suspensa;
    }

    public bool EstaCancelada()
    {
        return Status == StatusAssinatura.Cancelada;
    }

    public bool PrecisaDeCobrancaImediata()
    {
        return EstaAtiva() && DataDeTerminoDoTrial is null;
    }

    public Resultado Ativar()
    {
        return Transicionar(StatusAssinatura.Ativa);
    }

    public Resultado RegistrarInadimplencia()
    {
        return Transicionar(StatusAssinatura.Inadimplente);
    }

    public Resultado Reativar()
    {
        return Transicionar(StatusAssinatura.Ativa);
    }

    public Resultado Suspender()
    {
        return Transicionar(StatusAssinatura.Suspensa);
    }

    public Resultado CancelarImediatamente()
    {
        var resultadoDoCancelamento = Transicionar(StatusAssinatura.Cancelada);

        if (resultadoDoCancelamento.EhFalha)
        {
            return resultadoDoCancelamento;
        }

        DataDeCancelamentoAgendado = null;

        return Resultado.Sucesso();
    }

    public Resultado AgendarCancelamentoAoFimDoPeriodo(DateOnly dataDeFimDoPeriodoVigente)
    {
        if (EstaCancelada())
        {
            return Resultado.Falha(
                "Uma assinatura cancelada é um estado terminal e não aceita agendamento de cancelamento.");
        }

        if (!EstaAtiva() && !EstaEmTrial())
        {
            return Resultado.Falha(
                $"Somente uma assinatura ativa ou em trial pode agendar o cancelamento ao fim do período. Status atual: {Status}.");
        }

        if (PossuiCancelamentoAgendado)
        {
            return Resultado.Falha("A assinatura já possui um cancelamento agendado.");
        }

        if (dataDeFimDoPeriodoVigente < DataDeInicio)
        {
            return Resultado.Falha(
                "A data de fim do período vigente não pode ser anterior ao início da assinatura.");
        }

        DataDeCancelamentoAgendado = dataDeFimDoPeriodoVigente;

        return Resultado.Sucesso();
    }

    public Resultado EfetivarCancelamentoAgendado(DateOnly dataDeReferencia)
    {
        if (!PossuiCancelamentoAgendado)
        {
            return Resultado.Falha("A assinatura não possui um cancelamento agendado.");
        }

        if (dataDeReferencia < DataDeCancelamentoAgendado!.Value)
        {
            return Resultado.Falha("O período vigente da assinatura ainda não terminou.");
        }

        return Transicionar(StatusAssinatura.Cancelada);
    }

    public Resultado TrocarPlano(Plano novoPlano)
    {
        if (novoPlano is null)
        {
            return Resultado.Falha("O novo plano informado para a troca é obrigatório.");
        }

        if (!EstaAtiva())
        {
            return Resultado.Falha(
                $"Somente uma assinatura ativa pode trocar de plano. Status atual: {Status}.");
        }

        if (novoPlano.Identificador == Plano.Identificador)
        {
            return Resultado.Falha("O novo plano deve ser diferente do plano atual da assinatura.");
        }

        if (!novoPlano.Preco.PossuiMesmaMoedaQue(Plano.Preco))
        {
            return Resultado.Falha(
                $"A troca de plano não pode alterar a moeda da assinatura: de {Plano.Preco.Moeda} para {novoPlano.Preco.Moeda}.");
        }

        Plano = novoPlano;

        return Resultado.Sucesso();
    }

    public Resultado<Fatura> GerarFaturaDeCobranca(Guid identificadorDaFatura, DateOnly dataDeVencimento)
    {
        if (EstaCancelada())
        {
            return Resultado<Fatura>.Falha("Uma assinatura cancelada não pode gerar cobranças.");
        }

        if (!EstaAtiva())
        {
            return Resultado<Fatura>.Falha(
                $"Somente uma assinatura ativa pode gerar cobranças. Status atual: {Status}.");
        }

        return Fatura.Emitir(identificadorDaFatura, Identificador, Plano.Preco, dataDeVencimento);
    }

    public override string ToString()
    {
        return $"Assinatura {Identificador} - {Status}";
    }

    private Resultado Transicionar(StatusAssinatura statusDestino)
    {
        if (EstaCancelada())
        {
            return Resultado.Falha(
                "Uma assinatura cancelada é um estado terminal e não aceita novas transições.");
        }

        if (!TransicoesPermitidas.Contains((Status, statusDestino)))
        {
            return Resultado.Falha(
                $"Transição de status inválida: de {Status} para {statusDestino}.");
        }

        Status = statusDestino;

        return Resultado.Sucesso();
    }

    private static Resultado ValidarIdentificadorDoCliente(Guid identificadorDoCliente)
    {
        return Resultado.FalhaQuando(
            identificadorDoCliente == Guid.Empty,
            "A assinatura deve estar vinculada a um cliente válido.");
    }

    private static Resultado ValidarPlano(Plano plano)
    {
        return Resultado.FalhaQuando(
            plano is null,
            "A assinatura deve estar vinculada a um plano válido.");
    }
}
