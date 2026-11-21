using System.ComponentModel.DataAnnotations;

namespace Client.Models
{
    public class ForgotPasswordDto
    {
        [Required(ErrorMessage = "شماره موبایل را وارد کنید.")]
        [RegularExpression(
            @"^09\d{9}$",
            ErrorMessage = "شماره موبایل معتبر نیست.")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "کد تأیید را وارد کنید.")]
        [RegularExpression(
            @"^\d{6}$",
            ErrorMessage = "کد تأیید باید ۶ رقم باشد.")]
        public string Code { get; set; }

        [Required(ErrorMessage = "رمز عبور جدید را وارد کنید.")]
        [StringLength(
            100,
            MinimumLength = 8,
            ErrorMessage = "رمز عبور باید حداقل ۸ کاراکتر باشد.")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required(ErrorMessage = "تکرار رمز عبور را وارد کنید.")]
        [Compare(
            "Password",
            ErrorMessage = "رمز عبور و تکرار آن یکسان نیستند.")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; }
    }
}