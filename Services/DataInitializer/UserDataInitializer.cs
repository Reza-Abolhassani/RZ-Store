using Common;
using Common.Utilities;

using Data.Repositories;

using Entities;
using Entities.Setting;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using System.Threading;
using System.Threading.Tasks;

namespace Services.DataInitializer
{
    public class UserDataInitializer : IDataInitializer
    {
        private readonly UserManager<User> userManager;
        private readonly RoleManager<Role> roleManager;
        private readonly IRepository<Setting> SettingRepo;
        private readonly DataSeedSettings seedSettings;

        public UserDataInitializer(UserManager<User> userManager, RoleManager<Role> roleManager, IRepository<Setting> settingRepo, IOptionsSnapshot<SiteSettings> settings)
        {
            this.userManager = userManager;
            this.roleManager = roleManager;
            SettingRepo = settingRepo;
            seedSettings = settings.Value.DataSeedSettings ?? new DataSeedSettings();
        }

        public async Task InitializeDataAsync(CancellationToken cancellationToken = default)
        {
            if (seedSettings.SeedDefaultRoles)
            {
                await EnsureRoleAsync("Admin", "Admin role");
                await EnsureRoleAsync("Client", "Client role");
            }

            if (seedSettings.SeedDemoUsers)
            {
                await EnsureUserAsync(seedSettings.AdminUserName, seedSettings.AdminEmail, seedSettings.AdminPassword, "Admin");
                await EnsureUserAsync(seedSettings.ClientUserName, seedSettings.ClientEmail, seedSettings.ClientPassword, "Client");
            }

            if (!await SettingRepo.TableNoTracking.AnyAsync(cancellationToken))
            {
                await SettingRepo.AddAsync(new Setting(), cancellationToken);
            }
        }

        private async Task EnsureRoleAsync(string name, string description)
        {
            if (!await roleManager.RoleExistsAsync(name))
            {
                await roleManager.CreateAsync(new Role { Name = name, Description = description });
            }
        }

        private async Task EnsureUserAsync(string userName, string email, string password, string role)
        {
            if (!userName.HasValue() || !email.HasValue() || !password.HasValue())
                return;

            if (await userManager.Users.AsNoTracking().AnyAsync(p => p.UserName == userName))
                return;

            var user = new User
            {
                UserName = userName,
                Email = email
            };

            var createResult = await userManager.CreateAsync(user, password);
            if (createResult.Succeeded)
            {
                await userManager.AddToRoleAsync(user, role);
            }
        }
    }
}
