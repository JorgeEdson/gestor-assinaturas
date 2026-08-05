using GestorAssinaturas.Dominio.Assinaturas;
using GestorAssinaturas.Dominio.Faturas;
using GestorAssinaturas.Dominio.ObjetosDeValor;
using GestorAssinaturas.Dominio.Planos;
using Xunit;

namespace GestorAssinaturas.Dominio.Testes.Assinaturas;

public class AssinaturaTestes
{
    private static readonly Guid IdentificadorDaAssinatura = Guid.NewGuid();
    private static readonly Guid IdentificadorDoCliente = Guid.NewGuid();
    private static readonly DateOnly DataDeInicio = new(2026, 8, 5);

    private static Plano PlanoComTrial => Plano.Criar(
        Guid.NewGuid(),
        "Plano Profissional",
        Dinheiro.Criar(99.90m, "BRL").Instancia,
        CicloDeCobranca.Mensal,
        periodoDeTrialEmDias: 14).Instancia;

    private static Plano PlanoSemTrial => Plano.Criar(
        Guid.NewGuid(),
        "Plano Essencial",
        Dinheiro.Criar(49.90m, "BRL").Instancia,
        CicloDeCobranca.Mensal,
        periodoDeTrialEmDias: 0).Instancia;

    private static Assinatura CriarAssinaturaComTrial()
    {
        return Assinatura.Criar(IdentificadorDaAssinatura, IdentificadorDoCliente, PlanoComTrial, DataDeInicio).Instancia;
    }

    private static Assinatura CriarAssinaturaSemTrial()
    {
        return Assinatura.Criar(IdentificadorDaAssinatura, IdentificadorDoCliente, PlanoSemTrial, DataDeInicio).Instancia;
    }

    [Fact]
    public void DeveIniciarEmTrialQuandoOPlanoPossuiPeriodoDeTrial()
    {
        var resultado = Assinatura.Criar(IdentificadorDaAssinatura, IdentificadorDoCliente, PlanoComTrial, DataDeInicio);

        Assert.True(resultado.EhSucesso);

        var assinatura = resultado.Instancia;

        Assert.True(assinatura.EstaEmTrial());
        Assert.Equal(new DateOnly(2026, 8, 19), assinatura.DataDeTerminoDoTrial);
        Assert.False(assinatura.PrecisaDeCobrancaImediata());
    }

    [Fact]
    public void DeveIniciarAtivaQuandoOPlanoNaoPossuiPeriodoDeTrial()
    {
        var resultado = Assinatura.Criar(IdentificadorDaAssinatura, IdentificadorDoCliente, PlanoSemTrial, DataDeInicio);

        Assert.True(resultado.EhSucesso);

        var assinatura = resultado.Instancia;

        Assert.True(assinatura.EstaAtiva());
        Assert.Null(assinatura.DataDeTerminoDoTrial);
        Assert.True(assinatura.PrecisaDeCobrancaImediata());
    }

    [Fact]
    public void DeveExporOIdentificadorDoPlanoVinculado()
    {
        var plano = PlanoSemTrial;

        var assinatura = Assinatura.Criar(IdentificadorDaAssinatura, IdentificadorDoCliente, plano, DataDeInicio).Instancia;

        Assert.Equal(plano.Identificador, assinatura.IdentificadorDoPlano);
    }

    [Fact]
    public void DeveRetornarFalhaParaIdentificadorDaAssinaturaVazio()
    {
        var resultado = Assinatura.Criar(Guid.Empty, IdentificadorDoCliente, PlanoSemTrial, DataDeInicio);

        Assert.True(resultado.EhFalha);
    }

    [Fact]
    public void DeveRetornarFalhaParaClienteNaoVinculado()
    {
        var resultado = Assinatura.Criar(IdentificadorDaAssinatura, Guid.Empty, PlanoSemTrial, DataDeInicio);

        Assert.True(resultado.EhFalha);
        Assert.Contains(resultado.Erros!, erro => erro.Contains("cliente"));
    }

    [Fact]
    public void DeveRetornarFalhaParaPlanoNaoVinculado()
    {
        var resultado = Assinatura.Criar(IdentificadorDaAssinatura, IdentificadorDoCliente, null!, DataDeInicio);

        Assert.True(resultado.EhFalha);
        Assert.Contains(resultado.Erros!, erro => erro.Contains("plano"));
    }

    [Fact]
    public void DeveAtivarAssinaturaEmTrial()
    {
        var assinatura = CriarAssinaturaComTrial();

        var resultado = assinatura.Ativar();

        Assert.True(resultado.EhSucesso);
        Assert.True(assinatura.EstaAtiva());
    }

    [Fact]
    public void DeveRegistrarInadimplenciaDeAssinaturaAtiva()
    {
        var assinatura = CriarAssinaturaSemTrial();

        var resultado = assinatura.RegistrarInadimplencia();

        Assert.True(resultado.EhSucesso);
        Assert.True(assinatura.EstaInadimplente());
    }

