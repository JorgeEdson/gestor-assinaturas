using GestorAssinaturas.Aplicacao.Assinaturas;
using GestorAssinaturas.Aplicacao.Clientes;
using GestorAssinaturas.Aplicacao.Planos;
using Microsoft.Extensions.DependencyInjection;

namespace GestorAssinaturas.Aplicacao;

public static class InjecaoDeDependencia
{
    public static IServiceCollection AdicionarAplicacao(this IServiceCollection servicos)
    {
        ArgumentNullException.ThrowIfNull(servicos);

        servicos.AddScoped<CadastrarPlanoApplicationService>();
        servicos.AddScoped<CadastrarClienteApplicationService>();
        servicos.AddScoped<CriarAssinaturaApplicationService>();
        servicos.AddScoped<AtivarAssinaturaApplicationService>();
        servicos.AddScoped<RegistrarPagamentoApplicationService>();
        servicos.AddScoped<TrocarPlanoApplicationService>();
        servicos.AddScoped<CancelarAssinaturaApplicationService>();

        return servicos;
    }
}
