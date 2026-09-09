using Gloria.Domain.Periods;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gloria.Infrastructure.Persistence.Configurations;

public sealed class PeriodConfiguration : IEntityTypeConfiguration<Period>
{
    public void Configure(EntityTypeBuilder<Period> b)
    {
        b.ToTable("periods");
        b.HasKey(x => x.Id);
        b.Property(x => x.Year).IsRequired();
        b.Property(x => x.Month).IsRequired();
        b.Property(x => x.Status).HasMaxLength(10).IsRequired();
        b.Property(x => x.ClosedByUsername).HasMaxLength(50);
        b.Property(x => x.ClosedAt);
        b.Property(x => x.CreatedAt);
        b.Ignore(x => x.DomainEvents);
        b.HasIndex(x => new { x.Year, x.Month }).IsUnique();
    }
}
