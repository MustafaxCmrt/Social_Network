using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Context.Configurations;

public class ClubRequestsConfiguration : IEntityTypeConfiguration<ClubRequests>
{
    public void Configure(EntityTypeBuilder<ClubRequests> builder)
    {
        // Table name
        builder.ToTable("ClubRequests");

        // Primary Key
        builder.HasKey(cr => cr.Id);

        // Properties
        builder.Property(cr => cr.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(cr => cr.Description)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(cr => cr.Purpose)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(cr => cr.Status)
            .IsRequired()
            .HasDefaultValue(Domain.Enums.ClubRequestStatus.Pending);

        builder.Property(cr => cr.RequestedByUserId)
            .IsRequired();

        builder.Property(cr => cr.ReviewedByUserId)
            .IsRequired(false);

        builder.Property(cr => cr.ReviewedAt)
            .IsRequired(false);

        builder.Property(cr => cr.RejectionReason)
            .IsRequired(false)
            .HasMaxLength(500);

        // BaseEntity properties - Audit Trail
        builder.Property(cr => cr.CreatedAt)
            .IsRequired();

        builder.Property(cr => cr.UpdatedAt)
            .IsRequired();

        builder.Property(cr => cr.CreatedUserId)
            .IsRequired(false);

        builder.Property(cr => cr.UpdatedUserId)
            .IsRequired(false);

        builder.Property(cr => cr.DeletedUserId)
            .IsRequired(false);

        builder.Property(cr => cr.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(cr => cr.Recstatus)
            .IsRequired()
            .HasDefaultValue(true);

        // Indexes
        builder.HasIndex(cr => cr.RequestedByUserId)
            .HasDatabaseName("IX_ClubRequests_RequestedByUserId");

        builder.HasIndex(cr => cr.Status)
            .HasDatabaseName("IX_ClubRequests_Status");

        builder.HasIndex(cr => cr.ReviewedByUserId)
            .HasDatabaseName("IX_ClubRequests_ReviewedByUserId");

        builder.HasIndex(cr => cr.CreatedAt)
            .HasDatabaseName("IX_ClubRequests_CreatedAt");

        builder.HasIndex(cr => cr.IsDeleted)
            .HasDatabaseName("IX_ClubRequests_IsDeleted");

        // Relationships
        builder.HasOne(cr => cr.RequestedByUser)
            .WithMany()
            .HasForeignKey(cr => cr.RequestedByUserId)
            .OnDelete(DeleteBehavior.Restrict); // Kullanıcı silinse bile başvuru kayıtları kalsın

        builder.HasOne(cr => cr.ReviewedByUser)
            .WithMany()
            .HasForeignKey(cr => cr.ReviewedByUserId)
            .OnDelete(DeleteBehavior.SetNull); // Moderatör silinirse null olsun
    }
}
