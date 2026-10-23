using Lacos.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lacos.Infrastructure.Persistence.Configurations;

/// <summary>Tabela users — colunas e restrições do modelo de dados + campos de RF01 e RF17.</summary>
public sealed class UserConfiguration : BaseEntityConfiguration<User>
{
    protected override void ConfigureEntity(EntityTypeBuilder<User> b)
    {
        b.ToTable("users");
        b.Property(e => e.PublicCode).HasMaxLength(12).IsRequired();
        b.HasIndex(e => e.PublicCode).IsUnique();
        b.Property(e => e.Auth0UserId).HasColumnName("auth0_user_id").HasMaxLength(255).IsRequired();
        b.HasIndex(e => e.Auth0UserId).IsUnique();
        b.Property(e => e.Name).HasMaxLength(150).IsRequired();
        b.Property(e => e.Email).HasMaxLength(255).IsRequired();
        b.HasIndex(e => e.Email).IsUnique();
        b.Property(e => e.Phone).HasMaxLength(30);
        b.Property(e => e.ProfileType).HasMaxLength(30);
        b.Property(e => e.ProfileCompleted).HasDefaultValue(false);
        b.Property(e => e.IsActive).HasDefaultValue(true);
        b.Property(e => e.CpfHash).HasMaxLength(64);
        b.HasIndex(e => e.CpfHash).IsUnique().HasFilter("cpf_hash IS NOT NULL");
        b.Property(e => e.Profession).HasMaxLength(100);
        b.Property(e => e.Cep).HasMaxLength(8);
    }
}

/// <summary>Tabela user_consents — prova de consentimento LGPD (RNF01).</summary>
public sealed class UserConsentConfiguration : BaseEntityConfiguration<UserConsent>
{
    protected override void ConfigureEntity(EntityTypeBuilder<UserConsent> b)
    {
        b.ToTable("user_consents");
        b.HasOne<User>().WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Restrict);
        b.Property(e => e.Version).HasMaxLength(20).IsRequired();
        b.Property(e => e.IpAddress).HasMaxLength(45);
        b.HasIndex(e => new { e.UserId, e.ConsentType });
    }
}

/// <summary>Tabela patients — RF03 e RF17.</summary>
public sealed class PatientConfiguration : BaseEntityConfiguration<Patient>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Patient> b)
    {
        b.ToTable("patients");
        b.Property(e => e.PublicCode).HasMaxLength(12).IsRequired();
        b.HasIndex(e => e.PublicCode).IsUnique();
        b.HasOne<User>().WithMany().HasForeignKey(e => e.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        b.Property(e => e.FullName).HasMaxLength(150).IsRequired();
        b.HasIndex(e => e.FullName);
        b.Property(e => e.BirthPlace).HasMaxLength(150).IsRequired();
        b.Property(e => e.Cep).HasMaxLength(8).IsRequired();
        b.Property(e => e.CpfHash).HasMaxLength(64);
        b.HasIndex(e => e.CpfHash).IsUnique().HasFilter("cpf_hash IS NOT NULL");
        b.Property(e => e.Email).HasMaxLength(255);
        b.Property(e => e.Phone).HasMaxLength(30);
        b.Property(e => e.IsActive).HasDefaultValue(true);
        b.ToTable(t => t.HasCheckConstraint("ck_patients_height_cm", "height_cm >= 0 AND height_cm <= 300"));
    }
}

