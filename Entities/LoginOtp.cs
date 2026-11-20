using System;

namespace Entities
{
    public class LoginOtp : BaseEntity
    {
        // شماره موبایلی که درخواست کد ورود داده است
        public string PhoneNumber { get; set; }

        // هش کد تأیید؛ خود کد را ذخیره نمی‌کنیم
        public string CodeHash { get; set; }

        // زمان انقضای کد
        public DateTime ExpiresAtUtc { get; set; }

        // تعداد دفعات ورود کد اشتباه
        public int AttemptCount { get; set; }

        // آیا کد قبلاً استفاده شده است؟
        public bool IsUsed { get; set; }

        // IP درخواست‌کننده برای کنترل درخواست‌های مشکوک
        public string RequestIp { get; set; }
    }
}