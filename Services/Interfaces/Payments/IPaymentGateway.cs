using Services.Payments;
using System.Threading.Tasks;

namespace Services.Interfaces.Payments
{
    public interface IPaymentGateway
    {
        // ارسال درخواست پرداخت به درگاه
        Task<GatewayPaymentRequestResult> RequestPaymentAsync(
            GatewayPaymentRequest request);

        // تأیید پرداخت از طریق درگاه
        Task<GatewayPaymentVerificationResult> VerifyPaymentAsync(
            string authority,
            long amount);
    }
}