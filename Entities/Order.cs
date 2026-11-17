using System;
using System.Collections.Generic;
using System.Text;

namespace Entities
{
    public class Order : BaseEntity
    {

        public string Code { get; set; }
        public bool IsPaid { get; set; }//pardakht shodeh
        public string Shipping { get; set; }//ravesh ya etelaat sefaresh
        public int ShippingPrice { get; set; }//hazineye ersal
        public int RawPrice { get; set; }
        public int Discount { get; set; }
        public int Total { get; set; }//Total=rawprice - discount + shippingprice

        public OrderStatus Status { get; set; } = OrderStatus.NotPayed;

        public Guid UserId { get; set; }
        public User User { get; set; }
    }
}
