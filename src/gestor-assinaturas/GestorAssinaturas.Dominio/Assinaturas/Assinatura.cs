using GestorAssinaturas.Dominio.Comum;
using GestorAssinaturas.Dominio.Faturas;
using GestorAssinaturas.Dominio.ObjetosDeValor;
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

    private Assinatura()
    {
        ValorDaAssinatura = null!;
        CicloDeCobranca = null!;
    }

    private Assinatura(
        Guid identificador,
        Guid identificadorDoCliente,
        Guid identificadorDoPlano,
        Dinheiro valorDaAssinatura,
        CicloDeCobranca cicloDeCobranca,
        StatusAssinatura status,
        DateOnly dataDeInicio,
        DateOnly? dataDeTerminoDoTrial,
        DateOnly? dataDeInicioDaCobranca)
        : base(identificador)
    {
        IdentificadorDoCliente = identificadorDoCliente;
        IdentificadorDoPlano = identificadorDoPlano;
        ValorDaAssinatura = valorDaAssinatura;
        CicloDeCobranca = cicloDeCobranca;
        Status = status;
        DataDeInicio = dataDeInicio;
        DataDeTerminoDoTrial = dataDeTerminoDoTrial;
        DataDeInicioDaCobranca = dataDeInicioDaCobranca;
    }

    public Guid IdentificadorDoCliente { get; }

    public Guid IdentificadorDoPlano { get; private set; }

    public Dinheiro ValorDaAssinatura { get; private set; }

    public CicloDeCobranca CicloDeCobranca { get; private set; }

    public StatusAssinatura Status { get; private set; }

    public DateOnly DataDeInicio { get; }

    public DateOnly? DataDeTerminoDoTrial { get; }

    public DateOnly? DataDeInicioDaCobranca { get; private set; }

    public DateOnly? DataDeCancelamentoAgendado { get; private set; }

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
                plano.Identificador,
                plano.Preco,
                plano.CicloDeCobranca,
                StatusAssinatura.Trial,
                dataDeInicio,
                dataDeTerminoDoTrial,
                dataDeInicioDaCobranca: null);

            return Resultado<Assinatura>.Sucesso(assinaturaEmTrial);
        }

        var assinaturaAtiva = new Assinatura(
            identificador,
            identificadorDoCliente,
            plano.Identificador,
            plano.Preco,
            plano.CicloDeCobranca,
            StatusAssinatura.Ativa,
            dataDeInicio,
            dataDeTerminoDoTrial: null,
            dataDeInicioDaCobranca: dataDeInicio);

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

    public Resultado Ativar(DateOnly dataDeAtivacao)
    {
        if (dataDeAtivacao < DataDeInicio)
        {
            return Resultado.Falha("A data de ativação não pode ser anterior ao início da assinatura.");
        }

        var resultadoDaAtivacao = Transicionar(StatusAssinatura.Ativa);

        if (resultadoDaAtivacao.EhFalha)
        {
            return resultadoDaAtivacao;
        }

        DataDeInicioDaCobranca = dataDeAtivacao;

        return Resultado.Sucesso();
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

    public Resultado Cancelar(ModalidadeDeCancelamento modalidade, DateOnly dataDeReferencia)
    {
        return modalidade switch
        {
            ModalidadeDeCancelamento.Imediato => CancelarImediatamente(),
            ModalidadeDeCancelamento.AoFimDoPeriodoVigente =>
                AgendarCancelamentoAoFimDoPeriodo(CalcularDataDeFimDoPeriodoVigente(dataDeReferencia)),
            _ => Resultado.Falha($"Modalidade de cancelamento não suportada: {modalidade}.")
        };
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

    public DateOnly CalcularDataDeFimDoPeriodoVigente(DateOnly dataDeReferencia)
    {
        if (EstaEmTrial() && DataDeTerminoDoTrial is not null)
        {
            return DataDeTerminoDoTrial.Value;
        }

        var dataDeInicioDosCiclosDeCobranca = DataDeInicioDaCobranca ?? DataDeInicio;
        var numeroDoCiclo = 1;
        var dataDeFimDoCiclo = CicloDeCobranca.CalcularDataDeFimDoCiclo(dataDeInicioDosCiclosDeCobranca, numeroDoCiclo);

        while (dataDeFimDoCiclo <= dataDeReferencia)
        {
            numeroDoCiclo++;
            dataDeFimDoCiclo = CicloDeCobranca.CalcularDataDeFimDoCiclo(dataDeInicioDosCiclosDeCobranca, numeroDoCiclo);
        }

        return dataDeFimDoCiclo;
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

        if (novoPlano.Identificador == IdentificadorDoPlano)
        {
            return Resultado.Falha("O novo plano deve ser diferente do plano atual da assinatura.");
        }

        if (!novoPlano.Preco.PossuiMesmaMoedaQue(ValorDaAssinatura))
        {
            return Resultado.Falha(
                $"A troca de plano não pode alterar a moeda da assinatura: de {ValorDaAssinatura.Moeda} para {novoPlano.Preco.Moeda}.");
        }

        IdentificadorDoPlano = novoPlano.Identificador;
        ValorDaAssinatura = novoPlano.Preco;
        CicloDeCobranca = novoPlano.CicloDeCobranca;

        return Resultado.Sucesso();
    }

    public Resultado VerificarSePodeReceberPagamentoDa(Fatura fatura)
    {
        if (fatura is null)
        {
            return Resultado.Falha("A fatura é obrigatória para o registro do pagamento.");
        }

        if (fatura.IdentificadorDaAssinatura != Identificador)
        {
            return Resultado.Falha("A fatura informada não pertence à assinatura.");
        }

        if (EstaCancelada())
        {
            return Resultado.Falha("Uma assinatura cancelada não aceita novo pagamento.");
        }

        return Resultado.FalhaQuando(
            !fatura.EstaAberta(),
            $"Somente uma fatura em aberto pode receber pagamento. Status atual da fatura: {fatura.Status}.");
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

        return Fatura.Emitir(identificadorDaFatura, Identificador, ValorDaAssinatura, dataDeVencimento);
    }

    public Resultado<Fatura> EmitirFaturaDeRecobranca(Guid identificadorDaFatura, DateOnly dataDeVencimento)
    {
        if (!EstaInadimplente())
        {
            return Resultado<Fatura>.Falha(
                $"Somente uma assinatura inadimplente pode receber uma fatura de recobrança. Status atual: {Status}.");
        }

        return Fatura.Emitir(identificadorDaFatura, Identificador, ValorDaAssinatura, dataDeVencimento);
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
