using Entities;
using Entities.Constants;

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Data.Repositories
{
    public interface IUserRepository
    {
        //Task<User> GetByUserAndPass(string username, string password, CancellationToken cancellationToken);

        //Task AddAsync(User user, string password, CancellationToken cancellationToken);
        //Task UpdateSecurityStampAsync(User user, CancellationToken cancellationToken);
        //Task UpdateLastLoginDateAsync(User user, CancellationToken cancellationToken);


        Task<bool> UpdateUserRoles(User user, string Role, CancellationToken cancellationToken);

        Task<SystemRoles> CheckUserRole(Guid Id, CancellationToken cancellationToken);
        Task<bool> UpdatePassword(Guid Id, string Password, CancellationToken cancellationToken);
        Task<bool> UpdateLastLoginDate(Guid Id, CancellationToken cancellationToken);
    }
}