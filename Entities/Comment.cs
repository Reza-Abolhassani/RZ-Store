using System;
using Entities.Constants;
namespace Entities
{
    public class Comment : BaseEntity
    {
        public string AuthorName { get; set; }
        public string Email { get; set; }
        public string Content { get; set; }
        public bool IsApproved { get; set; }

        public CommentStatus Status { get; set; }

        public Guid ProductId { get; set; }
        public Product Product { get; set; }
    }
}
