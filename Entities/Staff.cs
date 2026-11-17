using System;
using System.Collections.Generic;
using System.Text;

namespace Entities
{
    public class Staff : BaseEntity
    {
       public string Name { get; set; }
        public int Age { get; set; }
        public string PhoneNumber { get; set; }

        public string Email { get; set; }
    }
}
