using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Api.Models;

namespace SchoolAdmission.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<SchoolClass> Classes => Set<SchoolClass>();
    public DbSet<Hold> Holds => Set<Hold>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<SchoolClass>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.ToTable(t => t.HasCheckConstraint("CK_Classes_TotalSeats", "\"TotalSeats\" > 0"));
        });

        b.Entity<Hold>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.StudentId).HasMaxLength(17).IsRequired();
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.HasOne(x => x.Class).WithMany().HasForeignKey(x => x.ClassId);
            e.HasIndex(x => new { x.ClassId, x.Status });
            e.HasIndex(x => x.StudentId);
        });
    }
}