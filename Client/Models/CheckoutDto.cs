using Client.Models;
using System;
using System.Collections.Generic;

namespace Client.Dto
{
    public class CheckoutDto
    {
        public ShopCartDto ShopCart { get; set; }//اطلاعات محصولات و مبالغ سبد خرید.

        public List<ShippingTypeDto> ShippingTypes { get; set; }//فهرست روش‌های ارسال.

        public Guid ShippingTypeId { get; set; }//شناسه روش ارسالی

        public List<UserAddressDto> UserAddresses { get; set; }

        public Guid UserAddressId { get; set; }


    }
}