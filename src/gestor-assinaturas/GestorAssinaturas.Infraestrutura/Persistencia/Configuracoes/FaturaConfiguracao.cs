using GestorAssinaturas.Dominio.Faturas;
using GestorAssinaturas.Dominio.ObjetosDeValor;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestorAssinaturas.Infraestrutura.Persistencia.Configuracoes;

public sealed class FaturaConfiguracao : IEntityTypeConfiguration<Fatura>
{
    public void Configure(EntityTypeBuilder<Fatura> construtor)
    {
        construtor.ToTable("Faturas");

        construtor.HasKey(fatura => fatura.Identificador);

        construtor.Property(fatura => fatura.Identificador)
            .ValueGeneratedNever();

        construtor.Property(fatura => fatura.IdentificadorDaAssinatura)
            .IsRequired();

        construtor.OwnsOne(fatura => fatura.Valor, valor =>
        {
            valor.Property(dinheiro => dinheiro.Valor)
                .HasColumnName("Valor")
                .HasPrecision(18, Dinheiro.QuantidadeDeCasasDecimais)
                .IsRequired();

            valor.Property(dinheiro => dinheiro.Moeda)
                .HasColumnName("Moeda")
                .HasMaxLength(Dinheiro.QuantidadeDeCaracteresDaMoeda)
                .IsRequired();
        });

        construtor.Navigation(fatura => fatura.Valor).IsRequired();

        construtor.Property(fatura => fatura.DataDeVencimento)
            .IsRequired();

        construtor.Property(fatura => fatura.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        construtor.HasIndex(fatura => fatura.IdentificadorDaAssinatura);
    }
}
