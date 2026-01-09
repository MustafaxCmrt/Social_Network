using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Context.Configurations;

public class NotificationsConfiguration : IEntityTypeConfiguration<Notifications>
{
    public void Configure(EntityTypeBuilder<Notifications> builder)
    {
        builder.ToTable("Notifications");
        
        builder.HasKey(n => n.Id);
        
        // Properties
        builder.Property(n => n.Message)
            .IsRequired()
            .HasMaxLength(500);
        
        builder.Property(n => n.Type)
            .IsRequired();
        
        builder.Property(n => n.IsRead)
            .IsRequired()
            .HasDefaultValue(false);
        
        // Indexes
        builder.HasIndex(n => n.UserId)
            .HasDatabaseName("IX_Notifications_UserId");
        
        builder.HasIndex(n => n.IsRead)
            .HasDatabaseName("IX_Notifications_IsRead");
        
        builder.HasIndex(n => n.CreatedAt)
            .HasDatabaseName("IX_Notifications_CreatedAt");
        
        // Relationships
        // Bildirimi alan kullanıcı
        builder.HasOne(n => n.User)
            .WithMany(u => u.ReceivedNotifications)
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade); // User silinince bildirimleri de silinir
        
        // İşlemi yapan kullanıcı (opsiyonel)
        builder.HasOne(n => n.ActorUser)
            .WithMany(u => u.TriggeredNotifications)
            .HasForeignKey(n => n.ActorUserId)
            .OnDelete(DeleteBehavior.SetNull); // Actor silinince ActorUserId null olur
        
        // İlgili thread (opsiyonel)
        builder.HasOne(n => n.Thread)
            .WithMany(t => t.Notifications)
            .HasForeignKey(n => n.ThreadId)
            .OnDelete(DeleteBehavior.Cascade); // Thread silinince bildirimleri de silinir
        
        // İlgili post (opsiyonel)
        builder.HasOne(n => n.Post)
            .WithMany(p => p.Notifications)
            .HasForeignKey(n => n.PostId)
            .OnDelete(DeleteBehavior.Cascade); // Post silinince bildirimleri de silinir
    }
}
