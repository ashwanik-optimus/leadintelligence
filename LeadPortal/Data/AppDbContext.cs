using LeadPortal.Models;
using Microsoft.EntityFrameworkCore;

namespace LeadPortal.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Lead> Leads => Set<Lead>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Lead>(b =>
        {
            b.HasKey(l => l.Id);
            b.Property(l => l.BudgetBand).HasConversion<string>();
            b.Property(l => l.LocalStatus).HasConversion<string>();
            b.Property(l => l.HubSpotSyncStatus).HasConversion<string>();
        });
    }
}
