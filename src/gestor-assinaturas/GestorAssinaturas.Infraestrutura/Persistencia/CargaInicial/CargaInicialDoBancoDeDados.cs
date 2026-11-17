using GestorAssinaturas.Dominio.Clientes;
using GestorAssinaturas.Dominio.Comum;
using GestorAssinaturas.Dominio.ObjetosDeValor;
using GestorAssinaturas.Dominio.Planos;
using GestorAssinaturas.Dominio.Repositorios;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GestorAssinaturas.Infraestrutura.Persistencia.CargaInicial;

public static class CargaInicialDoBancoDeDados
{
    public static async Task AplicarCargaInicialAsync(this IServiceProvider provedorDeServicos)
    {
        ArgumentNullException.ThrowIfNull(provedorDeServicos);

        await using var escopo = provedorDeServicos.CreateAsyncScope();

        var unidadeDeTrabalho = escopo.ServiceProvider.GetRequiredService<IUnidadeDeTrabalho>();
        var registradorDeLog = escopo.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger(nameof(CargaInicialDoBancoDeDados));

        if (await CargaInicialJaFoiAplicadaAsync(unidadeDeTrabalho))
        {
            registradorDeLog.LogInformation("Carga inicial já aplicada; nada a fazer.");

            return;
        }

        var erros = new List<string>();

        var planos = Construir(CatalogoDaCargaInicial.Planos, CriarPlano, erros);
        var clientes = Construir(CatalogoDaCargaInicial.Clientes, CriarCliente, erros);

        if (erros.Count > 0)
        {
            registradorDeLog.LogError(
                "Carga inicial abortada: {Quantidade} item(ns) do catálogo não passaram nas regras do domínio. {Erros}",
                erros.Count,
                string.Join(" | ", erros.Take(10)));

            return;
        }

        foreach (var plano in planos)
        {
            await unidadeDeTrabalho.Planos.AdicionarAsync(plano);
        }

        foreach (var cliente in clientes)
        {
            await unidadeDeTrabalho.Clientes.AdicionarAsync(cliente);
        }

        await unidadeDeTrabalho.SalvarAlteracoesAsync();

        registradorDeLog.LogInformation(
            "Carga inicial aplicada: {QuantidadeDePlanos} planos e {QuantidadeDeClientes} clientes.",
            planos.Count,
            clientes.Count);
    }

    private static async Task<bool> CargaInicialJaFoiAplicadaAsync(IUnidadeDeTrabalho unidadeDeTrabalho)
    {
        var identificadorDoPlanoMarcador = CatalogoDaCargaInicial.Planos[0].Identificador;

        var planoMarcador = await unidadeDeTrabalho.Planos.ObterPorIdentificadorAsync(identificadorDoPlanoMarcador);

        return planoMarcador is not null;
    }

    private static List<TAgregado> Construir<TDados, TAgregado>(
        IEnumerable<TDados> catalogo,
        Func<TDados, Resultado<TAgregado>> fabrica,
        List<string> erros)
    {
        var agregadosConstruidos = new List<TAgregado>();

        foreach (var dados in catalogo)
        {
            var resultadoDaConstrucao = fabrica(dados);

            if (resultadoDaConstrucao.EhFalha)
            {
                erros.AddRange(resultadoDaConstrucao.Erros!);
            }
            else
            {
                agregadosConstruidos.Add(resultadoDaConstrucao.Instancia);
            }
        }

        return agregadosConstruidos;
    }

    private static Resultado<Plano> CriarPlano(PlanoDaCargaInicial dados)
    {
        var resultadoDoPreco = Dinheiro.Criar(dados.Valor, dados.Moeda);
        var resultadoDoCicloDeCobranca = CicloDeCobranca.APartirDoTipo(dados.CicloDeCobranca);

        var resultadoDosObjetosDeValor = Resultado.Combinar(resultadoDoPreco, resultadoDoCicloDeCobranca);

        if (resultadoDosObjetosDeValor.EhFalha)
        {
            return Resultado<Plano>.Falha(
                RotularErros($"plano {dados.Identificador}", resultadoDosObjetosDeValor.Erros!));
        }

        var resultadoDoPlano = Plano.Criar(
            dados.Identificador,
            dados.Nome,
            resultadoDoPreco.Instancia,
            resultadoDoCicloDeCobranca.Instancia,
            dados.PeriodoDeTrialEmDias);

        if (resultadoDoPlano.EhFalha)
        {
            return Resultado<Plano>.Falha(
                RotularErros($"plano {dados.Identificador}", resultadoDoPlano.Erros!));
        }

        return resultadoDoPlano;
    }

    private static Resultado<Cliente> CriarCliente(ClienteDaCargaInicial dados)
    {
        var resultadoDoEmail = Email.Criar(dados.Email);

        if (resultadoDoEmail.EhFalha)
        {
            return Resultado<Cliente>.Falha(
                RotularErros($"cliente {dados.Identificador}", resultadoDoEmail.Erros!));
        }

        var resultadoDoCliente = Cliente.Criar(dados.Identificador, dados.Nome, resultadoDoEmail.Instancia);

        if (resultadoDoCliente.EhFalha)
        {
            return Resultado<Cliente>.Falha(
                RotularErros($"cliente {dados.Identificador}", resultadoDoCliente.Erros!));
        }

        return resultadoDoCliente;
    }

    private static IEnumerable<string> RotularErros(string rotulo, IEnumerable<string> erros)
    {
        return erros.Select(erro => $"{rotulo}: {erro}");
    }
}
