using Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.Configurations
{
    public class TransactionHistoryConfiguration
        : IEntityTypeConfiguration<TransactionHistory>
    {
        public void Configure(EntityTypeBuilder<TransactionHistory> builder)
        {
            builder.HasOne(x => x.Payment)
                .WithMany(x => x.TransactionHistories)
                .HasForeignKey(x => x.PaymentId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            // ایندکس معمولی برای جست‌وجوی تراکنش‌های یک پرداخت
            builder.HasIndex(x => x.PaymentId);

            // جلوگیری از ثبت تراکنش هم‌نوع تکراری برای هر پرداخت
            builder.HasIndex(x => new { x.PaymentId, x.Type })
                .IsUnique()
                .HasFilter("[PaymentId] IS NOT NULL");
        }
    }
}