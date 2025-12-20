using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Context.Configurations;

public class PostsConfiguration : IEntityTypeConfiguration<Posts>
{
    public void Configure(EntityTypeBuilder<Posts> builder)
    {
        // Table name
        builder.ToTable("Posts");

        // Primary Key
        builder.HasKey(p => p.Id);

        // Properties
        builder.Property(p => p.Content)
            .IsRequired()
            .HasMaxLength(5000);

        builder.Property(p => p.IsSolution)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(p => p.ThreadId)
            .IsRequired();

        builder.Property(p => p.UserId)
            .IsRequired();

        // BaseEntity properties
        builder.Property(p => p.CreatedAt)
            .IsRequired();

        builder.Property(p => p.UpdatedAt)
            .IsRequired();

        builder.Property(p => p.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(p => p.Recstatus)
            .IsRequired()
            .HasDefaultValue(true);

        // Indexes
        builder.HasIndex(p => p.ThreadId)
            .HasDatabaseName("IX_Posts_ThreadId");

        builder.HasIndex(p => p.UserId)
            .HasDatabaseName("IX_Posts_UserId");

        builder.HasIndex(p => p.IsSolution)
            .HasDatabaseName("IX_Posts_IsSolution");

        builder.HasIndex(p => p.IsDeleted)
            .HasDatabaseName("IX_Posts_IsDeleted");

        builder.HasIndex(p => p.CreatedAt)
            .HasDatabaseName("IX_Posts_CreatedAt");

        // Relationships - Foreign keys (ThreadsConfiguration ve UsersConfiguration'da tanımlandı)
    }
}
