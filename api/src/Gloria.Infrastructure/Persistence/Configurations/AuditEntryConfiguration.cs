using Gloria.Domain.Auditing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gloria.Infrastructure.Persistence.Configurations;

public sealed class AuditEntryConfiguration : IEntityTypeConfiguration<AuditEntry>
{
    public void Configure(EntityTypeBuilder<AuditEntry> b)
    {
        b.ToTable("audit_entries");
        b.HasKey(x => x.Id);
        b.Property(x => x.EntityName).HasMaxLength(80).IsRequired();
        b.Property(x => x.EntityId).HasMaxLength(40).IsRequired();
        b.Property(x => x.Action).HasMaxLength(10).IsRequired();
        b.Property(x => x.ChangesJson).IsRequired();
        b.Property(x => x.Username).HasMaxLength(50).IsRequired();
        b.Property(x => x.RoleCode).HasMaxLength(30).IsRequired();
        b.Property(x => x.TimestampUtc);
        b.Property(x => x.PreviousHash).HasMaxLength(64);
        b.Property(x => x.Hash).HasMaxLength(64);

        b.HasIndex(x => new { x.EntityName, x.EntityId });
        b.HasIndex(x => x.TimestampUtc);
    }
}
