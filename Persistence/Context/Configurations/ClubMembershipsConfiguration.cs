using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Context.Configurations;

public class ClubMembershipsConfiguration : IEntityTypeConfiguration<ClubMemberships>
{
    public void Configure(EntityTypeBuilder<ClubMemberships> builder)
    {
        // Table name
        builder.ToTable("ClubMemberships");

        // Primary Key
        builder.HasKey(cm => cm.Id);

        // Properties
        builder.Property(cm => cm.ClubId)
            .IsRequired();

        builder.Property(cm => cm.UserId)
            .IsRequired();

        builder.Property(cm => cm.Role)
            .IsRequired()
            .HasDefaultValue(Domain.Enums.ClubRole.Member);

        builder.Property(cm => cm.Status)
            .IsRequired()
            .HasDefaultValue(Domain.Enums.MembershipStatus.Pending);

        builder.Property(cm => cm.JoinedAt)
            .IsRequired(false);

        builder.Property(cm => cm.JoinNote)
            .IsRequired(false)
            .HasMaxLength(500);

        // BaseEntity properties - Audit Trail
        builder.Property(cm => cm.CreatedAt)
            .IsRequired();

        builder.Property(cm => cm.UpdatedAt)
            .IsRequired();

        builder.Property(cm => cm.CreatedUserId)
            .IsRequired(false);

        builder.Property(cm => cm.UpdatedUserId)
            .IsRequired(false);

        builder.Property(cm => cm.DeletedUserId)
            .IsRequired(false);

        builder.Property(cm => cm.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(cm => cm.Recstatus)
            .IsRequired()
            .HasDefaultValue(true);

        // Indexes
        // Composite unique index - Bir kullanıcı aynı kulüpte tek üyelik
        builder.HasIndex(cm => new { cm.ClubId, cm.UserId })
            .IsUnique()
            .HasDatabaseName("IX_ClubMemberships_ClubId_UserId");

        builder.HasIndex(cm => cm.ClubId)
            .HasDatabaseName("IX_ClubMemberships_ClubId");

        builder.HasIndex(cm => cm.UserId)
            .HasDatabaseName("IX_ClubMemberships_UserId");

        builder.HasIndex(cm => cm.Status)
            .HasDatabaseName("IX_ClubMemberships_Status");

        builder.HasIndex(cm => cm.Role)
            .HasDatabaseName("IX_ClubMemberships_Role");

        builder.HasIndex(cm => new { cm.ClubId, cm.Status })
            .HasDatabaseName("IX_ClubMemberships_ClubId_Status");

        builder.HasIndex(cm => cm.IsDeleted)
            .HasDatabaseName("IX_ClubMemberships_IsDeleted");

        // Relationships
        // Club ilişkisi ClubsConfiguration'da tanımlandı

        builder.HasOne(cm => cm.User)
            .WithMany()
            .HasForeignKey(cm => cm.UserId)
            .OnDelete(DeleteBehavior.Cascade); // Kullanıcı silindiğinde üyelikleri de silinsin
    }
}
