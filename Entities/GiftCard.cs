using System;
using System.Collections.Generic;
using System.Text;

namespace Entities
{
    public class GiftCard : BaseEntity
    {
        public string Code { get; set; }
        public int Amount { get; set; }
        public bool Isused { get; set; }
        public DateTime ExpireDate { get; set; }
    }
}
