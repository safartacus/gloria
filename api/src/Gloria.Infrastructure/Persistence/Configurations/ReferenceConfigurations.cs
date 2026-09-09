using Gloria.Domain.Reference;
using Gloria.Infrastructure.Persistence.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gloria.Infrastructure.Persistence.Configurations;

public sealed class SourceSystemConfiguration : IEntityTypeConfiguration<SourceSystem>
{
    public void Configure(EntityTypeBuilder<SourceSystem> b)
    {
        b.ToTable("source_systems");
        b.HasKey(x => x.Id);
        b.Property(x => x.Code).HasMaxLength(20).IsRequired();
        b.Property(x => x.Name).HasMaxLength(100).IsRequired();
        b.HasIndex(x => x.Code).IsUnique();
        b.HasData(SeedData.SourceSystems);
    }
}

public sealed class RuleTypeConfiguration : IEntityTypeConfiguration<RuleType>
{
    public void Configure(EntityTypeBuilder<RuleType> b)
    {
        b.ToTable("rule_types");
        b.HasKey(x => x.Id);
        b.Property(x => x.Code).HasMaxLength(40).IsRequired();
        b.Property(x => x.Name).HasMaxLength(100).IsRequired();
        b.Property(x => x.HandlerKey).HasMaxLength(40).IsRequired();
        b.Property(x => x.IsActive);
        b.HasIndex(x => x.Code).IsUnique();
        b.HasData(SeedData.RuleTypes);
    }
}
