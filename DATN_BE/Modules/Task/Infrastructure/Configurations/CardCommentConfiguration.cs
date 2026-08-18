using DATN_BE.Modules.Task.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DATN_BE.Modules.Task.Infrastructure.Configurations
{
    public class CardCommentConfiguration : IEntityTypeConfiguration<CardComment>
    {
        public void Configure(EntityTypeBuilder<CardComment> builder)
        {
            builder.ToTable("CardComments");
            builder.HasKey(cc => cc.Id);

            builder.Property(cc => cc.ContentText).IsRequired();

            // Quan hệ Card (1 Card có nhiều Comment)
            builder.HasOne(cc => cc.Card)
                .WithMany(c => c.Comments)
                .HasForeignKey(cc => cc.CardId)
                .OnDelete(DeleteBehavior.Cascade);

            // Quan hệ User (1 User có nhiều Comment)
            builder.HasOne(cc => cc.User)
                .WithMany(u => u.UserCardComments)
                .HasForeignKey(cc => cc.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
