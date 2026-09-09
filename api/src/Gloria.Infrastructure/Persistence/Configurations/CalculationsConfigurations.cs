using Gloria.Domain.Calculations;
using Gloria.Domain.Periods;
using Gloria.Domain.Personnel;
using Gloria.Domain.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gloria.Infrastructure.Persistence.Configurations;

public sealed class CommissionResultConfiguration : IEntityTypeConfiguration<CommissionResult>
{
    public void Configure(EntityTypeBuilder<CommissionResult> b)
    {
        b.ToTable("commission_results");
        b.HasKey(x => x.Id);
        b.Property(x => x.TotalCommission);
        b.Property(x => x.SaleCount);
        b.Property(x => x.RefundCount);
        b.Property(x => x.UnmatchedCount);
        b.Property(x => x.Status).HasMaxLength(10).IsRequired();
        b.Property(x => x.CalculatedAt);
        b.Property(x => x.CalculatedByUsername).HasMaxLength(50).IsRequired();
        b.Ignore(x => x.DomainEvents);

        b.HasIndex(x => new { x.PeriodId, x.EmployeeId }).IsUnique();

        b.HasOne<Period>().WithMany().HasForeignKey(x => x.PeriodId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Employee>().WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);

        b.HasMany(x => x.Steps).WithOne().HasForeignKey(s => s.CommissionResultId).OnDelete(DeleteBehavior.Cascade);
        b.Metadata.FindNavigation(nameof(CommissionResult.Steps))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class CommissionResultStepConfiguration : IEntityTypeConfiguration<CommissionResultStep>
{
    public void Configure(EntityTypeBuilder<CommissionResultStep> b)
    {
        b.ToTable("commission_result_steps");
        b.HasKey(x => x.Id);
        b.Property(x => x.Order);
        b.Property(x => x.RuleTypeCode).HasMaxLength(40).IsRequired();
        b.Property(x => x.ProductCode).HasMaxLength(40).IsRequired();
        b.Property(x => x.Base);
        b.Property(x => x.AppliedRate);
        b.Property(x => x.AppliedAmount);
        b.Property(x => x.StepAmount);
        b.Property(x => x.RunningTotal);
        b.Property(x => x.IsRefund);
        b.Property(x => x.Explanation).HasMaxLength(500).IsRequired();

        b.HasIndex(x => new { x.CommissionResultId, x.Order });
        b.HasOne<SaleRecord>().WithMany().HasForeignKey(x => x.SaleRecordId).OnDelete(DeleteBehavior.Restrict);
    }
}
