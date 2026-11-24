using Entities;
using System;
using WebFramework.Api;

namespace Admin.Models
{
    public class UserAddressDto : BaseDto<UserAddressDto, UserAddress>
    {
        public Guid UserId { get; set; }

        public string UserName { get; set; }

        public string ReceiverName { get; set; }

        public string ReceiverPhone { get; set; }

        public string Province { get; set; }

        public string City { get; set; }

        public string Address { get; set; }

        public string PostalCode { get; set; }

        public bool IsDefault { get; set; }
    }
}