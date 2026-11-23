using Entities;
using System;
using System.Collections.Generic;
using WebFramework.Api;

namespace Admin.Models
{
    public class OrderDto : BaseDto<OrderDto, Order>
    {
        public string Code { get; set; }

        public bool IsPaid { get; set; }

        public OrderStatus Status { get; set; }

        public string Shipping { get; set; }

        public int ShippingPrice { get; set; }

        public int RawPrice { get; set; }

        public int Discount { get; set; }

        public int Total { get; set; }
        public int ProductCount { get; set; }//مثلاً اگر مشتری ۲ قاب، ۱ شارژر و ۳ گلس خریده باشه،مقدار ProductCount برابر ۶ می‌شه.

        public Guid UserId { get; set; }

        public List<UserDto> Users { get; set; }

        public string UserFirstName { get; set; }

        public string UserLastName { get; set; }

        public string UserPhoneNumber { get; set; }
    }
}