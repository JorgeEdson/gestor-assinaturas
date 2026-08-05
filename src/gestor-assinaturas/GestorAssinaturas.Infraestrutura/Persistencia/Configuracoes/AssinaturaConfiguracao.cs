using GestorAssinaturas.Dominio.Assinaturas;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestorAssinaturas.Infraestrutura.Persistencia.Configuracoes;

public sealed class AssinaturaConfiguracao : IEntityTypeConfiguration<Assinatura>
{
    public void Configure(EntityTypeBuilder<Assinatura> construtor)
    {
        construtor.ToTable("Assinaturas");

        construtor.HasKey(assinatura => assinatura.Identificador);

        construtor.Property(assinatura => assinatura.Identificador)
            .ValueGeneratedNever();

        construtor.Property(assinatura => assinatura.IdentificadorDoCliente)
            .IsRequired();

        construtor.HasOne(assinatura => assinatura.Plano)
            .WithMany()
            .HasForeignKey("IdentificadorPlano")
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        construtor.Navigation(assinatura => assinatura.Plano).AutoInclude();

        construtor.Property(assinatura => assinatura.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        construtor.Property(assinatura => assinatura.DataDeInicio)
            .IsRequired();

        construtor.Property(assinatura => assinatura.DataDeTerminoDoTrial);

        construtor.Property(assinatura => assinatura.DataDeCancelamentoAgendado);

        construtor.HasIndex(assinatura => assinatura.IdentificadorDoCliente);
    }
}
