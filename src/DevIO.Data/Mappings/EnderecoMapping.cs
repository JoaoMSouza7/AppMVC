using DevIO.Business.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace DevIO.Data.Mappings
{
    class EnderecoMapping : IEntityTypeConfiguration<Endereco>
    {
        public void Configure(EntityTypeBuilder<Endereco> builder)
        {
            builder.HasKey(e => e.Id);

            builder.Property(e => e.Logradouro)
                .IsRequired()
                .HasColumnType("Varchar(200)");

            builder.Property(p => p.Numero)
                .IsRequired()
                .HasColumnType("Varchar(50)");

            builder.Property(p => p.Cep)
                .IsRequired()
                .HasColumnType("Varchar(8)");

            builder.Property(p => p.Complemento)
                .IsRequired()
                .HasColumnType("Varchar(250)");

            builder.Property(p => p.Bairro)
                .IsRequired()
                .HasColumnType("Varchar(100)");

            builder.Property(p => p.Cidade)
                .IsRequired()
                .HasColumnType("Varchar(100)");

            builder.Property(p => p.Estado)
                .IsRequired()
                .HasColumnType("Varchar(50)");

            builder.ToTable("Enderecos");
        }
    }
}
