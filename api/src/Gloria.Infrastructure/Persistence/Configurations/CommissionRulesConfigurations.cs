using Gloria.Domain.CommissionRules;
using Gloria.Domain.Reference;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gloria.Infrastructure.Persistence.Configurations;

public sealed class CommissionRuleConfiguration : IEntityTypeConfiguration<CommissionRule>
{
    public void Configure(EntityTypeBuilder<CommissionRule> b)
    {
        b.ToTable("commission_rules");
        b.HasKey(x => x.Id);

        b.Property(x => x.RuleGroupCode).HasMaxLength(40).IsRequired();
        b.Property(x => x.Version).IsRequired();
        b.Property(x => x.Name).HasMaxLength(150).IsRequired();
        b.Property(x => x.Priority);
        b.Property(x => x.EffectiveFrom);
        b.Property(x => x.EffectiveTo);
        b.Property(x => x.IsActive);
        b.Property(x => x.Rate);
        b.Property(x => x.AmountPerTransaction);
        b.Property(x => x.BracketBasis).HasMaxLength(40);
        b.Property(x => x.CreatedByUsername).HasMaxLength(50).IsRequired();
        b.Property(x => x.CreatedAt);
        b.Ignore(x => x.DomainEvents);

        b.OwnsOne(x => x.Scope, s =>
        {
            s.Property(p => p.DepartmentCode).HasColumnName("scope_department_code").HasMaxLength(30);
            s.Property(p => p.ProductCodePattern).HasColumnName("scope_product_code_pattern").HasMaxLength(40);
            s.Property(p => p.SourceSystemCode).HasColumnName("scope_source_system_code").HasMaxLength(20);
        });
        b.Navigation(x => x.Scope).IsRequired();

        b.HasIndex(x => new { x.RuleGroupCode, x.Version }).IsUnique();
        b.HasIndex(x => new { x.EffectiveFrom, x.EffectiveTo });

        b.HasOne(x => x.RuleType).WithMany().HasForeignKey(x => x.RuleTypeId).OnDelete(DeleteBehavior.Restrict);

        b.HasMany(x => x.BracketTiers).WithOne().HasForeignKey(t => t.CommissionRuleId).OnDelete(DeleteBehavior.Cascade);
        b.Metadata.FindNavigation(nameof(CommissionRule.BracketTiers))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class BracketTierConfiguration : IEntityTypeConfiguration<BracketTier>
{
    public void Configure(EntityTypeBuilder<BracketTier> b)
    {
        b.ToTable("bracket_tiers");
        b.HasKey(x => x.Id);
        b.Property(x => x.Order).IsRequired();
        b.Property(x => x.LowerBound);
        b.Property(x => x.UpperBound);
        b.Property(x => x.Rate);
        b.HasIndex(x => new { x.CommissionRuleId, x.Order }).IsUnique();
    }
}
