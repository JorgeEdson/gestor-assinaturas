using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GestorAssinaturas.Infraestrutura.Persistencia;

public sealed class GestorAssinaturasDbContextFactory : IDesignTimeDbContextFactory<GestorAssinaturasDbContext>
{
    private const string CadeiaDeConexaoPadrao =
        "Server=(localdb)\\MSSQLLocalDB;Database=GestorAssinaturas;Trusted_Connection=True;TrustServerCertificate=True";

    public GestorAssinaturasDbContext CreateDbContext(string[] argumentos)
    {
        var cadeiaDeConexao =
            Environment.GetEnvironmentVariable("ConnectionStrings__GestorAssinaturas")
            ?? CadeiaDeConexaoPadrao;

        var opcoes = new DbContextOptionsBuilder<GestorAssinaturasDbContext>()
            .UseSqlServer(cadeiaDeConexao)
            .Options;

        return new GestorAssinaturasDbContext(opcoes);
    }
}
