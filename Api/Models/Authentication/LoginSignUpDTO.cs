using Entities;

using System;
using System.ComponentModel.DataAnnotations;

using WebFramework.Api;

namespace API.Models.Authentication
{
    public class LoginSignUpDTO : BaseDto<LoginSignUpDTO, TempUser>
    {
        [Required(ErrorMessage = "نام کاربری وارد نشده!")]
        [MaxLength(11, ErrorMessage = "نام کاربری نمیتواند بیشتر از 11 کارکتر باشد!")]
        public string UserName { get; set; }
        [MaxLength(6, ErrorMessage = "کد یکبار مصرف نمیتواند بیشتر از 6 رقم باشد!")]
        public string OTP { get; set; }

        // This property indicates whether the user already exists in the system.
        public bool IsExist { get; set; }

        public DateTime SentAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime UsedAt { get; set; }
    }


    public class LoginPasswordDto
    {
        public string UserName { get; set; } 
        public string Password { get; set; } 
    }
}
