using DATN_BE.Modules.Notifications.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DATN_BE.Modules.Notifications.Infrastructure.Configurations
{
    public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
    {
        public void Configure(EntityTypeBuilder<Notification> builder)
        {
            builder.ToTable("Notifications");

            builder.HasKey(n => n.Id);

            builder.Property(n => n.Title)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(n => n.Content)
                .IsRequired();

            builder.Property(n => n.Type)
                .HasConversion<string>()
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(n => n.EntityType)
                .HasMaxLength(50);

            builder.Property(n => n.IsRead)
                .HasDefaultValue(false);

            // Quan hệ Người nhận thông báo
            builder.HasOne(n => n.Receiver)
                .WithMany(u => u.ReceivedNotifications)
                .HasForeignKey(n => n.ReceiverId)
                .OnDelete(DeleteBehavior.Cascade);

            // Quan hệ Người gửi thông báo
            builder.HasOne(n => n.Sender)
                .WithMany(u => u.SentNotifications)
                .HasForeignKey(n => n.SenderId)
                .OnDelete(DeleteBehavior.SetNull);

            // Chỉ mục cực kỳ quan trọng giúp tải thông báo chưa đọc siêu nhanh
            builder.HasIndex(n => new { n.ReceiverId, n.IsRead, n.CreatedAt });
        }
    }
}
