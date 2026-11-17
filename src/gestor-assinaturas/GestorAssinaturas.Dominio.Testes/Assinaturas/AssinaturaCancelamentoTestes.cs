using GestorAssinaturas.Dominio.Assinaturas;
using GestorAssinaturas.Dominio.ObjetosDeValor;
using GestorAssinaturas.Dominio.Planos;
using Xunit;

namespace GestorAssinaturas.Dominio.Testes.Assinaturas;

public class AssinaturaCancelamentoTestes
{
    private static readonly Guid IdentificadorDaAssinatura = Guid.NewGuid();
    private static readonly Guid IdentificadorDoCliente = Guid.NewGuid();
    private static readonly DateOnly DataDeInicio = new(2026, 8, 5);
    private static readonly DateOnly DataDeFimDoPeriodoVigente = new(2026, 9, 5);

    private static Plano PlanoSemTrial => Plano.Criar(
        Guid.NewGuid(),
        "Plano Essencial",
        Dinheiro.Criar(49.90m, "BRL").Instancia,
        CicloDeCobranca.Mensal,
        periodoDeTrialEmDias: 0).Instancia;

    private static Plano PlanoComTrial => Plano.Criar(
        Guid.NewGuid(),
        "Plano Profissional",
        Dinheiro.Criar(99.90m, "BRL").Instancia,
        CicloDeCobranca.Mensal,
        periodoDeTrialEmDias: 14).Instancia;

    private static Assinatura CriarAssinaturaAtiva()
    {
        return Assinatura.Criar(IdentificadorDaAssinatura, IdentificadorDoCliente, PlanoSemTrial, DataDeInicio).Instancia;
    }

    private static Assinatura CriarAssinaturaEmTrial()
    {
        return Assinatura.Criar(IdentificadorDaAssinatura, IdentificadorDoCliente, PlanoComTrial, DataDeInicio).Instancia;
    }

    [Fact]
    public void DeveCancelarImediatamenteUmaAssinaturaAtiva()
    {
        var assinatura = CriarAssinaturaAtiva();

        var resultado = assinatura.CancelarImediatamente();

        Assert.True(resultado.EhSucesso);
        Assert.True(assinatura.EstaCancelada());
        Assert.False(assinatura.PossuiCancelamentoAgendado);
    }

    [Fact]
    public void DeveAgendarCancelamentoAoFimDoPeriodoMantendoAAssinaturaAtiva()
    {
        var assinatura = CriarAssinaturaAtiva();

        var resultado = assinatura.AgendarCancelamentoAoFimDoPeriodo(DataDeFimDoPeriodoVigente);

        Assert.True(resultado.EhSucesso);
        Assert.True(assinatura.EstaAtiva());
        Assert.True(assinatura.PossuiCancelamentoAgendado);
        Assert.Equal(DataDeFimDoPeriodoVigente, assinatura.DataDeCancelamentoAgendado);
    }

    [Fact]
    public void DeveAgendarCancelamentoAoFimDoPeriodoParaAssinaturaEmTrial()
    {
        var assinatura = CriarAssinaturaEmTrial();

        var resultado = assinatura.AgendarCancelamentoAoFimDoPeriodo(DataDeFimDoPeriodoVigente);

        Assert.True(resultado.EhSucesso);
        Assert.True(assinatura.EstaEmTrial());
        Assert.True(assinatura.PossuiCancelamentoAgendado);
    }

    [Fact]
    public void DeveRejeitarAgendamentoComDataAnteriorAoInicioDaAssinatura()
    {
        var assinatura = CriarAssinaturaAtiva();

        var resultado = assinatura.AgendarCancelamentoAoFimDoPeriodo(DataDeInicio.AddDays(-1));

        Assert.True(resultado.EhFalha);
        Assert.False(assinatura.PossuiCancelamentoAgendado);
    }

    [Fact]
    public void DeveRejeitarSegundoAgendamentoDeCancelamento()
    {
        var assinatura = CriarAssinaturaAtiva();
        assinatura.AgendarCancelamentoAoFimDoPeriodo(DataDeFimDoPeriodoVigente);

        var resultado = assinatura.AgendarCancelamentoAoFimDoPeriodo(DataDeFimDoPeriodoVigente.AddMonths(1));

        Assert.True(resultado.EhFalha);
    }

    [Fact]
    public void DeveRejeitarAgendamentoParaAssinaturaInadimplente()
    {
        var assinatura = CriarAssinaturaAtiva();
        assinatura.RegistrarInadimplencia();

        var resultado = assinatura.AgendarCancelamentoAoFimDoPeriodo(DataDeFimDoPeriodoVigente);

        Assert.True(resultado.EhFalha);
    }

