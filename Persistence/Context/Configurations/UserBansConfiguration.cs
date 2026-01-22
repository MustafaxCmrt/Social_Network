using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Context.Configurations;

public class UserBansConfiguration : IEntityTypeConfiguration<UserBans>
{
    public void Configure(EntityTypeBuilder<UserBans> builder)
    {
        // Table name
        builder.ToTable("UserBans");

        // Primary Key
        builder.HasKey(ub => ub.Id);

        // Properties
        builder.Property(ub => ub.UserId)
            .IsRequired();

        builder.Property(ub => ub.BannedByUserId)
            .IsRequired();

        builder.Property(ub => ub.Reason)
            .IsRequired()
            .HasMaxLength(500); // Sebep max 500 karakter

        builder.Property(ub => ub.BannedAt)
            .IsRequired();

        builder.Property(ub => ub.ExpiresAt)
            .IsRequired(false); // nullable - null = permanent ban

        builder.Property(ub => ub.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        // BaseEntity properties - Audit Trail
        builder.Property(ub => ub.CreatedAt)
            .IsRequired();

        builder.Property(ub => ub.UpdatedAt)
            .IsRequired();
        
        builder.Property(ub => ub.CreatedUserId)
            .IsRequired(false);
        
        builder.Property(ub => ub.UpdatedUserId)
            .IsRequired(false);
        
        builder.Property(ub => ub.DeletedUserId)
            .IsRequired(false);

        builder.Property(ub => ub.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(ub => ub.Recstatus)
            .IsRequired()
            .HasDefaultValue(true);

        // Indexes
        builder.HasIndex(ub => ub.UserId)
            .HasDatabaseName("IX_UserBans_UserId");

        builder.HasIndex(ub => ub.IsActive)
            .HasDatabaseName("IX_UserBans_IsActive");

        builder.HasIndex(ub => new { ub.UserId, ub.IsActive })
            .HasDatabaseName("IX_UserBans_UserId_IsActive"); // Composite index - aktif ban sorguları için

        // Foreign Keys
        
        // User (Yasaklanan)
        builder.HasOne(ub => ub.User)
            .WithMany() // Users entity'de UserBans collection yok
            .HasForeignKey(ub => ub.UserId)
            .OnDelete(DeleteBehavior.Restrict) // User silinse bile ban geçmişi korunsun
            .IsRequired();

        // BannedByUser (Yasaklayan admin)
        builder.HasOne(ub => ub.BannedByUser)
            .WithMany()
            .HasForeignKey(ub => ub.BannedByUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();
    }
}
