using System;
using System.Collections.Generic;
using System.Text;

namespace Entities
{
    public class Category : BaseEntity
    {

        public string Title { get; set; }
        public int Order { get; set; }
        public string ImageUrl { get; set; }
        public string Description { get; set; }

        public Guid? ParentId { get; set; }

        // دسته بندی والد
        public Category Parent { get; set; }

        // زیر دسته ها
        public ICollection<Category> Children { get; set; }
            = new List<Category>();
    }
}
