using System;
using System.Collections.Generic;
using WebFramework.Api;

namespace Client.Models
{
    public class FactorDto 
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string UsersName { get; set; }
        public string PhoneNumber { get; set; }

        public string OrdersCode { get; set; }
        public int OrdersItemTotal { get; set; }
        public string AddressesTitle { get; set; }
        public string ProvinceName { get; set; }
        public string ProvincesName { get; set; }
        public string CitiesName { get; set; }




        public int TotalPrice { get; set; }
        public bool Payed { get; set; }
        public DateTime CreationDate { get; set; }
        public List<OrderItemDto> Orderitems { get; set; }

        public List<OrderDto> orders { get; set; }
    }
}