/// <summary>Tabela patient_user_permissions — modelo de dados + can_delete_patient_data (RF07).</summary>
public sealed class PatientUserPermissionConfiguration : BaseEntityConfiguration<PatientUserPermission>
{
    protected override void ConfigureEntity(EntityTypeBuilder<PatientUserPermission> b)
    {
        b.ToTable("patient_user_permissions");
        b.HasOne(e => e.Patient).WithMany().HasForeignKey(e => e.PatientId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(e => new { e.PatientId, e.UserId }).IsUnique();
        b.HasIndex(e => new { e.UserId, e.IsActive });
        b.Property(e => e.RelationshipType).HasMaxLength(50).IsRequired();
        b.Property(e => e.CanManageMembers).HasDefaultValue(false);
        b.Property(e => e.CanViewPatient).HasDefaultValue(false);
        b.Property(e => e.CanAddPatientData).HasDefaultValue(false);
        b.Property(e => e.CanEditPatientData).HasDefaultValue(false);
        b.Property(e => e.CanDeletePatientData).HasDefaultValue(false);
        b.Property(e => e.IsActive).HasDefaultValue(true);
    }
}

/// <summary>Tabela patient_invitations — convites (US01).</summary>
public sealed class PatientInvitationConfiguration : BaseEntityConfiguration<PatientInvitation>
{
    protected override void ConfigureEntity(EntityTypeBuilder<PatientInvitation> b)
    {
        b.ToTable("patient_invitations");
        b.HasOne(e => e.Patient).WithMany().HasForeignKey(e => e.PatientId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<User>().WithMany().HasForeignKey(e => e.InvitedByUserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<User>().WithMany().HasForeignKey(e => e.AcceptedByUserId).OnDelete(DeleteBehavior.Restrict);
        b.Property(e => e.InvitedEmail).HasMaxLength(255).IsRequired();
        b.Property(e => e.RelationshipType).HasMaxLength(50).IsRequired();
        b.HasIndex(e => new { e.InvitedEmail, e.Status });
        b.HasIndex(e => new { e.PatientId, e.Status });
    }
}

/// <summary>Tabela notification_preferences — modelo de dados + health_threshold_alert (RF12).</summary>
public sealed class NotificationPreferenceConfiguration : BaseEntityConfiguration<NotificationPreference>
{
    protected override void ConfigureEntity(EntityTypeBuilder<NotificationPreference> b)
    {
        b.ToTable("notification_preferences");
        b.HasOne<Patient>().WithMany().HasForeignKey(e => e.PatientId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<User>().WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(e => new { e.PatientId, e.UserId }).IsUnique();
        b.Property(e => e.IsActive).HasDefaultValue(true);
    }
}

/// <summary>Tabela notifications — caixa de entrada e fila de envio.</summary>
public sealed class NotificationConfiguration : BaseEntityConfiguration<Notification>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Notification> b)
    {
        b.ToTable("notifications");
        b.HasOne<User>().WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
        b.Property(e => e.Title).HasMaxLength(150).IsRequired();
        b.Property(e => e.Message).HasMaxLength(1000).IsRequired();
        b.Property(e => e.DeduplicationKey).HasMaxLength(200).IsRequired();
        b.HasIndex(e => e.DeduplicationKey).IsUnique();
        b.Property(e => e.LastError).HasMaxLength(1000);
        b.HasIndex(e => new { e.UserId, e.ReadAt });
        b.HasIndex(e => new { e.Status, e.CreatedAt });
    }
}

/// <summary>
/// Tabela audit_logs — modelo de dados + ip_address/user_agent (RNF04).
/// As FKs usam RESTRICT: usuários e pacientes nunca são removidos fisicamente (a exclusão LGPD anonimiza o registro),
/// preservando a integridade da trilha, que é imutável (trigger no banco).
/// </summary>
public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> b)
    {
        b.ToTable("audit_logs");
        b.HasKey(e => e.Id);
        b.Property(e => e.Id).ValueGeneratedNever();
        b.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Patient>().WithMany().HasForeignKey(e => e.PatientId).OnDelete(DeleteBehavior.Restrict);
        b.Property(e => e.EntityType).HasMaxLength(100).IsRequired();
        b.Property(e => e.Action).HasMaxLength(30);
        b.Property(e => e.OldValues).HasColumnType("jsonb");
        b.Property(e => e.NewValues).HasColumnType("jsonb");
        b.Property(e => e.IpAddress).HasMaxLength(45);
        b.Property(e => e.UserAgent).HasMaxLength(512);
        b.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
        b.HasIndex(e => new { e.PatientId, e.CreatedAt });
        b.HasIndex(e => new { e.EntityType, e.EntityId });
    }
}
