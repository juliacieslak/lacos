using Lacos.Domain.Common;
using Lacos.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lacos.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuração comum a todas as entidades: chave primária uuid e carimbos de data com default now() (modelo de dados).
/// </summary>
public abstract class BaseEntityConfiguration<T> : IEntityTypeConfiguration<T> where T : BaseEntity
{
    public void Configure(EntityTypeBuilder<T> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");
        ConfigureEntity(builder);
    }

    protected abstract void ConfigureEntity(EntityTypeBuilder<T> builder);
}

/// <summary>
/// Configuração comum aos registros clínicos: vínculo obrigatório com o paciente, autor, último editor
/// e campos de exclusão lógica. As FKs usam RESTRICT: dados clínicos só são removidos pelo fluxo LGPD explícito.
/// </summary>
public abstract class PatientRecordConfiguration<T> : BaseEntityConfiguration<T> where T : PatientRecord
{
    protected override void ConfigureEntity(EntityTypeBuilder<T> builder)
    {
        builder.HasOne<Patient>().WithMany().HasForeignKey(e => e.PatientId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(e => e.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(e => e.UpdatedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(e => e.DeletedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(e => e.PatientId);
        ConfigureRecord(builder);
    }

    protected abstract void ConfigureRecord(EntityTypeBuilder<T> builder);
}
