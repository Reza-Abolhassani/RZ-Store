using Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.Configurations
{
    public class UserWalletConfiguration : IEntityTypeConfiguration<UserWallet>
    {
        public void Configure(EntityTypeBuilder<UserWallet> builder)
        {
            builder.HasOne(x => x.User)
                .WithOne(x => x.UserWallet)
                .HasForeignKey<UserWallet>(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => x.UserId)
                .IsUnique();

            builder.Property(x => x.RowVersion)
                .IsRowVersion()
                .IsRequired();

            builder.ToTable(table =>
            {
                table.HasCheckConstraint(
                    "CK_UserWallets_Amount_NonNegative",
                    "[Amount] >= 0");
            });
        }
    }
}