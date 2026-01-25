using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Context.Configurations;

public class AuditLogsConfiguration : IEntityTypeConfiguration<AuditLogs>
{
    public void Configure(EntityTypeBuilder<AuditLogs> builder)
    {
        builder.ToTable("AuditLogs");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Username)
            .HasMaxLength(100);

        builder.Property(a => a.Action)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.EntityType)
            .HasMaxLength(50);

        builder.Property(a => a.OldValue)
            .HasColumnType("TEXT"); // JSON için TEXT kullanıyoruz

        builder.Property(a => a.NewValue)
            .HasColumnType("TEXT"); // JSON için TEXT kullanıyoruz

        builder.Property(a => a.IpAddress)
            .HasMaxLength(45); // IPv6 için 45 karakter yeterli

        builder.Property(a => a.UserAgent)
            .HasMaxLength(500);

        builder.Property(a => a.Success)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(a => a.ErrorMessage)
            .HasMaxLength(1000);

        builder.Property(a => a.Notes)
            .HasMaxLength(500);

        // Foreign Key - User silinse bile audit log kalır (nullable)
        builder.HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.SetNull); // User silindiğinde UserId NULL olur

        // Index'ler - Sorgulama performansı için
        builder.HasIndex(a => a.UserId);
        builder.HasIndex(a => a.Action);
        builder.HasIndex(a => a.EntityType);
        builder.HasIndex(a => a.CreatedAt); // Tarih bazlı sorgulama için
        builder.HasIndex(a => new { a.EntityType, a.EntityId }); // Entity bazlı geçmiş için
    }
}
