using System;
using System.Collections.Generic;
using System.Text;

namespace Entities
{
    public class Ticket : BaseEntity
    {
        public Guid UserId { get; set; }
        public User User { get; set; }
        public string Messege { get; set; }
        public string Email { get; set; }
        public string Status { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime LastUpdated { get; set; }
        public string Replies { get; set; }
    }
}
