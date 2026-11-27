using Common;
using Data;
using Entities;
using Entities.Payments;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Services.Interfaces;
using Services.Interfaces.Payments;
using Services.Payments;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Services
{
    public class PaymentService : IPaymentService, IScopedDependency
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IPaymentGateway _paymentGateway;
        private readonly UserManager<User> _userManager;

        private const string GatewayName = "ZarinPal";

        public PaymentService(
            ApplicationDbContext dbContext,
            IPaymentGateway paymentGateway,
            UserManager<User> userManager)
        {
            _dbContext = dbContext;
            _paymentGateway = paymentGateway;
            _userManager = userManager;
        }

        // ایجاد درخواست پرداخت سفارش
        public async Task<OrderPaymentResult> CreateOrderPaymentAsync(
            Guid orderId,
            Guid userId,
            string callbackUrl)
        {
            if (orderId == Guid.Empty)
                throw new ArgumentException("شناسه سفارش معتبر نیست.");

            if (userId == Guid.Empty)
                throw new ArgumentException("شناسه کاربر معتبر نیست.");

            if (!Uri.TryCreate(callbackUrl, UriKind.Absolute, out var callbackUri) ||
                (callbackUri.Scheme != Uri.UriSchemeHttp &&
                 callbackUri.Scheme != Uri.UriSchemeHttps))
            {
                throw new ArgumentException("آدرس بازگشت پرداخت معتبر نیست.");
            }

            var user = await _userManager.FindByIdAsync(userId.ToString());

            if (user == null)
                return new OrderPaymentResult
                {
                    Message = "کاربر موردنظر یافت نشد."
                };

            Payment payment;
            PaymentAttempt attempt;
            

            // بررسی سفارش و پرداخت قبلی
            await using (var transaction =
                await _dbContext.Database.BeginTransactionAsync())
            {
                var order = await _dbContext.Set<Order>()
                    .FirstOrDefaultAsync(x =>
                        x.Id == orderId &&
                        x.UserId == userId);

                if (order == null)
                    return new OrderPaymentResult
                    {
                        Message = "سفارش موردنظر یافت نشد."
                    };

                if (order.IsPaid ||
                    order.Status != OrderStatus.NotPayed)
                {
                    return new OrderPaymentResult
                    {
                        Message = "این سفارش در وضعیت قابل پرداخت نیست."
                    };
                }

                if (order.Total <= 0)
                    return new OrderPaymentResult
                    {
                        Message = "مبلغ سفارش معتبر نیست."
                    };

                payment = await _dbContext.Set<Payment>()
                    .Include(x => x.PaymentAttempts)
                    .FirstOrDefaultAsync(x =>
                        x.OrderId == orderId &&
                        x.UserId == userId &&
                        x.Type == PaymentType.OrderPayment);

                if (payment != null)
                {
                    if (payment.Status == PaymentStatus.Success)
                    {
                        return new OrderPaymentResult
                        {
                            IsSuccess = true,
                            PaymentId = payment.Id,
                            Message = "این سفارش قبلاً پرداخت شده است."
                        };
                    }

                    // جلوگیری از ایجاد درخواست تکراری
                    var pendingAttempt = payment.PaymentAttempts
                        .Where(x => x.Status == PaymentAttemptStatus.Pending)
                        .OrderByDescending(x => x.StartedAt)
                        .FirstOrDefault();

                    if (pendingAttempt != null)
                    {
                        if (!string.IsNullOrWhiteSpace(pendingAttempt.PaymentUrl) &&
                            !string.IsNullOrWhiteSpace(pendingAttempt.GatewayRequestId))
                        {
                            return new OrderPaymentResult
                            {
                                IsSuccess = true,
                                PaymentId = payment.Id,
                                PaymentUrl = pendingAttempt.PaymentUrl,
                                Message = "درخواست پرداخت قبلی آماده است."
                            };
                        }

                        return new OrderPaymentResult
                        {
                            PaymentId = payment.Id,
                            Message = "درخواست پرداخت قبلی هنوز تعیین تکلیف نشده است."
                        };
                    }

                    payment.Amount = order.Total;
                    payment.WalletAmount = 0;
                    payment.GatewayAmount = order.Total;
                    payment.Method = PaymentMethod.Gateway;
                    payment.Status = PaymentStatus.Pending;
                    payment.Description = "پرداخت سفارش " + order.Code;
                }
                else
                {
                    payment = new Payment
                    {
                        UserId = userId,
                        OrderId = orderId,
                        Amount = order.Total,
                        WalletAmount = 0,
                        GatewayAmount = order.Total,
                        Type = PaymentType.OrderPayment,
                        Method = PaymentMethod.Gateway,
                        Status = PaymentStatus.Pending,
                        Description = "پرداخت سفارش " + order.Code,
                        Order = order
                    };

                    _dbContext.Set<Payment>().Add(payment);
                    
                }

                attempt = new PaymentAttempt
                {
                    Payment = payment,
                    GatewayName = GatewayName,
                    Amount = order.Total,
                    Status = PaymentAttemptStatus.Pending,
                    StartedAt = DateTime.UtcNow
                };

                _dbContext.Set<PaymentAttempt>().Add(attempt);

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();
            }

            // ارسال درخواست به درگاه خارج از تراکنش دیتابیس
            var gatewayResult = await _paymentGateway.RequestPaymentAsync(
                new GatewayPaymentRequest
                {
                    Amount = attempt.Amount,
                    CallbackUrl = callbackUrl,
                    Description = payment.Description ?? "پرداخت سفارش",
                    Mobile = user.PhoneNumber,
                    Email = user.Email
                });
            if (gatewayResult.IsIndeterminate)
            {
                attempt.ErrorCode = gatewayResult.ErrorCode;
                attempt.ErrorMessage = gatewayResult.ErrorMessage ??
                    "نتیجه درخواست پرداخت در درگاه نامشخص است.";

                // تلاش پرداخت و پرداخت اصلی همچنان Pending باقی می‌مانند.
                await _dbContext.SaveChangesAsync();

                return new OrderPaymentResult
                {
                    PaymentId = payment.Id,
                    Message = attempt.ErrorMessage
                };
            }

            if (!gatewayResult.IsSuccess ||
                string.IsNullOrWhiteSpace(gatewayResult.Authority) ||
                string.IsNullOrWhiteSpace(gatewayResult.PaymentUrl))
            {
                attempt.Status = PaymentAttemptStatus.Failed;
                attempt.CompletedAt = DateTime.UtcNow;
                attempt.ErrorCode = gatewayResult.ErrorCode;
                attempt.ErrorMessage = gatewayResult.ErrorMessage ??
                    "ایجاد درخواست پرداخت در درگاه ناموفق بود.";

                payment.Status = PaymentStatus.Failed;

                await _dbContext.SaveChangesAsync();

                return new OrderPaymentResult
                {
                    PaymentId = payment.Id,
                    Message = attempt.ErrorMessage
                };
            }

            // ذخیره اطلاعات دریافتی از درگاه
            attempt.GatewayRequestId = gatewayResult.Authority;
            attempt.PaymentUrl = gatewayResult.PaymentUrl;

            await _dbContext.SaveChangesAsync();

            return new OrderPaymentResult
            {
                IsSuccess = true,
                PaymentId = payment.Id,
                PaymentUrl = gatewayResult.PaymentUrl,
                Message = "درخواست پرداخت با موفقیت ایجاد شد."
            };
        }

        // بررسی و ثبت نتیجه پرداخت
        public async Task<OrderPaymentResult> VerifyOrderPaymentAsync(
            string authority,
            string status)
        {
            if (string.IsNullOrWhiteSpace(authority))
                return new OrderPaymentResult
                {
                    Message = "شناسه پرداخت معتبر نیست."
                };

            var attempt = await _dbContext.Set<PaymentAttempt>()
                .Include(x => x.Payment)
                    .ThenInclude(x => x.Order)
                .Include(x => x.Payment)
                    .ThenInclude(x => x.PaymentAttempts)
                .FirstOrDefaultAsync(x =>
                    x.GatewayRequestId == authority &&
                    x.GatewayName == GatewayName);

            if (attempt == null)
                return new OrderPaymentResult
                {
                    Message = "درخواست پرداخت موردنظر یافت نشد."
                };

            var payment = attempt.Payment;
            var order = payment.Order;

            if (attempt.Status == PaymentAttemptStatus.Success ||
                payment.Status == PaymentStatus.Success)
            {
                return new OrderPaymentResult
                {
                    IsSuccess = true,
                    PaymentId = payment.Id,
                    Message = "این پرداخت قبلاً تأیید شده است."
                };
            }

            // وضعیت بازگشت مرورگر به‌تنهایی معتبر نیست.
            // برای وضعیت غیر OK هیچ تغییری در دیتابیس انجام نمی‌دهیم.
            if (!string.Equals(status, "OK", StringComparison.OrdinalIgnoreCase))
            {
                return new OrderPaymentResult
                {
                    PaymentId = payment.Id,
                    Message = "درگاه پرداخت نتیجه موفقی اعلام نکرده است. وضعیت پرداخت برای بررسی قطعی در حالت انتظار باقی می‌ماند."
                };
            }

            if (attempt.Amount <= 0)
                return new OrderPaymentResult
                {
                    PaymentId = payment.Id,
                    Message = "مبلغ تلاش پرداخت معتبر نیست."
                };

            // تأیید واقعی پرداخت از سرور زرین‌پال
            var verificationResult =
                await _paymentGateway.VerifyPaymentAsync(
                    authority,
                    attempt.Amount);

            if (verificationResult.IsIndeterminate)
            {
                attempt.ErrorCode = verificationResult.ErrorCode;
                attempt.ErrorMessage = verificationResult.ErrorMessage ??
                    "نتیجه تأیید پرداخت از طرف درگاه نامشخص است.";

                // وضعیت تلاش و پرداخت اصلی همچنان Pending باقی می‌ماند.
                await _dbContext.SaveChangesAsync();

                return new OrderPaymentResult
                {
                    PaymentId = payment.Id,
                    Message = attempt.ErrorMessage
                };
            }

            if (!verificationResult.IsSuccess ||
                string.IsNullOrWhiteSpace(verificationResult.TrackingCode))
            {
                await using var failTransaction =
                    await _dbContext.Database.BeginTransactionAsync();

                attempt.Status = PaymentAttemptStatus.Failed;
                attempt.CompletedAt = DateTime.UtcNow;
                attempt.ErrorCode = verificationResult.ErrorCode;
                attempt.ErrorMessage = verificationResult.ErrorMessage ??
                    "تأیید پرداخت توسط درگاه ناموفق بود.";

                bool hasPendingAttempts = payment.PaymentAttempts
                    .Any(x =>
                        x.Id != attempt.Id &&
                        x.Status == PaymentAttemptStatus.Pending);

                if (!hasPendingAttempts &&
                    payment.Status == PaymentStatus.Pending)
                {
                    payment.Status = PaymentStatus.Failed;
                }

                await _dbContext.SaveChangesAsync();
                await failTransaction.CommitAsync();

                return new OrderPaymentResult
                {
                    PaymentId = payment.Id,
                    Message = attempt.ErrorMessage
                };
            }

            // ثبت نتیجه موفق پرداخت به‌صورت اتمیک
            await using (var successTransaction =
                await _dbContext.Database.BeginTransactionAsync())
            {
                attempt.Status = PaymentAttemptStatus.Success;
                attempt.TrackingCode = verificationResult.TrackingCode;
                attempt.CompletedAt = DateTime.UtcNow;

                payment.Status = PaymentStatus.Success;
                payment.PaidAt = DateTime.UtcNow;

                if (order != null)
                {
                    order.IsPaid = true;

                    // وضعیت سفارش‌های ارسال‌شده و تحویل‌شده
                    // یا لغو و مرجوع‌شده را تغییر نمی‌دهیم.
                    if (order.Status == OrderStatus.NotPayed)
                    {
                        order.Status = OrderStatus.Pending;
                    }
                }

                await _dbContext.SaveChangesAsync();
                await successTransaction.CommitAsync();
            }

            string message = order != null &&
                (order.Status == OrderStatus.Cancelled ||
                 order.Status == OrderStatus.Returned)
                    ? "پرداخت موفق ثبت شد؛ اما سفارش لغو یا مرجوع شده است و نیاز به بررسی بازپرداخت دارد."
                    : "پرداخت با موفقیت تأیید شد.";

            return new OrderPaymentResult
            {
                IsSuccess = true,
                PaymentId = payment.Id,
                Message = message
            };
        }
    }
}