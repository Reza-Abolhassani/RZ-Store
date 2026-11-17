using Entities.Constants;

using System;

namespace Entities.Support
{
    public class SupportTicket : BaseEntity
    {
        public User User { get; set; }
        public Guid UserId { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public bool Urgent { get; set; }
        public bool Read { get; set; }
        public TicketStatus Status { get; set; }
    }
}
