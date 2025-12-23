using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Context.Configurations;

public class UsersConfiguration : IEntityTypeConfiguration<Users>
{
    public void Configure(EntityTypeBuilder<Users> builder)
    {
        // Table name
        builder.ToTable("Users");

        // Primary Key
        builder.HasKey(u => u.Id);

        // Properties
        builder.Property(u => u.FirstName)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(u => u.LastName)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(u => u.Username)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(u => u.PasswordHash)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(u => u.ProfileImg)
            .HasMaxLength(500);

        builder.Property(u => u.Role)
            .IsRequired()
            .HasConversion<int>(); // Enum'u int olarak sakla

        builder.Property(u => u.IsActive)
            .IsRequired()
            .HasDefaultValue(true);
        
        // Refresh Token Versioning - her login/logout/refresh'de artar
        builder.Property(u => u.RefreshTokenVersion)
            .IsRequired()
            .HasDefaultValue(0);

        // BaseEntity properties - Audit Trail
        builder.Property(u => u.CreatedAt)
            .IsRequired();

        builder.Property(u => u.UpdatedAt)
            .IsRequired();
        
        builder.Property(u => u.CreatedUserId)
            .IsRequired(false); // Nullable
        
        builder.Property(u => u.UpdatedUserId)
            .IsRequired(false); // Nullable
        
        builder.Property(u => u.DeletedUserId)
            .IsRequired(false); // Nullable

        builder.Property(u => u.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(u => u.Recstatus)
            .IsRequired()
            .HasDefaultValue(true);

        // Indexes
        builder.HasIndex(u => u.Username)
            .IsUnique()
            .HasDatabaseName("IX_Users_Username");

        builder.HasIndex(u => u.Email)
            .IsUnique()
            .HasDatabaseName("IX_Users_Email");

        builder.HasIndex(u => u.IsDeleted)
            .HasDatabaseName("IX_Users_IsDeleted");

        // Relationships
        builder.HasMany(u => u.Threads)
            .WithOne(t => t.User)
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Restrict); // Kullanıcı silindiğinde thread'ler silinmesin

        builder.HasMany(u => u.Posts)
            .WithOne(p => p.User)
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Restrict); // Kullanıcı silindiğinde post'lar silinmesin

        // Seed Data - Admin kullanıcısı
        builder.HasData(new Users
        {
            Id = 1,
            FirstName = "Admin",
            LastName = "User",
            Username = "admin",
            Email = "admin@socialnetwork.com",
            // BCrypt hash for "Admin123." password
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123."),
            Role = Roles.Admin,
            IsActive = true,
            RefreshTokenVersion = 0, // Başlangıç versiyonu
            CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            IsDeleted = false,
            Recstatus = true
        });
    }
}
