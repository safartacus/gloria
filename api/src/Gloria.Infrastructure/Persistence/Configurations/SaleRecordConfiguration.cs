using Gloria.Domain.Imports;
using Gloria.Domain.Personnel;
using Gloria.Domain.Reference;
using Gloria.Domain.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gloria.Infrastructure.Persistence.Configurations;

public sealed class SaleRecordConfiguration : IEntityTypeConfiguration<SaleRecord>
{
    public void Configure(EntityTypeBuilder<SaleRecord> b)
    {
        b.ToTable("sale_records");
        b.HasKey(x => x.Id);

        b.Property(x => x.SourceTransactionNo).HasMaxLength(64).IsRequired();
        b.Property(x => x.ProductCode).HasMaxLength(40).IsRequired();
        b.Property(x => x.TransactionDate);
        b.Property(x => x.Year);
        b.Property(x => x.Month);
        b.Property(x => x.IsRefund);
        b.Property(x => x.OriginalSourceTransactionNo).HasMaxLength(64);
        b.Property(x => x.RawRecordJson).IsRequired();
        b.Property(x => x.CreatedAt);
        b.Ignore(x => x.DomainEvents);

        b.OwnsOne(x => x.Amount, m =>
        {
            m.Property(p => p.Amount).HasColumnName("amount").IsRequired();
            m.Property(p => p.Currency).HasColumnName("currency").HasMaxLength(3).IsRequired();
        });
        b.Navigation(x => x.Amount).IsRequired();

        b.HasIndex(x => new { x.SourceSystemId, x.SourceTransactionNo }).IsUnique();
        b.HasIndex(x => new { x.EmployeeId, x.Year, x.Month });
        b.HasIndex(x => x.ProductCode);

        b.HasOne<Employee>().WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Department>().WithMany().HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<SourceSystem>().WithMany().HasForeignKey(x => x.SourceSystemId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ImportBatch>().WithMany().HasForeignKey(x => x.ImportBatchId).OnDelete(DeleteBehavior.Restrict);
    }
}
