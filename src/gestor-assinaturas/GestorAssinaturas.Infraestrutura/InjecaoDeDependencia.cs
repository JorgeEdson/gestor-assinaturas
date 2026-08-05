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

        servicos.AddDbContext<GestorAssinaturasDbContext>(opcoes => opcoes.UseSqlServer(cadeiaDeConexao));

        servicos.AddScoped<IRepositorioPlano, PlanoRepository>();
        servicos.AddScoped<IRepositorioCliente, ClienteRepository>();
        servicos.AddScoped<IRepositorioAssinatura, AssinaturaRepository>();
        servicos.AddScoped<IRepositorioFatura, FaturaRepository>();
        servicos.AddScoped<IUnitOfWork, UnitOfWork>();

        servicos.AddSingleton<IRelogioDoSistema, RelogioDoSistema>();
        servicos.AddSingleton<IGatewayPagamento, GatewayPagamentoSimulado>();

        servicos.AddSingleton<ServicoReativacao>();
        servicos.AddSingleton<ServicoInadimplencia>();
        servicos.AddSingleton<ServicoTrocaDePlano>();

        return servicos;
    }
}
