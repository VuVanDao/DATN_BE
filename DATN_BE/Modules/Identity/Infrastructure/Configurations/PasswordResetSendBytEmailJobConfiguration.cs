using DATN_BE.Modules.Identity.Domain.Entities;
using DATN_BE.Modules.Identity.Domain.Job;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DATN_BE.Modules.Identity.Infrastructure.Configurations
{
    public class PasswordResetSendBytEmailJobConfiguration : IEntityTypeConfiguration<PasswordResetSendBytEmailJob>
    {
        public void Configure(EntityTypeBuilder<PasswordResetSendBytEmailJob> builder)
        {
            builder.ToTable("PasswordResetSendBytEmailJob");
            builder.HasKey(prt => prt.Id);
            builder.Property(prt => prt.PasswordResetTokenId)
                .IsRequired();
            builder.Property(prt => prt.ToEmail)
                .IsRequired()
                .HasMaxLength(250);
            builder.Property(r => r.Status)
                .HasConversion<string>()
                .IsRequired()
                .HasMaxLength(20);
            builder.Property(prt => prt.Attempts);
            builder.Property(prt => prt.LastError)
                .HasMaxLength(2000);
            builder.Property(prt => prt.NextAttemptAt)
                .IsRequired(false); 

            builder.Property(prt => prt.SentAt)
                .IsRequired(false);
            builder.HasIndex(prt => new {prt.Status, prt.NextAttemptAt});
            builder.HasOne<PasswordResetToken>().WithMany()
                   .HasForeignKey(job => job.PasswordResetTokenId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
