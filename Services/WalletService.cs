using Common;
using Data;
using Data.Repositories;
using Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Services.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Services
{
    public class WalletService : IWalletService, IScopedDependency
    {
        private readonly IRepository<UserWallet> _walletRepository;
        private readonly ITransactionHistoryService _transactionHistoryService;
        private readonly ApplicationDbContext _dbContext;
        private readonly UserManager<User> _userManager;

        public WalletService(
            IRepository<UserWallet> walletRepository,
            ITransactionHistoryService transactionHistoryService,
            ApplicationDbContext dbContext,
            UserManager<User> userManager)
        {
            _walletRepository = walletRepository;
            _transactionHistoryService = transactionHistoryService;
            _dbContext = dbContext;
            _userManager = userManager;
        }

        public async Task<UserWallet> CreateWalletAsync(Guid userId)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("شناسه کاربر معتبر نیست.");

            var user = await _userManager.FindByIdAsync(userId.ToString());

            if (user == null)
                throw new ArgumentException("کاربر موردنظر وجود ندارد.");

            var existingWallet = await _walletRepository.Table
                .FirstOrDefaultAsync(x => x.UserId == userId);

            if (existingWallet != null)
            {
                if (!existingWallet.Active)
                    throw new InvalidOperationException("کیف پول کاربر غیرفعال است.");

                return existingWallet;
            }

            var wallet = new UserWallet
            {
                UserId = userId,
                Amount = 0
            };

            await _walletRepository.AddAsync(wallet, CancellationToken.None);

            return wallet;
        }

        public async Task<UserWallet?> GetWalletByUserIdAsync(Guid userId)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("شناسه کاربر معتبر نیست.");

            return await _walletRepository.TableNoTracking
                .FirstOrDefaultAsync(x => x.UserId == userId);
        }

        public async Task<long> GetBalanceAsync(Guid userId)
        {
            var wallet = await GetWalletByUserIdAsync(userId);

            return wallet?.Amount ?? 0;
        }

        public async Task DepositAsync(Guid userId, long amount, string description)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("شناسه کاربر معتبر نیست.");

            if (amount <= 0)
                throw new ArgumentException("مبلغ شارژ باید بزرگ‌تر از صفر باشد.");

            await using var dbTransaction =
                await _dbContext.Database.BeginTransactionAsync();

            try
            {
                var wallet = await _walletRepository.Table
                    .FirstOrDefaultAsync(x => x.UserId == userId && x.Active);

                if (wallet == null)
                    throw new InvalidOperationException("کیف پول فعال کاربر وجود ندارد.");

                if (wallet.Amount > long.MaxValue - amount)
                    throw new ArgumentException("مبلغ از محدوده مجاز بیشتر است.");

                wallet.Amount += amount;

                var transaction = new TransactionHistory
                {
                    UserId = userId,
                    Amount = amount,
                    Description = description,
                    Type = TransactionType.Deposit
                };

                await _transactionHistoryService.AddTransactionAsync(
                    transaction, false);

                transaction.Status = TransactionStatus.Success;

                await _dbContext.SaveChangesAsync();

                await dbTransaction.CommitAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                await dbTransaction.RollbackAsync();

                throw new InvalidOperationException(
                    "موجودی کیف پول هم‌زمان تغییر کرده است. لطفاً دوباره تلاش کنید.");
            }
            catch
            {
                await dbTransaction.RollbackAsync();
                throw;
            }
        }

        public async Task WithdrawAsync(Guid userId, long amount, string description)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("شناسه کاربر معتبر نیست.");

            if (amount <= 0)
                throw new ArgumentException("مبلغ برداشت باید بزرگ‌تر از صفر باشد.");

            await using var dbTransaction =
                await _dbContext.Database.BeginTransactionAsync();

            try
            {
                var wallet = await _walletRepository.Table
                    .FirstOrDefaultAsync(x => x.UserId == userId && x.Active);

                if (wallet == null)
                    throw new InvalidOperationException("کیف پول فعال کاربر وجود ندارد.");

                if (wallet.Amount < amount)
                    throw new InvalidOperationException("موجودی کیف پول کافی نیست.");

                wallet.Amount -= amount;

                var transaction = new TransactionHistory
                {
                    UserId = userId,
                    Amount = amount,
                    Description = description,
                    Type = TransactionType.Withdrawal
                };

                await _transactionHistoryService.AddTransactionAsync(
                    transaction, false);

                transaction.Status = TransactionStatus.Success;

                await _dbContext.SaveChangesAsync();

                await dbTransaction.CommitAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                await dbTransaction.RollbackAsync();

                throw new InvalidOperationException(
                    "موجودی کیف پول هم‌زمان تغییر کرده است. لطفاً دوباره تلاش کنید.");
            }
            catch
            {
                await dbTransaction.RollbackAsync();
                throw;
            }
        }
    }
}