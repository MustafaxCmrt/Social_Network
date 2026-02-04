using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Context.Configurations;

public class CategoriesConfiguration : IEntityTypeConfiguration<Categories>
{
    public void Configure(EntityTypeBuilder<Categories> builder)
    {
        // Table name
        builder.ToTable("Categories");

        // Primary Key
        builder.HasKey(c => c.Id);

        // Properties
        builder.Property(c => c.Title)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.Slug)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(c => c.Description)
            .HasMaxLength(500);

        // BaseEntity properties - Audit Trail
        builder.Property(c => c.CreatedAt)
            .IsRequired();

        builder.Property(c => c.UpdatedAt)
            .IsRequired();
        
        builder.Property(c => c.CreatedUserId)
            .IsRequired(false); // Nullable
        
        builder.Property(c => c.UpdatedUserId)
            .IsRequired(false); // Nullable
        
        builder.Property(c => c.DeletedUserId)
            .IsRequired(false); // Nullable

        builder.Property(c => c.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(c => c.Recstatus)
            .IsRequired()
            .HasDefaultValue(true);

        // Indexes
        builder.HasIndex(c => c.Slug)
            .IsUnique()
            .HasDatabaseName("IX_Categories_Slug");

        builder.HasIndex(c => c.IsDeleted)
            .HasDatabaseName("IX_Categories_IsDeleted");
        
        builder.HasIndex(c => c.ParentCategoryId)
            .HasDatabaseName("IX_Categories_ParentCategoryId");
        
        // PERFORMANS: ClubId index (kulüp kategorilerini hızlı bulmak için)
        builder.HasIndex(c => c.ClubId)
            .HasDatabaseName("IX_Categories_ClubId");
        
        // Composite index: ClubId + IsDeleted (kulüp kategorilerini çekerken soft delete filtresi ile birlikte)
        builder.HasIndex(c => new { c.ClubId, c.IsDeleted })
            .HasDatabaseName("IX_Categories_ClubId_IsDeleted");

        // Relationships
        
        // Self-referencing relationship (Alt Kategoriler)
        builder.HasOne(c => c.ParentCategory)
            .WithMany(c => c.SubCategories)
            .HasForeignKey(c => c.ParentCategoryId)
            .OnDelete(DeleteBehavior.Restrict) // Üst kategori silindiğinde alt kategoriler korunsun
            .IsRequired(false);
        
        // Kulüp ilişkisi (Kulüp kategorileri için)
        builder.HasOne(c => c.Club)
            .WithMany() // Clubs entity'sinde Categories collection'ı yoksa WithMany() boş kalabilir
            .HasForeignKey(c => c.ClubId)
            .OnDelete(DeleteBehavior.Cascade) // Kulüp silindiğinde kategorileri de sil
            .IsRequired(false); // Nullable (normal forum kategorileri için null)
        
        builder.HasMany(c => c.Threads)
            .WithOne(t => t.Category)
            .HasForeignKey(t => t.CategoryId)
            .OnDelete(DeleteBehavior.Restrict); // Kategori silindiğinde thread'ler silinmesin
    }
}
