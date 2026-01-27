using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SensorHub.Api.Models;

namespace SensorHub.Api.Data;

public class SensorHubDbContext : IdentityDbContext<ApplicationUser>
{
    public SensorHubDbContext(DbContextOptions<SensorHubDbContext> options) : base(options)
    {
    }

    public DbSet<Device> Devices => Set<Device>();
    public DbSet<Reading> Readings => Set<Reading>();
    public DbSet<AlertRule> AlertRules => Set<AlertRule>();
    public DbSet<Alert> Alerts => Set<Alert>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Device>(e =>
        {
            e.HasKey(d => d.Id);
            e.Property(d => d.Name).HasMaxLength(100).IsRequired();
            e.Property(d => d.Location).HasMaxLength(200);
            e.Property(d => d.ApiKeyHash).HasMaxLength(256);
            e.HasIndex(d => d.ApiKeyHash);
        });

        builder.Entity<Reading>(e =>
        {
            e.HasKey(r => r.Id);
            e.HasOne(r => r.Device)
                .WithMany(d => d.Readings)
                .HasForeignKey(r => r.DeviceId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(r => new { r.DeviceId, r.Timestamp });
        });

        builder.Entity<AlertRule>(e =>
        {
            e.HasKey(a => a.Id);
            e.HasOne(a => a.Device)
                .WithMany(d => d.AlertRules)
                .HasForeignKey(a => a.DeviceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Alert>(e =>
        {
            e.HasKey(a => a.Id);
            e.HasOne(a => a.AlertRule)
                .WithMany(ar => ar.Alerts)
                .HasForeignKey(a => a.AlertRuleId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(a => a.Device)
                .WithMany()
                .HasForeignKey(a => a.DeviceId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(a => a.CreatedAt);
        });
    }
}
