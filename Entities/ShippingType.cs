using System;
using System.Collections.Generic;
using System.Text;

namespace Entities
{
    public class ShippingType : BaseEntity
    {
        public string Name { get; set; }
        public long Price { get; set; }
    }
}
