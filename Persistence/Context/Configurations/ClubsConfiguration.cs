using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Context.Configurations;

public class ClubsConfiguration : IEntityTypeConfiguration<Clubs>
{
    public void Configure(EntityTypeBuilder<Clubs> builder)
    {
        // Table name
        builder.ToTable("Clubs");

        // Primary Key
        builder.HasKey(c => c.Id);

        // Properties
        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.Slug)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.Description)
            .IsRequired(false)
            .HasMaxLength(2000);

        builder.Property(c => c.LogoUrl)
            .IsRequired(false)
            .HasMaxLength(500);

        builder.Property(c => c.BannerUrl)
            .IsRequired(false)
            .HasMaxLength(500);

        builder.Property(c => c.IsPublic)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(c => c.RequiresApproval)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(c => c.MemberCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(c => c.FounderId)
            .IsRequired();

        // Application Status Properties
        builder.Property(c => c.ApplicationStatus)
            .IsRequired()
            .HasDefaultValue(ClubApplicationStatus.Pending)
            .HasConversion<int>(); // Enum'u int olarak sakla

        builder.Property(c => c.RejectionReason)
            .IsRequired(false)
            .HasMaxLength(500);

        builder.Property(c => c.ReviewedAt)
            .IsRequired(false);

        builder.Property(c => c.ReviewedBy)
            .IsRequired(false);

        // BaseEntity properties - Audit Trail
        builder.Property(c => c.CreatedAt)
            .IsRequired();

        builder.Property(c => c.UpdatedAt)
            .IsRequired();

        builder.Property(c => c.CreatedUserId)
            .IsRequired(false);

        builder.Property(c => c.UpdatedUserId)
            .IsRequired(false);

        builder.Property(c => c.DeletedUserId)
            .IsRequired(false);

        builder.Property(c => c.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(c => c.Recstatus)
            .IsRequired()
            .HasDefaultValue(true);

        // Indexes
        builder.HasIndex(c => c.Slug)
            .IsUnique()
            .HasDatabaseName("IX_Clubs_Slug");

        builder.HasIndex(c => c.Name)
            .IsUnique()
            .HasDatabaseName("IX_Clubs_Name");

        builder.HasIndex(c => c.FounderId)
            .HasDatabaseName("IX_Clubs_FounderId");

        builder.HasIndex(c => c.IsPublic)
            .HasDatabaseName("IX_Clubs_IsPublic");

        builder.HasIndex(c => c.MemberCount)
            .HasDatabaseName("IX_Clubs_MemberCount");

        builder.HasIndex(c => c.CreatedAt)
            .HasDatabaseName("IX_Clubs_CreatedAt");

        builder.HasIndex(c => c.IsDeleted)
            .HasDatabaseName("IX_Clubs_IsDeleted");

        builder.HasIndex(c => c.ApplicationStatus)
            .HasDatabaseName("IX_Clubs_ApplicationStatus");

        builder.HasIndex(c => c.ReviewedAt)
            .HasDatabaseName("IX_Clubs_ReviewedAt");

        // Relationships
        builder.HasOne(c => c.Founder)
            .WithMany()
            .HasForeignKey(c => c.FounderId)
            .OnDelete(DeleteBehavior.Restrict); // Kurucu silinse bile kulüp kalsın

        // ReviewedBy - Admin/Moderator relationship (optional)
        builder.HasOne<Users>()
            .WithMany()
            .HasForeignKey(c => c.ReviewedBy)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false); // Reviewer silinse bile kulüp kalsın

        builder.HasMany(c => c.Memberships)
            .WithOne(cm => cm.Club)
            .HasForeignKey(cm => cm.ClubId)
            .OnDelete(DeleteBehavior.Cascade); // Kulüp silindiğinde üyelikler de silinsin

        builder.HasMany(c => c.Threads)
            .WithOne(t => t.Club)
            .HasForeignKey(t => t.ClubId)
            .OnDelete(DeleteBehavior.SetNull); // Kulüp silindiğinde thread'ler genel foruma geçsin
    }
}
