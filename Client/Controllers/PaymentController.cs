using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;
using System;
using System.Threading.Tasks;

namespace Client.Controllers
{
    public class PaymentController : Controller
    {
        private readonly IPaymentService _paymentService;

        public PaymentController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        [HttpGet]
        public async Task<IActionResult> Callback(string? authority, string? status)
        {
            if (string.IsNullOrWhiteSpace(authority))
            {
                TempData["ErrorMessage"] = "اطلاعات بازگشت از درگاه پرداخت معتبر نیست.";
                return RedirectToAction("Index", "Home");
            }

            var result = await _paymentService.VerifyOrderPaymentAsync(
                authority,
                status ?? string.Empty);

            if (result.IsSuccess)
            {
                TempData["SuccessMessage"] = "پرداخت با موفقیت انجام شد.";
            }
            else
            {
                TempData["ErrorMessage"] =
                    result.Message ?? "پرداخت تأیید نشد یا وضعیت آن نیازمند بررسی است.";
            }

            return RedirectToAction("Index", "Home");
        }
    }
}