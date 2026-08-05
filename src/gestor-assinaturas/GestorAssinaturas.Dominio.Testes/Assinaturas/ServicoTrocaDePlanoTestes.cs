using GestorAssinaturas.Dominio.Assinaturas;
using GestorAssinaturas.Dominio.Faturas;
using GestorAssinaturas.Dominio.ObjetosDeValor;
using GestorAssinaturas.Dominio.Planos;
using Xunit;

namespace GestorAssinaturas.Dominio.Testes.Assinaturas;

public class ServicoTrocaDePlanoTestes
{
    private static readonly Guid IdentificadorDaAssinatura = Guid.NewGuid();
    private static readonly Guid IdentificadorDoCliente = Guid.NewGuid();
    private static readonly DateOnly DataDeInicio = new(2026, 8, 5);
    private static readonly DateOnly DataDeVencimento = new(2026, 8, 5);

    private static readonly Dinheiro PrecoDoPlanoAtual = Dinheiro.Criar(49.90m, "BRL").Instancia;
    private static readonly Dinheiro PrecoDoNovoPlano = Dinheiro.Criar(99.90m, "BRL").Instancia;

    private static Plano CriarPlano(string nome, Dinheiro preco)
    {
        return Plano.Criar(
            Guid.NewGuid(),
            nome,
            preco,
            CicloDeCobranca.Mensal,
            periodoDeTrialEmDias: 0).Instancia;
    }

    private static Assinatura CriarAssinaturaAtiva(Plano plano)
    {
        return Assinatura.Criar(IdentificadorDaAssinatura, IdentificadorDoCliente, plano, DataDeInicio).Instancia;
    }

    private static Fatura EmitirFatura(Guid identificadorDaAssinatura, Dinheiro valor)
    {
        return Fatura.Emitir(Guid.NewGuid(), identificadorDaAssinatura, valor, DataDeVencimento).Instancia;
    }

    [Fact]
    public void DeveTrocarOPlanoEReprecificarAsFaturasEmAberto()
    {
        var planoAtual = CriarPlano("Plano Essencial", PrecoDoPlanoAtual);
        var novoPlano = CriarPlano("Plano Profissional", PrecoDoNovoPlano);
        var assinatura = CriarAssinaturaAtiva(planoAtual);
        var faturaEmAberto = EmitirFatura(assinatura.Identificador, PrecoDoPlanoAtual);
        var servico = new ServicoTrocaDePlano();

        var resultado = servico.TrocarPlano(assinatura, novoPlano, new[] { faturaEmAberto });

        Assert.True(resultado.EhSucesso);
        Assert.Equal(novoPlano.Identificador, assinatura.IdentificadorDoPlano);
        Assert.Equal(PrecoDoNovoPlano, faturaEmAberto.Valor);
    }

    [Fact]
    public void DeveManterIntactasAsFaturasJaPagas()
    {
        var planoAtual = CriarPlano("Plano Essencial", PrecoDoPlanoAtual);
        var novoPlano = CriarPlano("Plano Profissional", PrecoDoNovoPlano);
        var assinatura = CriarAssinaturaAtiva(planoAtual);
        var faturaPaga = EmitirFatura(assinatura.Identificador, PrecoDoPlanoAtual);
        faturaPaga.MarcarComoPaga();
        var faturaEmAberto = EmitirFatura(assinatura.Identificador, PrecoDoPlanoAtual);
        var servico = new ServicoTrocaDePlano();

        var resultado = servico.TrocarPlano(assinatura, novoPlano, new[] { faturaPaga, faturaEmAberto });

        Assert.True(resultado.EhSucesso);
        Assert.Equal(PrecoDoPlanoAtual, faturaPaga.Valor);
        Assert.Equal(PrecoDoNovoPlano, faturaEmAberto.Valor);
    }

    [Fact]
    public void DeveIgnorarFaturasDeOutrasAssinaturas()
    {
        var planoAtual = CriarPlano("Plano Essencial", PrecoDoPlanoAtual);
        var novoPlano = CriarPlano("Plano Profissional", PrecoDoNovoPlano);
        var assinatura = CriarAssinaturaAtiva(planoAtual);
        var faturaDeOutraAssinatura = EmitirFatura(Guid.NewGuid(), PrecoDoPlanoAtual);
        var servico = new ServicoTrocaDePlano();

        var resultado = servico.TrocarPlano(assinatura, novoPlano, new[] { faturaDeOutraAssinatura });

        Assert.True(resultado.EhSucesso);
        Assert.Equal(PrecoDoPlanoAtual, faturaDeOutraAssinatura.Valor);
    }

    [Fact]
    public void DeveNaoReprecificarNenhumaFaturaQuandoATrocaDePlanoFalha()
    {
        var planoAtual = CriarPlano("Plano Essencial", PrecoDoPlanoAtual);
        var assinatura = CriarAssinaturaAtiva(planoAtual);
        var faturaEmAberto = EmitirFatura(assinatura.Identificador, PrecoDoPlanoAtual);
        var servico = new ServicoTrocaDePlano();

        var resultado = servico.TrocarPlano(assinatura, planoAtual, new[] { faturaEmAberto });

        Assert.True(resultado.EhFalha);
        Assert.Equal(PrecoDoPlanoAtual, faturaEmAberto.Valor);
    }

    [Fact]
    public void DeveRetornarFalhaQuandoAAssinaturaNaoEhInformada()
    {
        var novoPlano = CriarPlano("Plano Profissional", PrecoDoNovoPlano);
        var servico = new ServicoTrocaDePlano();

        var resultado = servico.TrocarPlano(null!, novoPlano, Array.Empty<Fatura>());

        Assert.True(resultado.EhFalha);
    }

    [Fact]
    public void DeveRetornarFalhaQuandoAColecaoDeFaturasNaoEhInformada()
    {
        var planoAtual = CriarPlano("Plano Essencial", PrecoDoPlanoAtual);
        var novoPlano = CriarPlano("Plano Profissional", PrecoDoNovoPlano);
        var assinatura = CriarAssinaturaAtiva(planoAtual);
        var servico = new ServicoTrocaDePlano();

        var resultado = servico.TrocarPlano(assinatura, novoPlano, null!);

        Assert.True(resultado.EhFalha);
    }
}
