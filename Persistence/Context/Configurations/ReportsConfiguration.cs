using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Context.Configurations;

public class ReportsConfiguration : IEntityTypeConfiguration<Reports>
{
    public void Configure(EntityTypeBuilder<Reports> builder)
    {
        // Table name
        builder.ToTable("Reports");

        // Primary Key
        builder.HasKey(r => r.Id);

        // Properties
        builder.Property(r => r.ReporterId)
            .IsRequired();

        builder.Property(r => r.Reason)
            .IsRequired()
            .HasConversion<int>(); // Enum'u int olarak sakla

        builder.Property(r => r.Description)
            .HasMaxLength(1000);

        builder.Property(r => r.Status)
            .IsRequired()
            .HasConversion<int>() // Enum'u int olarak sakla
            .HasDefaultValue(ReportStatus.Pending);

        builder.Property(r => r.AdminNote)
            .HasMaxLength(500);

        // BaseEntity properties - Audit Trail
        builder.Property(r => r.CreatedAt)
            .IsRequired();

        builder.Property(r => r.UpdatedAt)
            .IsRequired();
        
        builder.Property(r => r.CreatedUserId)
            .IsRequired(false);
        
        builder.Property(r => r.UpdatedUserId)
            .IsRequired(false);
        
        builder.Property(r => r.DeletedUserId)
            .IsRequired(false);

        builder.Property(r => r.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(r => r.Recstatus)
            .IsRequired()
            .HasDefaultValue(true);

        // Indexes
        builder.HasIndex(r => r.ReporterId)
            .HasDatabaseName("IX_Reports_ReporterId");

        builder.HasIndex(r => r.Status)
            .HasDatabaseName("IX_Reports_Status");

        builder.HasIndex(r => r.CreatedAt)
            .HasDatabaseName("IX_Reports_CreatedAt");

        builder.HasIndex(r => r.IsDeleted)
            .HasDatabaseName("IX_Reports_IsDeleted");

        // Relationships
        
        // Reporter (Raporu yapan kullanıcı) - Zorunlu
        builder.HasOne(r => r.Reporter)
            .WithMany() // Users entity'sinde Reports collection yok, gerekirse ekleyebiliriz
            .HasForeignKey(r => r.ReporterId)
            .OnDelete(DeleteBehavior.Restrict) // Reporter silinse bile rapor korunsun
            .IsRequired();

        // ReportedUser (Rapor edilen kullanıcı) - Opsiyonel
        builder.HasOne(r => r.ReportedUser)
            .WithMany()
            .HasForeignKey(r => r.ReportedUserId)
            .OnDelete(DeleteBehavior.Restrict) // Kullanıcı silinse bile rapor korunsun
            .IsRequired(false);

        // ReportedPost (Rapor edilen post) - Opsiyonel
        builder.HasOne(r => r.ReportedPost)
            .WithMany()
            .HasForeignKey(r => r.ReportedPostId)
            .OnDelete(DeleteBehavior.Restrict) // Post silinse bile rapor korunsun
            .IsRequired(false);

        // ReportedThread (Rapor edilen thread) - Opsiyonel
        builder.HasOne(r => r.ReportedThread)
            .WithMany()
            .HasForeignKey(r => r.ReportedThreadId)
            .OnDelete(DeleteBehavior.Restrict) // Thread silinse bile rapor korunsun
            .IsRequired(false);

        // ReviewedByUser (İnceleyen admin) - Opsiyonel
        builder.HasOne(r => r.ReviewedByUser)
            .WithMany()
            .HasForeignKey(r => r.ReviewedByUserId)
            .OnDelete(DeleteBehavior.Restrict) // Admin silinse bile rapor korunsun
            .IsRequired(false);
    }
}
