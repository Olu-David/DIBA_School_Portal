using DIBA_School_Portal.Server.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class ClassBookConfiguration : IEntityTypeConfiguration<ClassBook>
{
    public void Configure(EntityTypeBuilder<ClassBook> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Title)
               .IsRequired()
               .HasMaxLength(150);

        builder.Property(x => x.Description)
               .HasMaxLength(300);

        builder.Property(x => x.Price)
               .HasColumnType("decimal(18,2)");

        builder.HasOne(x => x.SchoolClass)
               .WithMany()
               .HasForeignKey(x => x.SchoolClassId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.AcademicSession)
               .WithMany()
               .HasForeignKey(x => x.AcademicSessionId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Term)
               .WithMany()
               .HasForeignKey(x => x.TermId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}