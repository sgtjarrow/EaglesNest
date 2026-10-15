using EaglesNest.Domain.Auditing;
using EaglesNest.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EaglesNest.Infrastructure.Data;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(x => x.ApprovalStatus).HasConversion<string>().HasMaxLength(24);
            entity.HasIndex(x => x.NormalizedEmail).IsUnique();
        });

        builder.Entity<RolePermission>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Permission).HasMaxLength(128);
            entity.HasIndex(x => new { x.RoleId, x.Permission }).IsUnique();
            entity.HasOne<Microsoft.AspNetCore.Identity.IdentityRole>()
                .WithMany()
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<AuditEvent>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ActorName).HasMaxLength(256);
            entity.Property(x => x.Action).HasMaxLength(128);
            entity.Property(x => x.DetailsJson).HasColumnType("nvarchar(max)");
            entity.Property(x => x.Notes).HasMaxLength(2000);
            entity.HasIndex(x => x.OccurredAtUtc);
            entity.HasIndex(x => x.Action);
        });
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        GuardAppendOnlyAuditEvents();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        GuardAppendOnlyAuditEvents();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void GuardAppendOnlyAuditEvents()
    {
        if (ChangeTracker.Entries<AuditEvent>().Any(x => x.State is EntityState.Modified or EntityState.Deleted))
        {
            throw new InvalidOperationException("Audit events are append-only and cannot be changed or deleted.");
        }
    }
}
