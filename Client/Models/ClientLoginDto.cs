using System.ComponentModel.DataAnnotations;

namespace Client.Dto
{
    public class ClientLoginDto
    {
        [Required(ErrorMessage = "شماره موبایل را وارد کنید")]
        public string PhoneNumber { get; set; }

        public string Otp { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }
    }
}