using JobPilot.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace JobPilot.Api.Data;

public class JobPilotDbContext(DbContextOptions<JobPilotDbContext> options) : DbContext(options)
{
    public DbSet<JobApplication> Applications => Set<JobApplication>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<JobApplication>(e =>
        {
            e.ToTable("applications");
            e.HasKey(a => a.Id);
            e.Property(a => a.Company).HasMaxLength(200).IsRequired();
            e.Property(a => a.Role).HasMaxLength(200).IsRequired();
            e.Property(a => a.Link).HasMaxLength(2000);
            e.Property(a => a.Notes).HasMaxLength(4000);
            e.Property(a => a.Status).HasConversion<string>().HasMaxLength(20);
            e.HasIndex(a => a.Status);
        });
    }
}
