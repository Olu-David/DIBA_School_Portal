using DIBA_School_Portal.Server.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class ParentStudentConfiguration : IEntityTypeConfiguration<ParentStudent>
{
    public void Configure(EntityTypeBuilder<ParentStudent> builder)
    {
        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.Parent)
               .WithMany(u => u.Children)
               .HasForeignKey(x => x.ParentUserId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.StudentProfile)
               .WithMany()
               .HasForeignKey(x => x.StudentProfileId)
               .OnDelete(DeleteBehavior.Cascade);

        // One parent can be linked to the same student only once
        builder.HasIndex(x => new { x.ParentUserId, x.StudentProfileId })
               .IsUnique();
    }
}