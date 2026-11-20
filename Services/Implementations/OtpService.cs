using Common;
using Common.Security;
using Common.Sms;
using Data.Repositories;
using Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Services.Models;



using System;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace Services
{
    public class OtpService : IOtpService
    {
        // 1. فیلدهای موردنیاز سرویس
        private readonly IRepository<LoginOtp> otpRepository;
        private readonly ISmsSender smsSender;
        private readonly SiteSettings siteSettings;

        // 2. سازنده کلاس
        public OtpService(
            IRepository<LoginOtp> otpRepository,
            ISmsSender smsSender,
            IOptionsSnapshot<SiteSettings> options)
        {
            this.otpRepository = otpRepository;
            this.smsSender = smsSender;
            siteSettings = options.Value;
        }

        // تولید کد شش رقمی OTP
        private string GenerateOtpCode()
        {
            var code = RandomNumberGenerator.GetInt32(0, 1000000);

            return code.ToString("D6");
        }

        // هش کردن کد OTP
        private string HashOtpCode(string code)
        {
            return OtpHasher.Hash(code, siteSettings.OtpHashKey);
        }

        // اعتبارسنجی شماره موبایل ایران
        private bool IsValidPhoneNumber(string phoneNumber)
        {
            return !string.IsNullOrWhiteSpace(phoneNumber)
                && System.Text.RegularExpressions.Regex.IsMatch(
                    phoneNumber,
                    @"^09\d{9}$");
        }

        // بررسی محدودیت تعداد درخواست OTP
        private async Task<bool> CanSendOtpAsync(
            string phoneNumber,
            CancellationToken cancellationToken)
        {
            var tenMinutesAgo = DateTime.UtcNow.AddMinutes(-10);

            var requestCount = await otpRepository.AllTableNoTracking
                .CountAsync(
                    x => x.PhoneNumber == phoneNumber
                         && x.CreationDateTime >= tenMinutesAgo,
                    cancellationToken);

            return requestCount < 3;
        }

        // بررسی فاصله زمانی ارسال مجدد OTP
        private async Task<bool> CanResendOtpAsync(
            string phoneNumber,
            CancellationToken cancellationToken)
        {
            var oneMinuteAgo = DateTime.UtcNow.AddSeconds(-60);

            var lastRequest = await otpRepository.AllTableNoTracking
                .Where(x => x.PhoneNumber == phoneNumber)
                .OrderByDescending(x => x.CreationDateTime)
                .FirstOrDefaultAsync(cancellationToken);

            return lastRequest == null
                || lastRequest.CreationDateTime <= oneMinuteAgo;
        }

        // ساخت نتیجه عملیات OTP
        private OtpResult CreateResult(bool succeeded, string message)
        {
            return new OtpResult
            {
                Succeeded = succeeded,
                Message = message
            };
        }

        // 3. متد ارسال کد OTP
        public async Task<OtpResult> SendOtpAsync(
    string phoneNumber,
    string requestIp,
    CancellationToken cancellationToken = default)
        {
            // 1. بررسی معتبر بودن شماره موبایل
            if (!IsValidPhoneNumber(phoneNumber))
            {
                return CreateResult(false, "شماره موبایل معتبر نیست.");
            }

            // 2. بررسی محدودیت فاصله ارسال مجدد
            if (!await CanResendOtpAsync(phoneNumber, cancellationToken))
            {
                return CreateResult(false, "لطفاً ۶۰ ثانیه صبر کنید.");
            }

            // 3. بررسی حداکثر تعداد درخواست
            if (!await CanSendOtpAsync(phoneNumber, cancellationToken))
            {
                return CreateResult(false, "تعداد درخواست‌های شما بیش از حد مجاز است.");
            }

            // غیرفعال کردن کدهای قبلی این شماره موبایل
            var previousOtps = await otpRepository.Table
                .Where(x => x.PhoneNumber == phoneNumber && !x.IsUsed)
                .ToListAsync(cancellationToken);

            foreach (var previousOtp in previousOtps)
            {
                previousOtp.IsUsed = true;

                await otpRepository.UpdateAsync(
                    previousOtp,
                    cancellationToken,
                    saveNow: false);
            }

            var otpCode = GenerateOtpCode();

            // 5. هش کردن کد
            var codeHash = HashOtpCode(otpCode);

            // 6. ساخت رکورد OTP
            var otp = new LoginOtp
            {
                PhoneNumber = phoneNumber,
                CodeHash = codeHash,
                ExpiresAtUtc = DateTime.UtcNow.AddMinutes(2),
                AttemptCount = 0,
                IsUsed = false,
                RequestIp = requestIp
            };

            // 7. ذخیره در دیتابیس
            await otpRepository.AddAsync(otp, cancellationToken);

            // 8. نتیجه موقت
            return CreateResult(true, "کد OTP با موفقیت ایجاد و ذخیره شد.");
        }

        // 4. متد تأیید کد OTP
        public async Task<OtpResult> VerifyOtpAsync(
    string phoneNumber,
    string code,
    CancellationToken cancellationToken = default)
        {
            // 1. اعتبارسنجی شماره موبایل
            if (!IsValidPhoneNumber(phoneNumber))
            {
                return CreateResult(false, "شماره موبایل معتبر نیست.");
            }

            // 2. اعتبارسنجی کد واردشده
            if (string.IsNullOrWhiteSpace(code)
                || !System.Text.RegularExpressions.Regex.IsMatch(code, @"^\d{6}$"))
            {
                return CreateResult(false, "کد تأیید باید ۶ رقم باشد.");
            }

            // 3. پیدا کردن آخرین کد فعال
            var otp = await otpRepository.Table
                .Where(x => x.PhoneNumber == phoneNumber && !x.IsUsed)
                .OrderByDescending(x => x.CreationDateTime)
                .FirstOrDefaultAsync(cancellationToken);

            if (otp == null)
            {
                return CreateResult(false, "کد تأیید معتبر نیست.");
            }

            // 4. بررسی انقضای کد
            if (otp.ExpiresAtUtc <= DateTime.UtcNow)
            {
                otp.IsUsed = true;

                await otpRepository.UpdateAsync(otp, cancellationToken);

                return CreateResult(false, "کد تأیید منقضی شده است.");
            }

            // 5. بررسی حداکثر تعداد تلاش
            if (otp.AttemptCount >= 5)
            {
                otp.IsUsed = true;

                await otpRepository.UpdateAsync(otp, cancellationToken);

                return CreateResult(false, "تعداد تلاش‌های شما بیش از حد مجاز است.");
            }

            // 6. هش کردن کد واردشده
            var enteredCodeHash = HashOtpCode(code);

            // 7. مقایسه هش‌ها
            if (!CryptographicOperations.FixedTimeEquals(
                Convert.FromBase64String(otp.CodeHash),
                Convert.FromBase64String(enteredCodeHash)))
            {
                // افزایش تعداد تلاش فقط در صورت اشتباه بودن کد
                otp.AttemptCount++;

                if (otp.AttemptCount >= 5)
                {
                    otp.IsUsed = true;
                }

                await otpRepository.UpdateAsync(otp, cancellationToken);

                return CreateResult(false, "کد تأیید اشتباه است.");
            }

            // 8. مصرف کد صحیح (کد فقط یک بار قابل استفاده است)
            otp.IsUsed = true;

            await otpRepository.UpdateAsync(otp, cancellationToken);

            return CreateResult(true, "کد تأیید با موفقیت انجام شد.");
        }
    }
}