using AcxiomCRM.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
namespace AcxiomCRM.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Lead> Leads => Set<Lead>();
    public DbSet<Opportunity> Opportunities => Set<Opportunity>();
    public DbSet<FollowUp> FollowUps => Set<FollowUp>();
    public DbSet<Activity> Activities => Set<Activity>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);
        b.Entity<ApplicationUser>().Property(u => u.FullName).HasMaxLength(100);

        b.Entity<Customer>(e =>
        {
            e.HasIndex(x => x.Email).IsUnique();     // 17.5 email uniqueness
            e.HasIndex(x => x.Phone).IsUnique();     // 17.5 phone uniqueness
            e.HasIndex(x => x.CustomerCode).IsUnique();
        });
        b.Entity<Lead>().HasIndex(x => x.LeadCode).IsUnique();
        b.Entity<Opportunity>(e =>
        {
            e.Property(x => x.Amount).HasColumnType("decimal(18,2)");
            e.Ignore(x => x.WeightedAmount);
            e.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Lead).WithMany().HasForeignKey(x => x.LeadId).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<Lead>().Property(x => x.ExpectedValue).HasColumnType("decimal(18,2)");
        b.Entity<FollowUp>(e =>
        {
            e.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Lead).WithMany().HasForeignKey(x => x.LeadId).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<Activity>(e =>
        {
            e.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Lead).WithMany().HasForeignKey(x => x.LeadId).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<AuditLog>().HasIndex(x => x.CreatedDate);
    }

    // Audit logs are append-only (spec 4.9): block updates/deletes at the context level.
    public override Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        foreach (var entry in ChangeTracker.Entries<AuditLog>())
            if (entry.State is EntityState.Modified or EntityState.Deleted)
                throw new InvalidOperationException("Audit log records are append-only.");
        return base.SaveChangesAsync(ct);
    }
}
