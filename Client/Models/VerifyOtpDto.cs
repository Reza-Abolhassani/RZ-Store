using System.ComponentModel.DataAnnotations;

namespace Client.Models
{
    public class VerifyOtpDto
    {
        [Required(ErrorMessage = "شماره موبایل را وارد کنید.")]
        [RegularExpression(@"^09\d{9}$", ErrorMessage = "شماره موبایل معتبر نیست.")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "کد تأیید را وارد کنید.")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "کد تأیید باید ۶ رقم باشد.")]
        public string Code { get; set; }

        [StringLength(50, ErrorMessage = "نام نمی‌تواند بیشتر از ۵۰ کاراکتر باشد.")]
        public string FirstName { get; set; }

        [StringLength(50, ErrorMessage = "نام خانوادگی نمی‌تواند بیشتر از ۵۰ کاراکتر باشد.")]
        public string LastName { get; set; }
    }
}