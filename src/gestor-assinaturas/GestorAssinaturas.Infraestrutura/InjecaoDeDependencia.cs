using GestorAssinaturas.Aplicacao.Portas;
using GestorAssinaturas.Aplicacao.Portas.Pagamentos;
using GestorAssinaturas.Dominio.Repositorios;
using GestorAssinaturas.Infraestrutura.Pagamentos;
using GestorAssinaturas.Infraestrutura.Persistencia;
using GestorAssinaturas.Infraestrutura.Persistencia.Repositorios;
using GestorAssinaturas.Infraestrutura.Tempo;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GestorAssinaturas.Infraestrutura;

public static class InjecaoDeDependencia
{
    public static IServiceCollection AdicionarInfraestrutura(
        this IServiceCollection servicos,
        string cadeiaDeConexao)
    {
        ArgumentNullException.ThrowIfNull(servicos);
        ArgumentException.ThrowIfNullOrWhiteSpace(cadeiaDeConexao);

        servicos.AddDbContext<ContextoDeDados>(opcoes => opcoes.UseSqlServer(cadeiaDeConexao));

        servicos.AddScoped<IRepositorioPlano, RepositorioPlano>();
        servicos.AddScoped<IRepositorioCliente, RepositorioCliente>();
        servicos.AddScoped<IRepositorioAssinatura, RepositorioAssinatura>();
        servicos.AddScoped<IRepositorioFatura, RepositorioFatura>();
        servicos.AddScoped<IUnidadeDeTrabalho, UnidadeDeTrabalho>();

        servicos.AddSingleton<IRelogioDoSistema, RelogioDoSistema>();
        servicos.AddSingleton<IGatewayPagamento, GatewayPagamentoSimulado>();

        return servicos;
    }

    public static async Task AplicarMigracoesDoBancoDeDadosAsync(this IServiceProvider provedorDeServicos)
    {
        ArgumentNullException.ThrowIfNull(provedorDeServicos);

        await using var escopo = provedorDeServicos.CreateAsyncScope();

        var contextoDeDados = escopo.ServiceProvider.GetRequiredService<ContextoDeDados>();

        await contextoDeDados.Database.MigrateAsync();
    }
}
