using System.Security.Claims;
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
    
    // Mevcut kullanıcı ID'sini saklamak için property
    // UnitOfWork tarafından set edilecek
    public int? CurrentUserId { get; set; }
    
    public DbSet<Users> Users { get; set; }
    public DbSet<Posts> Posts { get; set; }
    public DbSet<Threads> Threads { get; set; }
    public DbSet<Categories> Categories { get; set; }
    public DbSet<PostVotes> PostVotes { get; set; }
    public DbSet<Notifications> Notifications { get; set; }

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
        modelBuilder.Entity<PostVotes>().HasQueryFilter(pv => !pv.IsDeleted);
        modelBuilder.Entity<Notifications>().HasQueryFilter(n => !n.IsDeleted);
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
                entry.Entity.CreatedUserId = CurrentUserId;
                entry.Entity.UpdatedUserId = CurrentUserId;
            }
            else if (entry.State == EntityState.Modified)
            {
                // Kayıt güncellendiğinde
                entry.Entity.UpdatedAt = DateTime.UtcNow;
                entry.Entity.UpdatedUserId = CurrentUserId;
                
                // Soft delete işlemi ise DeletedUserId'yi de set et
                if (entry.Entity.IsDeleted && entry.Entity.DeletedDate.HasValue)
                {
                    entry.Entity.DeletedUserId = CurrentUserId;
                }
            }
        }
        
        return base.SaveChangesAsync(cancellationToken);
    }
}