    [Fact]
    public void DeveReativarAssinaturaInadimplente()
    {
        var assinatura = CriarAssinaturaSemTrial();
        assinatura.RegistrarInadimplencia();

        var resultado = assinatura.Reativar();

        Assert.True(resultado.EhSucesso);
        Assert.True(assinatura.EstaAtiva());
    }

    [Fact]
    public void DeveSuspenderAssinaturaInadimplente()
    {
        var assinatura = CriarAssinaturaSemTrial();
        assinatura.RegistrarInadimplencia();

        var resultado = assinatura.Suspender();

        Assert.True(resultado.EhSucesso);
        Assert.True(assinatura.EstaSuspensa());
    }

    [Fact]
    public void DeveCancelarAssinaturaAtiva()
    {
        var assinatura = CriarAssinaturaSemTrial();

        var resultado = assinatura.CancelarImediatamente();

        Assert.True(resultado.EhSucesso);
        Assert.True(assinatura.EstaCancelada());
    }

    [Fact]
    public void DeveCancelarAssinaturaEmTrial()
    {
        var assinatura = CriarAssinaturaComTrial();

        var resultado = assinatura.CancelarImediatamente();

        Assert.True(resultado.EhSucesso);
        Assert.True(assinatura.EstaCancelada());
    }

    [Fact]
    public void DeveCancelarAssinaturaSuspensa()
    {
        var assinatura = CriarAssinaturaSemTrial();
        assinatura.RegistrarInadimplencia();
        assinatura.Suspender();

        var resultado = assinatura.CancelarImediatamente();

        Assert.True(resultado.EhSucesso);
        Assert.True(assinatura.EstaCancelada());
    }

    [Fact]
    public void DeveRejeitarInadimplenciaEmAssinaturaEmTrial()
    {
        var assinatura = CriarAssinaturaComTrial();

        var resultado = assinatura.RegistrarInadimplencia();

        Assert.True(resultado.EhFalha);
        Assert.True(assinatura.EstaEmTrial());
    }

    [Fact]
    public void DeveRejeitarSuspensaoDeAssinaturaAtiva()
    {
        var assinatura = CriarAssinaturaSemTrial();

        var resultado = assinatura.Suspender();

        Assert.True(resultado.EhFalha);
    }

    [Fact]
    public void DeveRejeitarCancelamentoDeAssinaturaInadimplente()
    {
        var assinatura = CriarAssinaturaSemTrial();
        assinatura.RegistrarInadimplencia();

        var resultado = assinatura.CancelarImediatamente();

        Assert.True(resultado.EhFalha);
        Assert.True(assinatura.EstaInadimplente());
    }

    [Fact]
    public void DeveTratarAssinaturaCanceladaComoEstadoTerminal()
    {
        var assinatura = CriarAssinaturaSemTrial();
        assinatura.CancelarImediatamente();

        var resultadoDaAtivacao = assinatura.Ativar();
        var resultadoDaInadimplencia = assinatura.RegistrarInadimplencia();

        Assert.True(resultadoDaAtivacao.EhFalha);
        Assert.True(resultadoDaInadimplencia.EhFalha);
        Assert.True(assinatura.EstaCancelada());
    }

    [Fact]
    public void DeveGerarFaturaDeCobrancaParaAssinaturaAtiva()
    {
        var assinatura = CriarAssinaturaSemTrial();
        var identificadorDaFatura = Guid.NewGuid();
        var dataDeVencimento = new DateOnly(2026, 8, 5);

        var resultado = assinatura.GerarFaturaDeCobranca(identificadorDaFatura, dataDeVencimento);

        Assert.True(resultado.EhSucesso);

        var fatura = resultado.Instancia;

        Assert.Equal(identificadorDaFatura, fatura.Identificador);
        Assert.Equal(assinatura.Identificador, fatura.IdentificadorDaAssinatura);
        Assert.Equal(assinatura.Plano.Preco, fatura.Valor);
        Assert.Equal(StatusFatura.Aberta, fatura.Status);
    }

    [Fact]
    public void DeveRejeitarGeracaoDeFaturaParaAssinaturaEmTrial()
    {
        var assinatura = CriarAssinaturaComTrial();

        var resultado = assinatura.GerarFaturaDeCobranca(Guid.NewGuid(), new DateOnly(2026, 8, 5));

        Assert.True(resultado.EhFalha);
    }

    [Fact]
    public void DeveRejeitarGeracaoDeFaturaParaAssinaturaCancelada()
    {
        var assinatura = CriarAssinaturaSemTrial();
        assinatura.CancelarImediatamente();

        var resultado = assinatura.GerarFaturaDeCobranca(Guid.NewGuid(), new DateOnly(2026, 8, 5));

        Assert.True(resultado.EhFalha);
        Assert.Contains(resultado.Erros!, erro => erro.Contains("cancelada"));
    }
}
