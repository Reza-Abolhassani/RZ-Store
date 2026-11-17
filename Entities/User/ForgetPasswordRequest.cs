namespace Entities
{
    public class ForgetPasswordRequest : BaseEntity
    {
        public string OTP { get; set; }
        public string Email { get; set; }
    }
}
