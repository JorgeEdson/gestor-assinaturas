using GestorAssinaturas.Dominio.Assinaturas;
using GestorAssinaturas.Dominio.ObjetosDeValor;
using GestorAssinaturas.Dominio.Planos;
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

        construtor.Property(assinatura => assinatura.IdentificadorDoPlano)
            .IsRequired();

        construtor.HasOne<Plano>()
            .WithMany()
            .HasForeignKey(assinatura => assinatura.IdentificadorDoPlano)
            .OnDelete(DeleteBehavior.Restrict);

        construtor.OwnsOne(assinatura => assinatura.ValorDaAssinatura, valor =>
        {
            valor.Property(dinheiro => dinheiro.Valor)
                .HasColumnName("ValorDaAssinatura")
                .HasPrecision(18, Dinheiro.QuantidadeDeCasasDecimais)
                .IsRequired();

            valor.Property(dinheiro => dinheiro.Moeda)
                .HasColumnName("MoedaDaAssinatura")
                .HasMaxLength(Dinheiro.QuantidadeDeCaracteresDaMoeda)
                .IsRequired();
        });

        construtor.Navigation(assinatura => assinatura.ValorDaAssinatura).IsRequired();

        construtor.Property(assinatura => assinatura.CicloDeCobranca)
            .HasConversion(
                cicloDeCobranca => cicloDeCobranca.Tipo,
                tipo => CicloDeCobranca.APartirDoTipo(tipo).Instancia)
            .HasColumnName("CicloDeCobranca")
            .IsRequired();

        construtor.Property(assinatura => assinatura.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        construtor.Property(assinatura => assinatura.DataDeInicio)
            .IsRequired();

        construtor.Property(assinatura => assinatura.DataDeTerminoDoTrial);

        construtor.Property(assinatura => assinatura.DataDeInicioDaCobranca);

        construtor.Property(assinatura => assinatura.DataDeCancelamentoAgendado);

        construtor.HasIndex(assinatura => assinatura.IdentificadorDoCliente);
    }
}
