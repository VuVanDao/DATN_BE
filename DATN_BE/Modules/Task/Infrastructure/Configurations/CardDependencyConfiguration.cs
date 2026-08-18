using DATN_BE.Modules.Task.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DATN_BE.Modules.Task.Infrastructure.Configurations
{
    public class CardDependencyConfiguration : IEntityTypeConfiguration<CardDependency>
    {
        public void Configure(EntityTypeBuilder<CardDependency> builder)
        {
            builder.ToTable("CardDependencies");
            builder.HasKey(cd => cd.Id);

            builder.Property(cd => cd.DependencyType)
                .HasConversion<string>()
                .HasMaxLength(30);

            // Quan hệ Thẻ bị chặn
            builder.HasOne(cd => cd.BlockedCard)
                .WithMany(c => c.BlockedByDependencies)
                .HasForeignKey(cd => cd.BlockedCardId)
                .OnDelete(DeleteBehavior.Cascade);

            // Quan hệ Thẻ gây chặn
            builder.HasOne(cd => cd.BlockingCard)
                .WithMany(c => c.BlockingDependencies)
                .HasForeignKey(cd => cd.BlockingCardId)
                .OnDelete(DeleteBehavior.Cascade);

            // Ràng buộc Unique cặp (BlockedCardId, BlockingCardId) để không bị trùng quan hệ
            builder.HasIndex(cd => new { cd.BlockedCardId, cd.BlockingCardId }).IsUnique();
        }
    }
}
