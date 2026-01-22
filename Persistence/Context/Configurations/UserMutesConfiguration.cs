using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Context.Configurations;

public class UserMutesConfiguration : IEntityTypeConfiguration<UserMutes>
{
    public void Configure(EntityTypeBuilder<UserMutes> builder)
    {
        // Table name
        builder.ToTable("UserMutes");

        // Primary Key
        builder.HasKey(um => um.Id);

        // Properties
        builder.Property(um => um.UserId)
            .IsRequired();

        builder.Property(um => um.MutedByUserId)
            .IsRequired();

        builder.Property(um => um.Reason)
            .IsRequired()
            .HasMaxLength(500); // Sebep max 500 karakter

        builder.Property(um => um.MutedAt)
            .IsRequired();

        builder.Property(um => um.ExpiresAt)
            .IsRequired(); // Mute her zaman geçici - ExpiresAt zorunlu

        builder.Property(um => um.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        // BaseEntity properties - Audit Trail
        builder.Property(um => um.CreatedAt)
            .IsRequired();

        builder.Property(um => um.UpdatedAt)
            .IsRequired();
        
        builder.Property(um => um.CreatedUserId)
            .IsRequired(false);
        
        builder.Property(um => um.UpdatedUserId)
            .IsRequired(false);
        
        builder.Property(um => um.DeletedUserId)
            .IsRequired(false);

        builder.Property(um => um.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(um => um.Recstatus)
            .IsRequired()
            .HasDefaultValue(true);

        // Indexes
        builder.HasIndex(um => um.UserId)
            .HasDatabaseName("IX_UserMutes_UserId");

        builder.HasIndex(um => um.IsActive)
            .HasDatabaseName("IX_UserMutes_IsActive");

        builder.HasIndex(um => new { um.UserId, um.IsActive })
            .HasDatabaseName("IX_UserMutes_UserId_IsActive"); // Composite index - aktif mute sorguları için

        // Foreign Keys
        
        // User (Susturulan)
        builder.HasOne(um => um.User)
            .WithMany() // Users entity'de UserMutes collection yok
            .HasForeignKey(um => um.UserId)
            .OnDelete(DeleteBehavior.Restrict) // User silinse bile mute geçmişi korunsun
            .IsRequired();

        // MutedByUser (Sustururan admin)
        builder.HasOne(um => um.MutedByUser)
            .WithMany()
            .HasForeignKey(um => um.MutedByUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();
    }
}