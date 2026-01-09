using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Context.Configurations;

public class PostVotesConfiguration : IEntityTypeConfiguration<PostVotes>
{
    public void Configure(EntityTypeBuilder<PostVotes> builder)
    {
        builder.ToTable("PostVotes");
        
        builder.HasKey(pv => pv.Id);
        
        // Bir kullanıcı bir post'a sadece 1 kere upvote verebilir
        builder.HasIndex(pv => new { pv.PostId, pv.UserId })
            .IsUnique()
            .HasDatabaseName("IX_PostVotes_PostId_UserId_Unique");
        
        // Relationships
        builder.HasOne(pv => pv.Post)
            .WithMany(p => p.Votes)
            .HasForeignKey(pv => pv.PostId)
            .OnDelete(DeleteBehavior.Cascade); // Post silinince upvote'ları da silinir
        
        builder.HasOne(pv => pv.User)
            .WithMany(u => u.PostVotes)
            .HasForeignKey(pv => pv.UserId)
            .OnDelete(DeleteBehavior.Cascade); // User silinince upvote'ları da silinir
    }
}
