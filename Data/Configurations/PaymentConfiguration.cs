using Entities;
using Entities.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.Configurations
{
    public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
    {
        public void Configure(EntityTypeBuilder<Payment> builder)
        {
            // ارتباط با کاربر
            builder.HasOne(x => x.User)
                .WithMany(x => x.Payments)
                .HasForeignKey(x => x.UserId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            // ارتباط اختیاری با سفارش
            builder.HasOne(x => x.Order)
                .WithMany(x => x.Payments)
                .HasForeignKey(x => x.OrderId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            // محدودیت مبالغ
            builder.Property(x => x.Amount)
                .IsRequired();

            builder.Property(x => x.WalletAmount)
                .IsRequired();

            builder.Property(x => x.GatewayAmount)
                .IsRequired();

            // توضیحات
            builder.Property(x => x.Description)
                .HasMaxLength(500)
                .IsRequired(false);

            // Indexها
            builder.HasIndex(x => x.UserId);

            builder.HasIndex(x => x.OrderId)
                .IsUnique()
                .HasFilter("[OrderId] IS NOT NULL");

            builder.HasIndex(x => x.Status);

            builder.HasIndex(x => x.Type);

            // محدودیت‌های دیتابیسی
            builder.ToTable(table =>
            {
                // جلوگیری از مبالغ منفی
                table.HasCheckConstraint(
                    "CK_Payments_Amount_NonNegative",
                    "[Amount] >= 0");

                table.HasCheckConstraint(
                    "CK_Payments_WalletAmount_NonNegative",
                    "[WalletAmount] >= 0");

                table.HasCheckConstraint(
                    "CK_Payments_GatewayAmount_NonNegative",
                    "[GatewayAmount] >= 0");

                // حداقل یکی از دو مبلغ باید مثبت باشد
                table.HasCheckConstraint(
                    "CK_Payments_AtLeastOneAmount",
                    "[WalletAmount] > 0 OR [GatewayAmount] > 0");

                // مبلغ کل باید برابر مجموع دو بخش باشد
                // ابتدا از سرریز احتمالی جمع جلوگیری می‌کنیم
                table.HasCheckConstraint(
                    "CK_Payments_Amount_EqualsParts",
                    "[WalletAmount] <= [Amount] " +
                    "AND [GatewayAmount] = [Amount] - [WalletAmount]");

                // سازگاری نوع پرداخت با سفارش
                table.HasCheckConstraint(
                    "CK_Payments_Type_OrderId",
                    "([Type] = 0 AND [OrderId] IS NOT NULL) OR " +
                    "([Type] = 1 AND [OrderId] IS NULL)");

                // سازگاری روش پرداخت با مبالغ
                table.HasCheckConstraint(
                    "CK_Payments_Method_Amounts",
                    "([Method] = 0 AND [WalletAmount] > 0 AND [GatewayAmount] = 0) OR " +
                    "([Method] = 1 AND [WalletAmount] = 0 AND [GatewayAmount] > 0) OR " +
                    "([Method] = 2 AND [WalletAmount] > 0 AND [GatewayAmount] > 0)");

                // شارژ کیف پول فقط از طریق درگاه
                table.HasCheckConstraint(
                    "CK_Payments_WalletCharge_Method",
                    "[Type] <> 1 OR [Method] = 1");
            });
        }
    }
}