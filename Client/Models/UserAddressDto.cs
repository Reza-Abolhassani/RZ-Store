using System;

namespace Client.Dto
{
    public class UserAddressDto
    {
        public Guid Id { get; set; }

        public string ReceiverName { get; set; }
        public string ReceiverPhone { get; set; }

        public string Province { get; set; }
        public string City { get; set; }
        public string Address { get; set; }
        public string PostalCode { get; set; }

        public bool IsDefault { get; set; }
    }
}