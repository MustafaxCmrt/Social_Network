using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Context.Configurations;

public class ThreadsConfiguration : IEntityTypeConfiguration<Threads>
{
    public void Configure(EntityTypeBuilder<Threads> builder)
    {
        // Table name
        builder.ToTable("Threads");

        // Primary Key
        builder.HasKey(t => t.Id);

        // Properties
        builder.Property(t => t.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(t => t.Content)
            .IsRequired()
            .HasMaxLength(5000);

        builder.Property(t => t.ViewCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(t => t.IsSolved)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(t => t.UserId)
            .IsRequired();

        builder.Property(t => t.CategoryId)
            .IsRequired();

        // BaseEntity properties
        builder.Property(t => t.CreatedAt)
            .IsRequired();

        builder.Property(t => t.UpdatedAt)
            .IsRequired();

        builder.Property(t => t.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(t => t.Recstatus)
            .IsRequired()
            .HasDefaultValue(true);

        // Indexes
        builder.HasIndex(t => t.UserId)
            .HasDatabaseName("IX_Threads_UserId");

        builder.HasIndex(t => t.CategoryId)
            .HasDatabaseName("IX_Threads_CategoryId");

        builder.HasIndex(t => t.IsSolved)
            .HasDatabaseName("IX_Threads_IsSolved");

        builder.HasIndex(t => t.IsDeleted)
            .HasDatabaseName("IX_Threads_IsDeleted");

        builder.HasIndex(t => t.CreatedAt)
            .HasDatabaseName("IX_Threads_CreatedAt");

        // Relationships - Foreign keys (UsersConfiguration ve CategoriesConfiguration'da tanımlandı)
        builder.HasMany(t => t.Posts)
            .WithOne(p => p.Thread)
            .HasForeignKey(p => p.ThreadId)
            .OnDelete(DeleteBehavior.Cascade); // Thread silindiğinde post'lar da silinsin
    }
}
