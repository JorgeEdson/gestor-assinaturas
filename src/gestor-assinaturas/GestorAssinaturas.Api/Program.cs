using System.Text.Json.Serialization;
using GestorAssinaturas.Api.Configuracao;
using GestorAssinaturas.Aplicacao;
using GestorAssinaturas.Infraestrutura;
using GestorAssinaturas.Infraestrutura.Persistencia.CargaInicial;

var construtorDaAplicacao = WebApplication.CreateBuilder(args);

construtorDaAplicacao.Services
    .AddControllers()
    .AddJsonOptions(opcoes =>
        opcoes.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

construtorDaAplicacao.Services.AddOpenApi();
construtorDaAplicacao.Services.AddProblemDetails();
construtorDaAplicacao.Services.AddExceptionHandler<ManipuladorDeExcecoesNaoTratadas>();

var cadeiaDeConexao = construtorDaAplicacao.Configuration.GetConnectionString("GestorAssinaturas")
    ?? throw new InvalidOperationException("A cadeia de conexão 'GestorAssinaturas' não foi configurada.");

construtorDaAplicacao.Services.AdicionarAplicacao();
construtorDaAplicacao.Services.AdicionarInfraestrutura(cadeiaDeConexao);

var aplicacao = construtorDaAplicacao.Build();

aplicacao.UseExceptionHandler();

if (aplicacao.Configuration.GetValue<bool>("AplicarMigracoesAoIniciar"))
{
    await aplicacao.Services.AplicarMigracoesDoBancoDeDadosAsync();
}

if (aplicacao.Environment.IsDevelopment())
{
    await aplicacao.Services.AplicarCargaInicialAsync();
    aplicacao.MapOpenApi();
}

aplicacao.MapControllers();

aplicacao.Run();
