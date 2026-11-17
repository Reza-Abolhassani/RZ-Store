using Entities;
using Entities.Constants;
using Microsoft.AspNetCore.Http;
using System;

using WebFramework.Api;

namespace Admin.Models
{
    public class UserDto : BaseDto<UserDto, User>
    {
        public string UserName { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public SystemRoles Role { get; set; }
        public string Password { get; set; }
        public string ImageUrl { get; set; }
        public IFormFile ImageFile { get; set; }

        public DateTimeOffset? LastLoginDate { get; set; }
        public DateTime RegisterDate { get; set; }
        public bool IsActive { get; set; }

    }


}
