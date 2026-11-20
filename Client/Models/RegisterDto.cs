using System.ComponentModel.DataAnnotations;

namespace Client.Models
{
    public class RegisterDto
    {
        [Required(ErrorMessage = "نام را وارد کنید.")]
        [StringLength(50, ErrorMessage = "نام نمی‌تواند بیشتر از ۵۰ کاراکتر باشد.")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "نام خانوادگی را وارد کنید.")]
        [StringLength(50, ErrorMessage = "نام خانوادگی نمی‌تواند بیشتر از ۵۰ کاراکتر باشد.")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "شماره موبایل را وارد کنید.")]
        [RegularExpression(@"^09\d{9}$", ErrorMessage = "شماره موبایل معتبر نیست.")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "رمز عبور را وارد کنید.")]
        [StringLength(100, MinimumLength = 8,
            ErrorMessage = "رمز عبور باید حداقل ۸ کاراکتر باشد.")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required(ErrorMessage = "تکرار رمز عبور را وارد کنید.")]
        [Compare("Password", ErrorMessage = "رمز عبور و تکرار آن یکسان نیستند.")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "لطفاً ایمیل خود را وارد کنید.")]
        [EmailAddress(ErrorMessage = "ایمیل وارد شده معتبر نیست.")]
        public string Email { get; set; }
    }
}