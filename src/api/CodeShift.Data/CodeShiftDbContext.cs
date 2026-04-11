using CodeShift.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace CodeShift.Data;

public class CodeShiftDbContext : DbContext
{
    public CodeShiftDbContext(DbContextOptions<CodeShiftDbContext> options) : base(options) { }

    public DbSet<ProjectEntity> Projects => Set<ProjectEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProjectEntity>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Name).IsRequired().HasMaxLength(256);
            e.Property(p => p.Status).IsRequired().HasMaxLength(64);
        });
    }
}
