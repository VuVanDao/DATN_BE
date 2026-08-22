using DATN_BE.Modules.Task.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DATN_BE.Modules.Task.Infrastructure.Configurations
{
    public class CardConfiguration : IEntityTypeConfiguration<Card>
    {
        public void Configure(EntityTypeBuilder<Card> builder)
        {
            builder.ToTable("Cards");
            builder.HasKey(c => c.Id);

            builder.Property(c => c.Title).IsRequired().HasMaxLength(200);
            builder.Property(c => c.Type)
                .HasConversion<string>()
                .HasMaxLength(20);
            builder.Property(c => c.Priority)
                .HasConversion<string>()
                .HasMaxLength(20);
            builder.Property(c => c.CoverUrl).HasMaxLength(500);

            // Quan hệ Column (1 Column có nhiều Card)
            builder.HasOne(c => c.Column)
                .WithMany(col => col.Cards)
                .HasForeignKey(c => c.ColumnId)
                .OnDelete(DeleteBehavior.Cascade);

            // Quan hệ Assignee (1 User được giao nhiều Card)
            builder.HasOne(c => c.Assignee)
                .WithMany(u => u.AssignedCards)
                .HasForeignKey(c => c.AssigneeId)
                .OnDelete(DeleteBehavior.SetNull);

            // Quan hệ Reporter (1 User tạo nhiều Card)
            builder.HasOne(c => c.Reporter)
                .WithMany(u => u.ReportedCards)
                .HasForeignKey(c => c.ReporterId)
                .OnDelete(DeleteBehavior.Restrict);

            // Quan hệ Sprint (1 Sprint có nhiều Card)
            builder.HasOne(c => c.Sprint)
                .WithMany(s => s.Cards)
                .HasForeignKey(c => c.SprintId)
                .OnDelete(DeleteBehavior.SetNull);

            // Index thông thường phục vụ ORDER BY Position nhanh chóng
            builder.HasIndex(c => new { c.ColumnId, c.Position });
        }
    }
}
