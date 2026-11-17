using System;
using System.Collections.Generic;
using System.Text;

namespace Entities
{
    public class Blog : BaseEntity
    {
        public string Title { get; set; }
        public string Content { get; set; }
        public string ImageUrl { get; set; }
        public Guid BlogCategoryId { get; set; }
        public BlogCategory BlogCategory { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime UpdateDate { get; set; }
        public string Author { get; set; }
    }
}
