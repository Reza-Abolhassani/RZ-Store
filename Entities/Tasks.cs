using System;
using System.Collections.Generic;
using System.Text;

namespace Entities
{
    public class Tasks : BaseEntity
    {
        public string StaffName { get; set; }

        public Guid StaffId { get; set; }
        public Staff Staff { get; set; }
        public DateTime CreationDate { get; set; }
       

    }
}
