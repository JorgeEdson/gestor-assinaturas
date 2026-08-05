using GestorAssinaturas.Dominio.Assinaturas;
using GestorAssinaturas.Dominio.Clientes;
using GestorAssinaturas.Dominio.Faturas;
using GestorAssinaturas.Dominio.Planos;
using Microsoft.EntityFrameworkCore;

namespace GestorAssinaturas.Infraestrutura.Persistencia;

public sealed class GestorAssinaturasDbContext : DbContext
{
    public GestorAssinaturasDbContext(DbContextOptions<GestorAssinaturasDbContext> opcoes) : base(opcoes)
    {
    }

    public DbSet<Plano> Planos => Set<Plano>();

    public DbSet<Cliente> Clientes => Set<Cliente>();

    public DbSet<Assinatura> Assinaturas => Set<Assinatura>();

    public DbSet<Fatura> Faturas => Set<Fatura>();

    protected override void OnModelCreating(ModelBuilder construtorDeModelo)
    {
        construtorDeModelo.ApplyConfigurationsFromAssembly(typeof(GestorAssinaturasDbContext).Assembly);
    }
}
