using Common;

using Entities;
using Entities.Constants;

using Microsoft.AspNetCore.Identity;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Data.Repositories
{
    public class UserRepository : IUserRepository, IScopedDependency
    {
        private readonly UserManager<User> userManager;
        public UserRepository(UserManager<User> userManager)
        {
            this.userManager = userManager;
        }

        public async Task<bool> UpdateUserRoles(User user, string Role, CancellationToken cancellationToken)
        {
            var Roles = await userManager.GetRolesAsync(user);
            if (Roles.Any())
            {
                await userManager.RemoveFromRolesAsync(user, Roles);
            }
            var Status = await userManager.AddToRoleAsync(user, Role);
            return Status.Succeeded;
        }
        public async Task<SystemRoles> CheckUserRole(Guid Id, CancellationToken cancellationToken)
        {
            var User = await userManager.FindByIdAsync(Id.ToString());
            var Roles = await userManager.GetRolesAsync(User);
            foreach (var item in Roles)
            {
                if (item == "Admin")
                {
                    return SystemRoles.Admin;
                }
                else if (item == "Client")
                {
                    return SystemRoles.Client;
                }
             
            }
            return SystemRoles.Client;
        }
        public async Task<bool> UpdatePassword(Guid Id, string Password, CancellationToken cancellationToken)
        {
            var User = await userManager.FindByIdAsync(Id.ToString());
            var Token = await userManager.GeneratePasswordResetTokenAsync(User);
            var Status = await userManager.ResetPasswordAsync(User, Token, Password);

            return Status.Succeeded;
        }
        public async Task<bool> UpdateLastLoginDate(Guid Id, CancellationToken cancellationToken)
        {
            var User = await userManager.FindByIdAsync(Id.ToString());

            User.LastLoginDate = DateTime.UtcNow;

            await userManager.UpdateAsync(User);

            return true;
        }
    }
}
