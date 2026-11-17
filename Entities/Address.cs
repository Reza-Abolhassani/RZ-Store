using System;
using System.Collections.Generic;
using System.Text;

namespace Entities
{
    public class Address : BaseEntity
    {
        public Guid CityId { get; set; }
        public City City { get; set; }
        public Guid UserId { get; set; }
        public User User { get; set; }
        public string FullAddress { get; set; }
        public String PostalCode { get; set; }
        public String Title { get; set; }
    }
}
