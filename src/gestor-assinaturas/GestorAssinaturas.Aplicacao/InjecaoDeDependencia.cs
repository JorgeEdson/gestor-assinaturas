using GestorAssinaturas.Aplicacao.Assinaturas;
using GestorAssinaturas.Aplicacao.Clientes;
using GestorAssinaturas.Aplicacao.Planos;
using GestorAssinaturas.Dominio.Servicos;
using Microsoft.Extensions.DependencyInjection;

namespace GestorAssinaturas.Aplicacao;

public static class InjecaoDeDependencia
{
    public static IServiceCollection AdicionarAplicacao(this IServiceCollection servicos)
    {
        ArgumentNullException.ThrowIfNull(servicos);

        servicos.AddSingleton<ServicoReativacao>();
        servicos.AddSingleton<ServicoInadimplencia>();
        servicos.AddSingleton<ServicoTrocaDePlano>();
        servicos.AddSingleton<ServicoRecobranca>();

        servicos.AddScoped<CadastrarPlanoServicoDeAplicacao>();
        servicos.AddScoped<CadastrarClienteServicoDeAplicacao>();
        servicos.AddScoped<CriarAssinaturaServicoDeAplicacao>();
        servicos.AddScoped<AtivarAssinaturaServicoDeAplicacao>();
        servicos.AddScoped<RegistrarPagamentoServicoDeAplicacao>();
        servicos.AddScoped<RecobrarAssinaturaInadimplenteServicoDeAplicacao>();
        servicos.AddScoped<TrocarPlanoServicoDeAplicacao>();
        servicos.AddScoped<CancelarAssinaturaServicoDeAplicacao>();

        return servicos;
    }
}
