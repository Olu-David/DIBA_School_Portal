using DIBA_School_Portal.Server.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DIBA_School_Portal.Server.Configurations.EntityConfigurations
{
    public class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
    {
        public void Configure(EntityTypeBuilder<Expense> builder)
        {
            builder.ToTable("Expenses");

            // ---------- Key ----------
            builder.HasKey(x => x.Id);

            // ---------- Columns ----------
            builder.Property(x => x.Id)
                .HasColumnName("Id")
                .ValueGeneratedNever();

            builder.Property(x => x.Category)
                .HasColumnName("Category")
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.Description)
                .HasColumnName("Description")
                .HasMaxLength(500)
                .IsRequired();

            builder.Property(x => x.Amount)
                .HasColumnName("Amount")
                .HasColumnType("numeric(18,2)")   // 👈 was decimal(18,2) — Postgres uses numeric
                .IsRequired();

            builder.Property(x => x.PaymentGateway)
                .HasColumnName("PaymentGateway")
                .HasConversion<int>()
                .IsRequired();

            builder.Property(x => x.Vendor)
                .HasColumnName("Vendor")
                .HasMaxLength(200)
                .IsRequired(false);

            builder.Property(x => x.ReferenceNumber)
                .HasColumnName("ReferenceNumber")
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.ExpenseDate)
                .HasColumnName("ExpenseDate")
                .IsRequired();

            builder.Property(x => x.ReceiptUrl)
                .HasColumnName("ReceiptUrl")
                .IsRequired(false);

            builder.Property(x => x.Notes)
                .HasColumnName("Notes")
                .IsRequired(false);

            builder.Property(x => x.RecordedByUserId)
                .HasColumnName("RecordedByUserId")
                .IsRequired();

            builder.Property(x => x.AcademicSessionId)
                .HasColumnName("AcademicSessionId")
                .IsRequired(false);

            builder.Property(x => x.TermId)
                .HasColumnName("TermId")
                .IsRequired(false);

            builder.Property(x => x.IsDeleted)
                .HasColumnName("IsDeleted")
                .HasDefaultValue(false)
                .IsRequired();

            builder.Property(x => x.CreatedAt)
                .HasColumnName("CreatedAt")
                .IsRequired();

            builder.Property(x => x.UpdatedAt)
                .HasColumnName("UpdatedAt")
                .IsRequired(false);

            // ---------- Indexes ----------
            builder.HasIndex(x => x.ExpenseDate)
                .HasDatabaseName("IX_Expenses_ExpenseDate");

            builder.HasIndex(x => x.Category)
                .HasDatabaseName("IX_Expenses_Category");

            builder.HasIndex(x => x.PaymentGateway)
                .HasDatabaseName("IX_Expenses_PaymentGateway");

            builder.HasIndex(x => x.AcademicSessionId)
                .HasDatabaseName("IX_Expenses_AcademicSessionId");

            builder.HasIndex(x => x.IsDeleted)
                .HasDatabaseName("IX_Expenses_IsDeleted");

            builder.HasIndex(x => x.ReferenceNumber)
                .HasDatabaseName("IX_Expenses_ReferenceNumber")
                .IsUnique();

         
            // builder.HasQueryFilter(x => !x.IsDeleted);
        }
    }
}