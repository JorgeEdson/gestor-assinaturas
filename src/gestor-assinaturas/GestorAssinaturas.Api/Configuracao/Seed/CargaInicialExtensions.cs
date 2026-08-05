using GestorAssinaturas.Aplicacao.Portas.Persistencia;
using GestorAssinaturas.Dominio.Clientes;
using GestorAssinaturas.Dominio.Comum;
using GestorAssinaturas.Dominio.ObjetosDeValor;
using GestorAssinaturas.Dominio.Planos;

namespace GestorAssinaturas.Api.Configuracao.Seed;

public static class CargaInicialExtensions
{
    public static async Task AplicarCargaInicialAsync(this WebApplication aplicacao)
    {
        await using var escopo = aplicacao.Services.CreateAsyncScope();

        var unitOfWork = escopo.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var logger = escopo.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("CargaInicial");

        if (await CargaJaAplicadaAsync(unitOfWork, logger))
        {
            return;
        }

        var erros = new List<string>();

        var planos = Construir(CatalogoDeSeed.Planos, CriarPlano, erros);
        var clientes = Construir(CatalogoDeSeed.Clientes, CriarCliente, erros);

        if (erros.Count > 0)
        {
            logger.LogError(
                "Carga inicial abortada: {Quantidade} item(ns) do catálogo não passaram nas regras do domínio. {Erros}",
                erros.Count,
                string.Join(" | ", erros.Take(10)));

            return;
        }

        foreach (var plano in planos)
        {
            await unitOfWork.Planos.AdicionarAsync(plano);
        }

        foreach (var cliente in clientes)
        {
            await unitOfWork.Clientes.AdicionarAsync(cliente);
        }

        var persistencia = await unitOfWork.SalvarAlteracoesAsync();

        if (persistencia.EhFalha)
        {
            logger.LogError(
                "Falha ao gravar a carga inicial: {Erros}",
                string.Join(" | ", persistencia.Erros!));

            return;
        }

        logger.LogInformation(
            "Carga inicial aplicada: {Planos} planos e {Clientes} clientes.",
            planos.Count,
            clientes.Count);
    }

    private static async Task<bool> CargaJaAplicadaAsync(IUnitOfWork unitOfWork, ILogger logger)
    {
        var marcador = CatalogoDeSeed.Planos[0].Identificador;

        var existente = await unitOfWork.Planos.ObterPorIdentificadorAsync(marcador);

        if (existente.EhFalha)
        {
            logger.LogWarning(
                "Não foi possível verificar a carga inicial: {Erros}",
                string.Join(" | ", existente.Erros!));

            return true;
        }

        if (existente.Instancia is not null)
        {
            logger.LogInformation("Carga inicial já aplicada; nada a fazer.");

            return true;
        }

        return false;
    }

    private static List<TAgregado> Construir<TDados, TAgregado>(
        IEnumerable<TDados> catalogo,
        Func<TDados, Resultado<TAgregado>> fabrica,
        List<string> erros)
    {
        var construidos = new List<TAgregado>();

        foreach (var dados in catalogo)
        {
            var resultado = fabrica(dados);

            if (resultado.EhFalha)
            {
                erros.AddRange(resultado.Erros!);
            }
            else
            {
                construidos.Add(resultado.Instancia);
            }
        }

        return construidos;
    }

    private static Resultado<Plano> CriarPlano(PlanoDeSeed dados)
    {
        var preco = Dinheiro.Criar(dados.Valor, dados.Moeda);
        var cicloDeCobranca = CicloDeCobranca.APartirDoTipo(dados.CicloDeCobranca);

        var combinado = Resultado.Combinar(preco, cicloDeCobranca);

        if (combinado.EhFalha)
        {
            return Resultado<Plano>.Falha(
                Rotular($"plano {dados.Identificador} ({dados.Nome})", combinado.Erros!));
        }

        var plano = Plano.Criar(
            dados.Identificador,
            dados.Nome,
            preco.Instancia,
            cicloDeCobranca.Instancia,
            dados.PeriodoDeTrialEmDias);

        if (plano.EhFalha)
        {
            return Resultado<Plano>.Falha(
                Rotular($"plano {dados.Identificador} ({dados.Nome})", plano.Erros!));
        }

        return plano;
    }

    private static Resultado<Cliente> CriarCliente(ClienteDeSeed dados)
    {
        var email = Email.Criar(dados.Email);

        if (email.EhFalha)
        {
            return Resultado<Cliente>.Falha(
                Rotular($"cliente {dados.Identificador} ({dados.Nome})", email.Erros!));
        }

        var cliente = Cliente.Criar(dados.Identificador, dados.Nome, email.Instancia);

        if (cliente.EhFalha)
        {
            return Resultado<Cliente>.Falha(
                Rotular($"cliente {dados.Identificador} ({dados.Nome})", cliente.Erros!));
        }

        return cliente;
    }

    private static IEnumerable<string> Rotular(string rotulo, IEnumerable<string> erros)
    {
        return erros.Select(erro => $"{rotulo}: {erro}");
    }
}
