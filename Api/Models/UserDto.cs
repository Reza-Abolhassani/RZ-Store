using Entities;

using WebFramework.Api;

namespace API.Controllers.v1.Models
{
    public class UserDto : BaseDto<UserDto, User>
    {
        public string UserName { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }
    public class UpdateUserDto : BaseDto<UpdateUserDto, User>
    {
        public bool PhoneNumberConfirmed { get; set; }
        public string UserName { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string Code { get; set; }



    }

}
