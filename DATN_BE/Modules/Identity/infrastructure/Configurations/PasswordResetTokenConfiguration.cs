using DATN_BE.Modules.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DATN_BE.Modules.Identity.Infrastructure.Configurations
{
    public class PasswordResetTokenConfiguration : IEntityTypeConfiguration<PasswordResetToken>
    {
        public void Configure(EntityTypeBuilder<PasswordResetToken> builder)
        {
            builder.ToTable("PasswordResetTokens");
            builder.HasKey(prt => prt.Id);
            builder.Property(prt => prt.Token)
                .IsRequired()
                .HasMaxLength(256);
            builder.HasIndex(prt => prt.Token);
            builder.HasOne(prt => prt.User)
                .WithMany(u => u.PasswordResetTokens)
                .HasForeignKey(prt => prt.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
