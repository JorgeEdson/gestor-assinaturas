using GestorAssinaturas.Dominio.Clientes;
using GestorAssinaturas.Dominio.ObjetosDeValor;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestorAssinaturas.Infraestrutura.Persistencia.Configuracoes;

public sealed class ClienteConfiguracao : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> construtor)
    {
        construtor.ToTable("Clientes");

        construtor.HasKey(cliente => cliente.Identificador);

        construtor.Property(cliente => cliente.Identificador)
            .ValueGeneratedNever();

        construtor.Property(cliente => cliente.Nome)
            .HasMaxLength(Cliente.QuantidadeMaximaDeCaracteresDoNome)
            .IsRequired();

        construtor.Property(cliente => cliente.Email)
            .HasConversion(
                email => email.Endereco,
                endereco => Email.Criar(endereco).Instancia)
            .HasColumnName("Email")
            .HasMaxLength(Email.QuantidadeMaximaDeCaracteres)
            .IsRequired();
    }
}
