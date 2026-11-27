using Services.Payments;
using System;
using System.Threading.Tasks;

namespace Services.Interfaces
{
    public interface IPaymentService
    {
        // ایجاد درخواست پرداخت برای سفارش
        Task<OrderPaymentResult> CreateOrderPaymentAsync(
            Guid orderId,
            Guid userId,
            string callbackUrl);

        // بررسی و ثبت نتیجه پرداخت
        Task<OrderPaymentResult> VerifyOrderPaymentAsync(
            string authority,
            string status);
    }
}