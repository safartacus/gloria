using Gloria.Domain.Imports;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gloria.Infrastructure.Persistence.Configurations;

public sealed class ImportBatchConfiguration : IEntityTypeConfiguration<ImportBatch>
{
    public void Configure(EntityTypeBuilder<ImportBatch> b)
    {
        b.ToTable("import_batches");
        b.HasKey(x => x.Id);
        b.Property(x => x.BusinessDay).HasMaxLength(10).IsRequired();
        b.Property(x => x.Description).HasMaxLength(200).IsRequired();
        b.Property(x => x.Status).HasMaxLength(20).IsRequired();
        b.Property(x => x.StartedAt);
        b.Property(x => x.FinishedAt);
        b.Property(x => x.RowsRead);
        b.Property(x => x.RowsInserted);
        b.Property(x => x.RowsDuplicate);
        b.Property(x => x.RowsFailed);
        b.Property(x => x.RowsReconciled);
        b.Property(x => x.RunByUsername).HasMaxLength(50).IsRequired();
        b.Ignore(x => x.DomainEvents);

        b.HasMany(x => x.Errors).WithOne().HasForeignKey(e => e.ImportBatchId).OnDelete(DeleteBehavior.Cascade);
        b.Metadata.FindNavigation(nameof(ImportBatch.Errors))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class ImportErrorConfiguration : IEntityTypeConfiguration<ImportError>
{
    public void Configure(EntityTypeBuilder<ImportError> b)
    {
        b.ToTable("import_errors");
        b.HasKey(x => x.Id);
        b.Property(x => x.SourceSystemCode).HasMaxLength(20).IsRequired();
        b.Property(x => x.RowNumber);
        b.Property(x => x.RawContent).HasMaxLength(4000).IsRequired();
        b.Property(x => x.ErrorCode).HasMaxLength(40).IsRequired();
        b.Property(x => x.ErrorMessage).HasMaxLength(400).IsRequired();
        b.Property(x => x.TimestampUtc);
        b.HasIndex(x => x.ImportBatchId);
    }
}
