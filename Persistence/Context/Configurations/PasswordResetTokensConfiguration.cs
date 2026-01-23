using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Context.Configurations;

public class PasswordResetTokensConfiguration : IEntityTypeConfiguration<PasswordResetTokens>
{
    public void Configure(EntityTypeBuilder<PasswordResetTokens> builder)
    {
        // Table Name
        builder.ToTable("PasswordResetTokens");
        
        // Primary Key
        builder.HasKey(prt => prt.Id);
        
        // Guid: Required, Unique, MaxLength (hash SHA256 = 64 karakter)
        builder.Property(prt => prt.Guid)
            .IsRequired()
            .HasMaxLength(64);
        
        builder.HasIndex(prt => prt.Guid)
            .IsUnique(); // Aynı token 2 kez üretilemez
        
        // ExpiresAt: Required
        builder.Property(prt => prt.ExpiresAt)
            .IsRequired();
        
        // IsUsed: Default false
        builder.Property(prt => prt.IsUsed)
            .IsRequired()
            .HasDefaultValue(false);
        
        // RequestIp: Optional, MaxLength
        builder.Property(prt => prt.RequestIp)
            .HasMaxLength(50);
        
        // UsedAt: Optional
        builder.Property(prt => prt.UsedAt)
            .IsRequired(false);
        
        // Indexes (hızlı sorgulama için)
        builder.HasIndex(prt => prt.UserId);
        builder.HasIndex(prt => prt.IsUsed);
        builder.HasIndex(prt => prt.ExpiresAt);
        
        // Composite index: UserId + IsUsed (aktif tokenları bulmak için)
        builder.HasIndex(prt => new { prt.UserId, prt.IsUsed });
        
        // Foreign Key: User
        builder.HasOne(prt => prt.User)
            .WithMany() // Users entity'de navigation yok
            .HasForeignKey(prt => prt.UserId)
            .OnDelete(DeleteBehavior.Cascade); // Kullanıcı silinirse tokenları da sil
    }
}
