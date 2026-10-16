using BudgetCivil.Domain.Users.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetCivil.Infrastructure.Persistence.Configurations;

public sealed class SecurityAuditEventConfiguration : IEntityTypeConfiguration<SecurityAuditEvent>
{
    public void Configure(EntityTypeBuilder<SecurityAuditEvent> builder)
    {
        builder.ToTable("security_audit_events");

        builder.HasKey(auditEvent => auditEvent.Id);

        builder.Property(auditEvent => auditEvent.Id)
            .HasColumnName("id");

        builder.Property(auditEvent => auditEvent.ActorUserId)
            .HasColumnName("actor_user_id");

        builder.Property(auditEvent => auditEvent.CompanyId)
            .HasColumnName("company_id");

        builder.Property(auditEvent => auditEvent.EventType)
            .HasColumnName("event_type")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(auditEvent => auditEvent.PermissionKey)
            .HasColumnName("permission_key")
            .HasMaxLength(100);

        builder.Property(auditEvent => auditEvent.TargetType)
            .HasColumnName("target_type")
            .HasMaxLength(50);

        builder.Property(auditEvent => auditEvent.TargetId)
            .HasColumnName("target_id");

        builder.Property(auditEvent => auditEvent.ReasonCode)
            .HasColumnName("reason_code")
            .HasMaxLength(80);

        builder.Property(auditEvent => auditEvent.Result)
            .HasColumnName("result")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(auditEvent => auditEvent.OccurredAtUtc)
            .HasColumnName("occurred_at_utc")
            .IsRequired();

        builder.Property(auditEvent => auditEvent.MetadataJson)
            .HasColumnName("metadata_json");

        builder.HasIndex(auditEvent => auditEvent.OccurredAtUtc);

        builder.HasOne(auditEvent => auditEvent.ActorUser)
            .WithMany(user => user.SecurityAuditEvents)
            .HasForeignKey(auditEvent => auditEvent.ActorUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
