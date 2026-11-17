using System;
using System.Collections.Generic;
using System.Text;

namespace Entities
{
    public class FavoriteItem : BaseEntity
    {
        public Guid ProductId { get; set; }
        public Product Product { get; set; }
        public Guid UserId { get; set; }
        public User User { get; set; }
    }
}
