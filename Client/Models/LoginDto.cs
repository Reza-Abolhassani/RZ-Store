using System.ComponentModel.DataAnnotations;

namespace Client.Models
{
    public class LoginDto
    {
        [Required(ErrorMessage = "شماره موبایل را وارد کنید.")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "رمز عبور را وارد کنید.")]
        public string Password { get; set; }

        public bool RememberMe { get; set; }
    }
}