using GestorAssinaturas.Aplicacao.Portas;
using GestorAssinaturas.Aplicacao.Portas.Pagamentos;
using GestorAssinaturas.Aplicacao.Portas.Persistencia;
using GestorAssinaturas.Dominio.Assinaturas;
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
        servicos.AddScoped<IUnitOfWork, UnitOfWork>();

        servicos.AddSingleton<IRelogioDoSistema, RelogioDoSistema>();
        servicos.AddSingleton<IGatewayPagamento, GatewayPagamentoSimulado>();

        servicos.AddSingleton<ServicoReativacao>();
        servicos.AddSingleton<ServicoInadimplencia>();
        servicos.AddSingleton<ServicoTrocaDePlano>();

        return servicos;
    }
}
