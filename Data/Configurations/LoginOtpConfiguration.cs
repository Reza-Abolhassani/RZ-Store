using Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.Configurations
{
    public class LoginOtpConfiguration : IEntityTypeConfiguration<LoginOtp>
    {
        public void Configure(EntityTypeBuilder<LoginOtp> builder)
        {
            builder.Property(x => x.PhoneNumber)
                .IsRequired()
                .HasMaxLength(11);

            builder.Property(x => x.CodeHash)
                .IsRequired()
                .HasMaxLength(128);

            builder.Property(x => x.RequestIp)
                .HasMaxLength(45);

            builder.Property(x => x.AttemptCount)
                .HasDefaultValue(0);

            builder.Property(x => x.IsUsed)
                .HasDefaultValue(false);

            builder.HasIndex(x => new { x.PhoneNumber, x.CreationDateTime });

            builder.HasIndex(x => x.ExpiresAtUtc);
        }
    }
}