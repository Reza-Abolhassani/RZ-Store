using Entities.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.Configurations
{
    public class PaymentAttemptConfiguration : IEntityTypeConfiguration<PaymentAttempt>
    {
        public void Configure(EntityTypeBuilder<PaymentAttempt> builder)
        {
            // ارتباط با پرداخت اصلی
            builder.HasOne(x => x.Payment)
                .WithMany(x => x.PaymentAttempts)
                .HasForeignKey(x => x.PaymentId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            // نام درگاه
            builder.Property(x => x.GatewayName)
                .IsRequired()
                .HasMaxLength(100);

            // شناسه درخواست درگاه
            builder.Property(x => x.GatewayRequestId)
                .HasMaxLength(200)
                .IsRequired(false);

            // آدرس صفحه پرداخت درگاه
            builder.Property(x => x.PaymentUrl)
                .HasMaxLength(1000)
                .IsRequired(false);

            // کد رهگیری
            builder.Property(x => x.TrackingCode)
                .HasMaxLength(100)
                .IsRequired(false);

            // اطلاعات خطا
            builder.Property(x => x.ErrorCode)
                .HasMaxLength(100)
                .IsRequired(false);

            builder.Property(x => x.ErrorMessage)
                .HasMaxLength(1000)
                .IsRequired(false);

            // مبلغ
            builder.Property(x => x.Amount)
                .IsRequired();

            // کنترل هم‌زمانی تغییرات
            builder.Property(x => x.RowVersion)
                .IsRowVersion()
                .IsRequired();

            // Indexها
            builder.HasIndex(x => x.PaymentId);

            builder.HasIndex(x => x.PaymentId)
                .IsUnique()
                .HasFilter("[Status] IN (0, 4)");

            builder.HasIndex(x => x.Status);

            builder.HasIndex(x => x.GatewayRequestId)
                .IsUnique()
                .HasFilter("[GatewayRequestId] IS NOT NULL");

            builder.HasIndex(x => x.TrackingCode);

            // جلوگیری از مبلغ منفی
            builder.ToTable(table =>
            {
                table.HasCheckConstraint(
                    "CK_PaymentAttempts_Amount_Positive",
                    "[Amount] > 0");
            });
        }
    }
}