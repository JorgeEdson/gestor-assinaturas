using GestorAssinaturas.Dominio.ObjetosDeValor;
using GestorAssinaturas.Dominio.Planos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestorAssinaturas.Infraestrutura.Persistencia.Configuracoes;

public sealed class PlanoConfiguracao : IEntityTypeConfiguration<Plano>
{
    public void Configure(EntityTypeBuilder<Plano> construtor)
    {
        construtor.ToTable("Planos");

        construtor.HasKey(plano => plano.Identificador);

        construtor.Property(plano => plano.Identificador)
            .ValueGeneratedNever();

        construtor.Property(plano => plano.Nome)
            .HasMaxLength(Plano.QuantidadeMaximaDeCaracteresDoNome)
            .IsRequired();

        construtor.OwnsOne(plano => plano.Preco, preco =>
        {
            preco.Property(dinheiro => dinheiro.Valor)
                .HasColumnName("PrecoValor")
                .HasPrecision(18, Dinheiro.QuantidadeDeCasasDecimais)
                .IsRequired();

            preco.Property(dinheiro => dinheiro.Moeda)
                .HasColumnName("PrecoMoeda")
                .HasMaxLength(Dinheiro.QuantidadeDeCaracteresDaMoeda)
                .IsRequired();
        });

        construtor.Navigation(plano => plano.Preco).IsRequired();

        construtor.Property(plano => plano.CicloDeCobranca)
            .HasConversion(
                cicloDeCobranca => cicloDeCobranca.Tipo,
                tipo => CicloDeCobranca.APartirDoTipo(tipo).Instancia)
            .HasColumnName("CicloDeCobranca")
            .IsRequired();

        construtor.Property(plano => plano.PeriodoDeTrialEmDias)
            .IsRequired();
    }
}