    [Fact]
    public void DeveEfetivarCancelamentoAgendadoQuandoOPeriodoTermina()
    {
        var assinatura = CriarAssinaturaAtiva();
        assinatura.AgendarCancelamentoAoFimDoPeriodo(DataDeFimDoPeriodoVigente);

        var resultado = assinatura.EfetivarCancelamentoAgendado(DataDeFimDoPeriodoVigente);

        Assert.True(resultado.EhSucesso);
        Assert.True(assinatura.EstaCancelada());
    }

    [Fact]
    public void DeveManterAcessoQuandoOPeriodoVigenteAindaNaoTerminou()
    {
        var assinatura = CriarAssinaturaAtiva();
        assinatura.AgendarCancelamentoAoFimDoPeriodo(DataDeFimDoPeriodoVigente);

        var resultado = assinatura.EfetivarCancelamentoAgendado(DataDeFimDoPeriodoVigente.AddDays(-1));

        Assert.True(resultado.EhFalha);
        Assert.True(assinatura.EstaAtiva());
    }

    [Fact]
    public void DeveRejeitarEfetivacaoQuandoNaoHaCancelamentoAgendado()
    {
        var assinatura = CriarAssinaturaAtiva();

        var resultado = assinatura.EfetivarCancelamentoAgendado(DataDeFimDoPeriodoVigente);

        Assert.True(resultado.EhFalha);
    }

    [Fact]
    public void DeveCalcularOFimDoPeriodoVigenteNoPrimeiroCiclo()
    {
        var assinatura = CriarAssinaturaAtiva();

        var dataDeFim = assinatura.CalcularDataDeFimDoPeriodoVigente(DataDeInicio);

        Assert.Equal(new DateOnly(2026, 9, 5), dataDeFim);
    }

    [Fact]
    public void DeveCalcularOFimDoCicloVigenteQuandoAAssinaturaJaRenovouAlgunsCiclos()
    {
        var assinatura = CriarAssinaturaAtiva();

        var dataDeFim = assinatura.CalcularDataDeFimDoPeriodoVigente(new DateOnly(2026, 10, 20));

        Assert.Equal(new DateOnly(2026, 11, 5), dataDeFim);
    }

    [Fact]
    public void DeveAvancarParaOProximoCicloQuandoAReferenciaCaiExatamenteNoVencimento()
    {
        var assinatura = CriarAssinaturaAtiva();

        var dataDeFim = assinatura.CalcularDataDeFimDoPeriodoVigente(new DateOnly(2026, 9, 5));

        Assert.Equal(new DateOnly(2026, 10, 5), dataDeFim);
    }

    [Fact]
    public void DeveUsarOFimDoTrialComoFimDoPeriodoVigenteDeUmaAssinaturaEmTrial()
    {
        var assinatura = CriarAssinaturaEmTrial();

        var dataDeFim = assinatura.CalcularDataDeFimDoPeriodoVigente(DataDeInicio);

        Assert.Equal(assinatura.DataDeTerminoDoTrial, dataDeFim);
    }

    [Fact]
    public void DeveContarOsCiclosAPartirDaAtivacaoDeUmaAssinaturaQueSaiuDoTrial()
    {
        var assinatura = CriarAssinaturaEmTrial();
        assinatura.Ativar(new DateOnly(2026, 8, 10));

        var dataDeFim = assinatura.CalcularDataDeFimDoPeriodoVigente(new DateOnly(2026, 8, 20));

        Assert.Equal(new DateOnly(2026, 9, 10), dataDeFim);
    }

    [Fact]
    public void DeveCancelarImediatamentePelaModalidadeInformada()
    {
        var assinatura = CriarAssinaturaAtiva();

        var resultado = assinatura.Cancelar(ModalidadeDeCancelamento.Imediato, DataDeInicio);

        Assert.True(resultado.EhSucesso);
        Assert.True(assinatura.EstaCancelada());
    }

    [Fact]
    public void DeveAgendarOCancelamentoPeloDominioAoFimDoPeriodoVigente()
    {
        var assinatura = CriarAssinaturaAtiva();

        var resultado = assinatura.Cancelar(ModalidadeDeCancelamento.AoFimDoPeriodoVigente, new DateOnly(2026, 10, 20));

        Assert.True(resultado.EhSucesso);
        Assert.True(assinatura.EstaAtiva());
        Assert.Equal(new DateOnly(2026, 11, 5), assinatura.DataDeCancelamentoAgendado);
    }

    [Fact]
    public void DevePermitirCancelamentoImediatoMesmoComCancelamentoAgendado()
    {
        var assinatura = CriarAssinaturaAtiva();
        assinatura.AgendarCancelamentoAoFimDoPeriodo(DataDeFimDoPeriodoVigente);

        var resultado = assinatura.CancelarImediatamente();

        Assert.True(resultado.EhSucesso);
        Assert.True(assinatura.EstaCancelada());
        Assert.False(assinatura.PossuiCancelamentoAgendado);
    }
}
