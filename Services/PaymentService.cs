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

using Microsoft.Data.SqlClient;

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

            if (!Uri.TryCreate(
                    callbackUrl,
                    UriKind.Absolute,
                    out var callbackUri) ||
                (callbackUri.Scheme != Uri.UriSchemeHttp &&
                 callbackUri.Scheme != Uri.UriSchemeHttps))
            {
                throw new ArgumentException(
                    "آدرس بازگشت پرداخت معتبر نیست.");
            }

            var user = await _userManager.FindByIdAsync(userId.ToString());

            if (user == null)
            {
                return new OrderPaymentResult
                {
                    Message = "کاربر موردنظر یافت نشد."
                };
            }

            Payment? payment = null;
            PaymentAttempt? attempt = null;

            const int maxAttempts = 3;

            for (int retry = 1; retry <= maxAttempts; retry++)
            {
                try
                {
                    await using var transaction =
                        await _dbContext.Database.BeginTransactionAsync(
                            System.Data.IsolationLevel.Serializable);

                    var order = await _dbContext.Set<Order>()
                        .FirstOrDefaultAsync(x =>
                            x.Id == orderId &&
                            x.UserId == userId);

                    if (order == null)
                    {
                        return new OrderPaymentResult
                        {
                            Message = "سفارش موردنظر یافت نشد."
                        };
                    }

                    if (order.IsPaid ||
                        order.Status != OrderStatus.NotPayed)
                    {
                        return new OrderPaymentResult
                        {
                            Message = "این سفارش در وضعیت قابل پرداخت نیست."
                        };
                    }

                    if (order.Total <= 0)
                    {
                        return new OrderPaymentResult
                        {
                            Message = "مبلغ سفارش معتبر نیست."
                        };
                    }

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

                        var activeAttempt = payment.PaymentAttempts
                            .Where(x =>
                                x.Status == PaymentAttemptStatus.Pending ||
                                x.Status == PaymentAttemptStatus.Verifying)
                            .OrderByDescending(x => x.StartedAt)
                            .FirstOrDefault();

                        if (activeAttempt != null)
                        {
                            if (activeAttempt.Status == PaymentAttemptStatus.Verifying)
                            {
                                return new OrderPaymentResult
                                {
                                    PaymentId = payment.Id,
                                    Message = "تأیید پرداخت در حال انجام است. لطفاً منتظر تعیین نتیجه باشید."
                                };
                            }

                            if (!string.IsNullOrWhiteSpace(activeAttempt.PaymentUrl) &&
                                !string.IsNullOrWhiteSpace(activeAttempt.GatewayRequestId))
                            {
                                return new OrderPaymentResult
                                {
                                    IsSuccess = true,
                                    PaymentId = payment.Id,
                                    PaymentUrl = activeAttempt.PaymentUrl,
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

                    break;
                }
                catch (Exception ex) when (
                    ex.GetBaseException() is SqlException sqlException &&
                    (sqlException.Number == 1205 ||
                     sqlException.Number == 2601 ||
                     sqlException.Number == 2627))
                {
                    // پاک کردن موجودیت‌های رهگیری‌شده پس از شکست تراکنش
                    _dbContext.ChangeTracker.Clear();

                    if (retry == maxAttempts)
                        throw;

                    // تأخیر کوتاه قبل از تلاش مجدد
                    await Task.Delay(100 * retry);
                }
            }

            if (payment == null || attempt == null)
            {
                throw new InvalidOperationException(
                    "ایجاد اطلاعات پرداخت با موفقیت انجام نشد.");
            }

            // ارسال درخواست به درگاه خارج از تراکنش دیتابیس
            GatewayPaymentRequestResult gatewayResult;

            try
            {
                gatewayResult =
                    await _paymentGateway.RequestPaymentAsync(
                        new GatewayPaymentRequest
                        {
                            Amount = attempt.Amount,
                            CallbackUrl = callbackUrl,
                            Description = payment.Description ??
                                "پرداخت سفارش",
                            Mobile = user.PhoneNumber,
                            Email = user.Email
                        });
            }
            catch (Exception ex)
            {
                attempt.ErrorCode = ex.GetType().Name;
                attempt.ErrorMessage =
                    "ارتباط با درگاه با خطا مواجه شد. " +
                    "وضعیت درخواست پرداخت نامشخص است.";

                // نتیجه نامشخص است؛ وضعیت Pending حفظ می‌شود.
                await _dbContext.SaveChangesAsync();

                return new OrderPaymentResult
                {
                    PaymentId = payment.Id,
                    Message = attempt.ErrorMessage
                };
            }

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
        public async Task<OrderPaymentResult> VerifyOrderPaymentAsync(string authority,string status)
        {
            if (string.IsNullOrWhiteSpace(authority))
            {
                return new OrderPaymentResult
                {
                    Message = "شناسه پرداخت معتبر نیست."
                };
            }

            var attempt = await _dbContext.Set<PaymentAttempt>()
                .Include(x => x.Payment)
                    .ThenInclude(x => x.Order)
                .Include(x => x.Payment)
                    .ThenInclude(x => x.PaymentAttempts)
                .FirstOrDefaultAsync(x =>
                    x.GatewayRequestId == authority &&
                    x.GatewayName == GatewayName);

            if (attempt == null)
            {
                return new OrderPaymentResult
                {
                    Message = "درخواست پرداخت موردنظر یافت نشد."
                };
            }

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

            if (attempt.Status == PaymentAttemptStatus.Verifying)
            {
                return new OrderPaymentResult
                {
                    PaymentId = payment.Id,
                    Message =
                        "تأیید این پرداخت در حال انجام است. لطفاً منتظر تعیین نتیجه باشید."
                };
            }

            if (attempt.Status != PaymentAttemptStatus.Pending)
            {
                return new OrderPaymentResult
                {
                    PaymentId = payment.Id,
                    Message = "این درخواست پرداخت قبلاً تعیین تکلیف شده است."
                };
            }

            // اطمینان از اینکه پرداخت متعلق به سفارش است
            // و فقط از طریق درگاه بانکی انجام شده است.
            if (payment.Type != PaymentType.OrderPayment ||
                payment.Method != PaymentMethod.Gateway ||
                !payment.OrderId.HasValue ||
                order == null)
            {
                return new OrderPaymentResult
                {
                    PaymentId = payment.Id,
                    Message =
                        "اطلاعات پرداخت با ساختار پرداخت سفارش مطابقت ندارد."
                };
            }

            // کنترل تطابق مبالغ ثبت‌شده
            if (attempt.Amount <= 0 ||
                payment.Amount != attempt.Amount ||
                payment.GatewayAmount != attempt.Amount ||
                payment.WalletAmount != 0 ||
                order.Total != attempt.Amount)
            {
                return new OrderPaymentResult
                {
                    PaymentId = payment.Id,
                    Message =
                        "مبالغ ثبت‌شده در پرداخت، سفارش و تلاش پرداخت با یکدیگر مطابقت ندارند."
                };
            }

            // وضعیت بازگشت مرورگر به‌تنهایی معتبر نیست.
            // برای وضعیت غیر OK هیچ تغییری در دیتابیس انجام نمی‌دهیم.
            if (!string.Equals(
                    status,
                    "OK",
                    StringComparison.OrdinalIgnoreCase))
            {
                return new OrderPaymentResult
                {
                    PaymentId = payment.Id,
                    Message =
                        "درگاه پرداخت نتیجه موفقی اعلام نکرده است. " +
                        "وضعیت پرداخت برای بررسی قطعی در حالت انتظار باقی می‌ماند."
                };
            }

            // مرحله اول: تصاحب اتمیک تلاش پرداخت
            // فقط یکی از درخواست‌های هم‌زمان می‌تواند Pending را به Verifying تغییر دهد.
            await using (var claimTransaction =
                await _dbContext.Database.BeginTransactionAsync())
            {
                try
                {
                    attempt.Status = PaymentAttemptStatus.Verifying;
                    attempt.VerificationStartedAt = DateTime.UtcNow;

                    await _dbContext.SaveChangesAsync();
                    await claimTransaction.CommitAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    await claimTransaction.RollbackAsync();

                    _dbContext.ChangeTracker.Clear();

                    var latestAttempt = await _dbContext.Set<PaymentAttempt>()
                        .AsNoTracking()
                        .Include(x => x.Payment)
                        .FirstOrDefaultAsync(x => x.Id == attempt.Id);

                    if (latestAttempt == null)
                    {
                        return new OrderPaymentResult
                        {
                            PaymentId = payment.Id,
                            Message = "وضعیت درخواست پرداخت قابل بازیابی نیست."
                        };
                    }

                    if (latestAttempt.Status == PaymentAttemptStatus.Success ||
                        latestAttempt.Payment.Status == PaymentStatus.Success)
                    {
                        return new OrderPaymentResult
                        {
                            IsSuccess = true,
                            PaymentId = latestAttempt.PaymentId,
                            Message = "این پرداخت قبلاً تأیید شده است."
                        };
                    }

                    if (latestAttempt.Status == PaymentAttemptStatus.Verifying)
                    {
                        return new OrderPaymentResult
                        {
                            PaymentId = latestAttempt.PaymentId,
                            Message =
                                "درخواست دیگری در حال تأیید این پرداخت است. لطفاً منتظر بمانید."
                        };
                    }

                    return new OrderPaymentResult
                    {
                        PaymentId = latestAttempt.PaymentId,
                        Message =
                            "وضعیت پرداخت هم‌زمان تغییر کرده است. لطفاً وضعیت پرداخت را مجدداً بررسی کنید."
                    };
                }
            }

            // مرحله دوم: تأیید واقعی پرداخت از سرور زرین‌پال
            // در این مرحله تراکنش دیتابیس باز نیست.
            GatewayPaymentVerificationResult verificationResult;

            try
            {
                verificationResult =
                    await _paymentGateway.VerifyPaymentAsync(
                        authority,
                        attempt.Amount);
            }
            catch (Exception ex)
            {
                attempt.ErrorCode = ex.GetType().Name;
                attempt.ErrorMessage =
                    "ارتباط با درگاه هنگام تأیید پرداخت با خطا مواجه شد. " +
                    "نتیجه پرداخت نامشخص است.";

                // وضعیت Verifying حفظ می‌شود.
                await _dbContext.SaveChangesAsync();

                return new OrderPaymentResult
                {
                    PaymentId = payment.Id,
                    Message = attempt.ErrorMessage
                };
            }

            if (verificationResult.IsIndeterminate)
            {
                attempt.ErrorCode = verificationResult.ErrorCode;
                attempt.ErrorMessage =
                    verificationResult.ErrorMessage ??
                    "نتیجه تأیید پرداخت از طرف درگاه نامشخص است.";

                // وضعیت Verifying حفظ می‌شود.
                await _dbContext.SaveChangesAsync();

                return new OrderPaymentResult
                {
                    PaymentId = payment.Id,
                    Message = attempt.ErrorMessage
                };
            }

            // نتیجه قطعی ناموفق
            if (!verificationResult.IsSuccess)
            {
                await using var failTransaction =
                    await _dbContext.Database.BeginTransactionAsync();

                try
                {
                    attempt.Status = PaymentAttemptStatus.Failed;
                    attempt.CompletedAt = DateTime.UtcNow;
                    attempt.ErrorCode = verificationResult.ErrorCode;
                    attempt.ErrorMessage =
                        verificationResult.ErrorMessage ??
                        "تأیید پرداخت توسط درگاه ناموفق بود.";

                    bool hasActiveAttempts =
                        await _dbContext.Set<PaymentAttempt>()
                            .AnyAsync(x =>
                                x.PaymentId == payment.Id &&
                                x.Id != attempt.Id &&
                                (x.Status == PaymentAttemptStatus.Pending ||
                                 x.Status == PaymentAttemptStatus.Verifying));

                    if (!hasActiveAttempts &&
                        payment.Status == PaymentStatus.Pending)
                    {
                        payment.Status = PaymentStatus.Failed;
                    }

                    await _dbContext.SaveChangesAsync();
                    await failTransaction.CommitAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    await failTransaction.RollbackAsync();

                    _dbContext.ChangeTracker.Clear();

                    var latestAttempt = await _dbContext.Set<PaymentAttempt>()
                        .AsNoTracking()
                        .Include(x => x.Payment)
                        .FirstOrDefaultAsync(x => x.Id == attempt.Id);

                    if (latestAttempt?.Status == PaymentAttemptStatus.Success ||
                        latestAttempt?.Payment.Status == PaymentStatus.Success)
                    {
                        return new OrderPaymentResult
                        {
                            IsSuccess = true,
                            PaymentId = payment.Id,
                            Message = "این پرداخت قبلاً با موفقیت ثبت شده است."
                        };
                    }

                    return new OrderPaymentResult
                    {
                        PaymentId = payment.Id,
                        Message =
                            "هنگام ثبت نتیجه پرداخت تداخل هم‌زمان رخ داد. وضعیت پرداخت نیازمند بررسی مجدد است."
                    };
                }

                return new OrderPaymentResult
                {
                    PaymentId = payment.Id,
                    Message = attempt.ErrorMessage
                };
            }

            // پاسخ موفق بدون کد رهگیری معتبر، نتیجه قطعی قابل ثبت نیست.
            if (string.IsNullOrWhiteSpace(verificationResult.TrackingCode))
            {
                attempt.ErrorCode = verificationResult.ErrorCode;
                attempt.ErrorMessage =
                    "درگاه نتیجه موفق اعلام کرده است، اما کد رهگیری معتبر دریافت نشد. وضعیت پرداخت نیازمند بررسی است.";

                await _dbContext.SaveChangesAsync();

                return new OrderPaymentResult
                {
                    PaymentId = payment.Id,
                    Message = attempt.ErrorMessage
                };
            }

            // مرحله سوم: ثبت نتیجه موفق پرداخت به‌صورت اتمیک
            await using (var successTransaction =
                await _dbContext.Database.BeginTransactionAsync())
            {
                try
                {
                    attempt.Status = PaymentAttemptStatus.Success;
                    attempt.TrackingCode = verificationResult.TrackingCode;
                    attempt.CompletedAt = DateTime.UtcNow;
                    attempt.ErrorCode = null;
                    attempt.ErrorMessage = null;

                    payment.Status = PaymentStatus.Success;
                    payment.PaidAt = DateTime.UtcNow;

                    order.IsPaid = true;

                    // وضعیت سفارش‌های لغو یا مرجوع‌شده
                    // را تغییر نمی‌دهیم.
                    if (order.Status == OrderStatus.NotPayed)
                    {
                        order.Status = OrderStatus.Pending;
                    }

                    await _dbContext.SaveChangesAsync();
                    await successTransaction.CommitAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    await successTransaction.RollbackAsync();

                    _dbContext.ChangeTracker.Clear();

                    var latestAttempt = await _dbContext.Set<PaymentAttempt>()
                        .AsNoTracking()
                        .Include(x => x.Payment)
                            .ThenInclude(x => x.Order)
                        .FirstOrDefaultAsync(x => x.Id == attempt.Id);

                    if (latestAttempt?.Status == PaymentAttemptStatus.Success ||
                        latestAttempt?.Payment.Status == PaymentStatus.Success)
                    {
                        return new OrderPaymentResult
                        {
                            IsSuccess = true,
                            PaymentId = payment.Id,
                            Message = "این پرداخت قبلاً با موفقیت ثبت شده است."
                        };
                    }

                    return new OrderPaymentResult
                    {
                        PaymentId = payment.Id,
                        Message =
                            "درگاه پرداخت را موفق اعلام کرده است، اما ثبت نهایی در دیتابیس با تداخل مواجه شد. پرداخت نیازمند بررسی مجدد است."
                    };
                }
            }

            string message =
                order.Status == OrderStatus.Cancelled ||
                order.Status == OrderStatus.Returned
                    ? "پرداخت موفق ثبت شد؛ اما سفارش لغو یا مرجوع شده است و نیاز به بررسی بازپرداخت دارد."
                    : "پرداخت با موفقیت تأیید شد.";

            return new OrderPaymentResult
            {
                IsSuccess = true,
                PaymentId = payment.Id,
                Message = message
            };
        }

        // بررسی موجودی و کسر اتمیک موجودی محصولات سفارش
        // این متد باید داخل تراکنش دیتابیس فراخوانی شود.
        private async Task<bool> TryDeductOrderInventoryAsync(Guid orderId)
        {
            // دریافت اقلام سفارش
            var orderItems = await _dbContext.Set<OrderItem>()
                .AsNoTracking()
                .Where(x => x.OrderId == orderId)
                .ToListAsync();

            // سفارش بدون قلم معتبر نیست
            if (!orderItems.Any())
                return false;

            foreach (var orderItem in orderItems)
            {
                // کنترل تعداد درخواستی
                if (orderItem.Count <= 0)
                    return false;

                // کسر موجودی فقط در صورت کافی بودن موجودی محصول
                // شرط و کاهش موجودی در یک دستور SQL انجام می‌شوند.
                var affectedRows = await _dbContext.Set<Product>()
                    .Where(x =>
                        x.Id == orderItem.ProductId &&
                        x.Count >= orderItem.Count)
                    .ExecuteUpdateAsync(setters =>
                        setters.SetProperty(
                            x => x.Count,
                            x => x.Count - orderItem.Count));

                // اگر محصول موجود نباشد یا موجودی کافی نباشد
                if (affectedRows != 1)
                    return false;
            }

            return true;
        }
    }
}