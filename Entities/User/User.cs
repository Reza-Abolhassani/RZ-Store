using Entities.Constants;

using Microsoft.AspNetCore.Identity;

using System;

namespace Entities
{
    public class User : IdentityUser<Guid>, IEntity<Guid>
    {
        public User()
        {
            IsActive = true;
        }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTimeOffset? LastLoginDate { get; set; }
        public DateTime RegisterDate { get; set; }
        public bool IsActive { get; set; }
        public long WalletAmount { get; set; }
        public string ImageUrl { get; set; }

        //public GenderType Gender { get; set; }

    }

}
