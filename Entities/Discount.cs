using System;
using System.Collections.Generic;
using System.Text;

namespace Entities
{
    public class Discount : BaseEntity
    {
        public string Code { get; set; }
        public int Percentage { get; set; }
        public long Amount { get; set; }
        public DateTime Startdate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
    }
}
