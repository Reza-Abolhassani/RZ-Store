using System;

namespace Entities.Notification
{
    public class Notification : BaseEntity
    {
        public string Title { get; set; }
        public string Message { get; set; }
        public User User { get; set; }
        public Guid UserId { get; set; }
        public DateTime DueDate { get; set; }
    }
}
