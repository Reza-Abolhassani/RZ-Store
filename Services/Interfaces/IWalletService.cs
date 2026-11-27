using Entities;
using System;
using System.Threading.Tasks;

namespace Services.Interfaces
{
    public interface IWalletService
    {
        Task<UserWallet> CreateWalletAsync(Guid userId);

        Task<UserWallet?> GetWalletByUserIdAsync(Guid userId);

        Task<long> GetBalanceAsync(Guid userId);

        Task DepositAsync(
            Guid userId,
            long amount,
            string description,
            Guid? paymentId = null);

        Task WithdrawAsync(
            Guid userId,
            long amount,
            string description,
            Guid? paymentId = null);

        Task RefundAsync(
            Guid userId,
            Guid paymentId,
            string description);
    }
}