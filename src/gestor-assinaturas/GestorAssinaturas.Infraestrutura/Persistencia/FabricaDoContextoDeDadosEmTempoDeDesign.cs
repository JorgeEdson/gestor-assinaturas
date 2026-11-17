using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GestorAssinaturas.Infraestrutura.Persistencia;

public sealed class FabricaDoContextoDeDadosEmTempoDeDesign : IDesignTimeDbContextFactory<ContextoDeDados>
{
    private const string CadeiaDeConexaoPadrao =
        "Server=(localdb)\\MSSQLLocalDB;Database=GestorAssinaturas;Trusted_Connection=True;TrustServerCertificate=True";

    public ContextoDeDados CreateDbContext(string[] argumentos)
    {
        var cadeiaDeConexao =
            Environment.GetEnvironmentVariable("ConnectionStrings__GestorAssinaturas")
            ?? CadeiaDeConexaoPadrao;

        var opcoes = new DbContextOptionsBuilder<ContextoDeDados>()
            .UseSqlServer(cadeiaDeConexao)
            .Options;

        return new ContextoDeDados(opcoes);
    }
}
