using System.Text.Json.Serialization;
using GestorAssinaturas.Aplicacao;
using GestorAssinaturas.Infraestrutura;
using GestorAssinaturas.Infraestrutura.Persistencia;
using Microsoft.EntityFrameworkCore;

var construtorDaAplicacao = WebApplication.CreateBuilder(args);

construtorDaAplicacao.Services
    .AddControllers()
    .AddJsonOptions(opcoes =>
        opcoes.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

construtorDaAplicacao.Services.AddOpenApi();

var cadeiaDeConexao = construtorDaAplicacao.Configuration.GetConnectionString("GestorAssinaturas")
    ?? throw new InvalidOperationException("A cadeia de conexão 'GestorAssinaturas' não foi configurada.");

construtorDaAplicacao.Services.AdicionarInfraestrutura(cadeiaDeConexao);
construtorDaAplicacao.Services.AdicionarAplicacao();

var aplicacao = construtorDaAplicacao.Build();

if (aplicacao.Configuration.GetValue<bool>("AplicarMigracoesAoIniciar"))
{
    using var escopoDeInicializacao = aplicacao.Services.CreateScope();
    var contextoDeDados = escopoDeInicializacao.ServiceProvider.GetRequiredService<ContextoDeDados>();
    await contextoDeDados.Database.MigrateAsync();
}

if (aplicacao.Environment.IsDevelopment())
{
    aplicacao.MapOpenApi();
}

aplicacao.MapControllers();

aplicacao.Run();
