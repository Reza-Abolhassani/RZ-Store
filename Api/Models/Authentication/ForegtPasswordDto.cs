namespace API.Controllers.v1.Models.Authentication
{
    public class ForgetPasswordDto
    {
        public string Email { get; set; }
        public string Otp { get; set; }
        public string Password { get; set; }
        public string PasswordConfirm { get; set; }

    }
}
