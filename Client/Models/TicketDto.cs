using Entities;
using System;
using System.Collections.Generic;
using WebFramework.Api;

namespace Client.Models
{
    public class TicketDto : BaseDto<TicketDto, Ticket>
    {
        public Guid UserId { get; set; }
        public string UsersName { get; set; }
        public List<User> Users { get; set; }
        public string Subject { get; set; }
        public string Messege { get; set; }
        public string Email { get; set; }
        public string Status { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime LastUpdated { get; set; }
        public string Replies { get; set; }
        public TicketDto()
        {
            Users = new List<User>();
        }
    }
}
