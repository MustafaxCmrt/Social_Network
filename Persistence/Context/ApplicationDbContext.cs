using Domain.Common;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Context;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
    
    public DbSet<Users> Users { get; set; }
    public DbSet<Posts> Posts { get; set; }
    public DbSet<Threads> Threads { get; set; }
    public DbSet<Categories> Categories { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Entity Configurations'ları uygula
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Soft Delete Query Filter - Silinenleri otomatik gösterme
        modelBuilder.Entity<Users>().HasQueryFilter(u => !u.IsDeleted);
        modelBuilder.Entity<Posts>().HasQueryFilter(p => !p.IsDeleted);
        modelBuilder.Entity<Threads>().HasQueryFilter(t => !t.IsDeleted);
        modelBuilder.Entity<Categories>().HasQueryFilter(c => !c.IsDeleted);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries<BaseEntity>();
        
        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                // Yeni kayıt eklendiğinde
                entry.Entity.CreatedAt = DateTime.UtcNow;
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                // Kayıt güncellendiğinde
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
        }
        
        return base.SaveChangesAsync(cancellationToken);
    }
}