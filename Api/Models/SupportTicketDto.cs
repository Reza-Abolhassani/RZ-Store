using Entities.Support;

using System;

using WebFramework.Api;

namespace API.Models
{
    public class SupportTicketDto : BaseDto<SupportTicketDto, SupportTicket>
    {
        public string UserFname { get; set; }
        public string UserLname { get; set; }
        public Guid UserId { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public bool Urgent { get; set; }
        public bool Read { get; set; }
    }
}
