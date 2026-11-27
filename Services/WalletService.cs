using Common;
using Data;
using Data.Repositories;
using Entities;
using Entities.Payments;
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

            var existingWallet = await _walletRepository.AllTableNoTracking
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

        public async Task DepositAsync(
    Guid userId,
    long amount,
    string description,
    Guid? paymentId = null)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("شناسه کاربر معتبر نیست.");

            if (amount <= 0)
                throw new ArgumentException("مبلغ شارژ باید بزرگ‌تر از صفر باشد.");

            // بررسی مالکیت تراکنش
            var ownsTransaction =
                _dbContext.Database.CurrentTransaction == null;

            // فقط در صورت نبود تراکنش فعال، تراکنش جدید ایجاد می‌کنیم
            await using var dbTransaction = ownsTransaction
                ? await _dbContext.Database.BeginTransactionAsync()
                : null;

            try
            {
                if (paymentId.HasValue)
                {
                    var payment = await _dbContext.Set<Payment>()
                        .FirstOrDefaultAsync(x =>
                            x.Id == paymentId.Value &&
                            x.UserId == userId);

                    if (payment == null)
                        throw new InvalidOperationException(
                            "پرداخت موردنظر برای این کاربر وجود ندارد.");

                    if (payment.Type != PaymentType.WalletCharge ||
                        payment.Amount != amount)
                    {
                        throw new InvalidOperationException(
                            "اطلاعات پرداخت با درخواست شارژ مطابقت ندارد.");
                    }
                }

                if (paymentId.HasValue)
                {
                    var existingTransaction = await _dbContext
                        .Set<TransactionHistory>()
                        .FirstOrDefaultAsync(x =>
                            x.PaymentId == paymentId &&
                            x.Type == TransactionType.Deposit);

                    if (existingTransaction != null)
                    {
                        if (existingTransaction.UserId == userId &&
                            existingTransaction.Amount == amount &&
                            existingTransaction.Status == TransactionStatus.Success)
                        {
                            return;
                        }

                        throw new InvalidOperationException(
                            "برای این پرداخت قبلاً تراکنش شارژ ثبت شده است یا اطلاعات آن با درخواست فعلی مطابقت ندارد.");
                    }
                }

                var wallet = await _walletRepository.Table
                    .FirstOrDefaultAsync(x =>
                        x.UserId == userId && x.Active);

                if (wallet == null)
                    throw new InvalidOperationException(
                        "کیف پول فعال کاربر وجود ندارد.");

                if (wallet.Amount > long.MaxValue - amount)
                    throw new ArgumentException(
                        "مبلغ از محدوده مجاز بیشتر است.");

                wallet.Amount += amount;

                var transaction = new TransactionHistory
                {
                    UserId = userId,
                    Amount = amount,
                    Description = description,
                    Type = TransactionType.Deposit,
                    PaymentId = paymentId
                };

                await _transactionHistoryService.AddTransactionAsync(
                    transaction, false);

                transaction.Status = TransactionStatus.Success;

                await _dbContext.SaveChangesAsync();

                // فقط مالک تراکنش اجازه Commit دارد
                if (ownsTransaction)
                    await dbTransaction!.CommitAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                // فقط مالک تراکنش اجازه Rollback دارد
                if (ownsTransaction)
                    await dbTransaction!.RollbackAsync();

                throw new InvalidOperationException(
                    "موجودی کیف پول هم‌زمان تغییر کرده است. لطفاً دوباره تلاش کنید.");
            }
            catch
            {
                if (ownsTransaction)
                    await dbTransaction!.RollbackAsync();

                throw;
            }
        }

        public async Task WithdrawAsync(
            Guid userId,
            long amount,
            string description,
            Guid? paymentId = null)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("شناسه کاربر معتبر نیست.");

            if (amount <= 0)
                throw new ArgumentException("مبلغ برداشت باید بزرگ‌تر از صفر باشد.");

            await using var dbTransaction =
                await _dbContext.Database.BeginTransactionAsync();

            try
            {
                if (paymentId.HasValue)
                {
                    var payment = await _dbContext.Set<Payment>()
                        .FirstOrDefaultAsync(x =>
                            x.Id == paymentId.Value &&
                            x.UserId == userId);

                    if (payment == null)
                        throw new InvalidOperationException(
                            "پرداخت موردنظر برای این کاربر وجود ندارد.");

                    if (payment.Type != PaymentType.OrderPayment ||
                        payment.WalletAmount != amount)
                    {
                        throw new InvalidOperationException(
                            "اطلاعات پرداخت با درخواست برداشت از کیف پول مطابقت ندارد.");
                    }
                }
                if (paymentId.HasValue)
                {
                    var existingTransaction = await _dbContext
                        .Set<TransactionHistory>()
                        .FirstOrDefaultAsync(x =>
                            x.PaymentId == paymentId &&
                            x.Type == TransactionType.Withdrawal);

                    if (existingTransaction != null)
                    {
                        if (existingTransaction.UserId == userId &&
                            existingTransaction.Amount == amount &&
                            existingTransaction.Status == TransactionStatus.Success)
                        {
                            return;
                        }

                        throw new InvalidOperationException(
                            "برای این پرداخت قبلاً تراکنش برداشت ثبت شده است یا اطلاعات آن با درخواست فعلی مطابقت ندارد.");
                    }
                }

                var wallet = await _walletRepository.Table
                    .FirstOrDefaultAsync(x =>
                        x.UserId == userId && x.Active);

                if (wallet == null)
                    throw new InvalidOperationException(
                        "کیف پول فعال کاربر وجود ندارد.");

                if (wallet.Amount < amount)
                    throw new InvalidOperationException(
                        "موجودی کیف پول کافی نیست.");

                wallet.Amount -= amount;

                var transaction = new TransactionHistory
                {
                    UserId = userId,
                    Amount = amount,
                    Description = description,
                    Type = TransactionType.Withdrawal,
                    PaymentId = paymentId
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

        public async Task RefundAsync(
    Guid userId,
    Guid paymentId,
    string description)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("شناسه کاربر معتبر نیست.");

            if (paymentId == Guid.Empty)
                throw new ArgumentException("شناسه پرداخت معتبر نیست.");

            await using var dbTransaction =
                await _dbContext.Database.BeginTransactionAsync();

            try
            {
                var payment = await _dbContext.Set<Payment>()
                    .FirstOrDefaultAsync(x =>
                        x.Id == paymentId &&
                        x.UserId == userId);

                if (payment == null)
                    throw new InvalidOperationException(
                        "پرداخت موردنظر برای این کاربر وجود ندارد.");

                long refundAmount;
                TransactionType originalTransactionType;

                if (payment.Type == PaymentType.OrderPayment)
                {
                    refundAmount = payment.WalletAmount;
                    originalTransactionType = TransactionType.Withdrawal;

                    if (refundAmount < 0)
                        throw new InvalidOperationException(
                            "مبلغ کیف پول برای برگشت وجه معتبر نیست.");
                }
                else if (payment.Type == PaymentType.WalletCharge)
                {
                    refundAmount = payment.Amount;
                    originalTransactionType = TransactionType.Deposit;

                    if (refundAmount <= 0)
                        throw new InvalidOperationException(
                            "مبلغ شارژ کیف پول برای برگشت معتبر نیست.");
                }
                else
                {
                    throw new InvalidOperationException(
                        "نوع پرداخت برای برگشت وجه معتبر نیست.");
                }

                var existingRefund = await _dbContext
                    .Set<TransactionHistory>()
                    .FirstOrDefaultAsync(x =>
                        x.PaymentId == paymentId &&
                        x.Type == TransactionType.Refund);

                if (existingRefund != null)
                {
                    if (existingRefund.UserId == userId &&
                        existingRefund.Amount == refundAmount &&
                        existingRefund.Status == TransactionStatus.Success)
                    {
                        return;
                    }

                    throw new InvalidOperationException(
                        "برای این پرداخت قبلاً تراکنش برگشت وجه ثبت شده است یا اطلاعات آن با درخواست فعلی مطابقت ندارد.");
                }

                if (payment.Status != PaymentStatus.Success)
                    throw new InvalidOperationException(
                        "فقط پرداخت‌های موفق قابل برگشت هستند.");

                // پرداخت سفارش بدون سهم کیف پول، عملیات برگشت کیف پول ندارد.
                if (payment.Type == PaymentType.OrderPayment &&
                    refundAmount == 0)
                {
                    return;
                }

                var originalTransaction = await _dbContext
                    .Set<TransactionHistory>()
                    .FirstOrDefaultAsync(x =>
                        x.PaymentId == paymentId &&
                        x.UserId == userId &&
                        x.Type == originalTransactionType);

                if (originalTransaction == null ||
                    originalTransaction.Status != TransactionStatus.Success ||
                    originalTransaction.Amount != refundAmount)
                {
                    throw new InvalidOperationException(
                        "تراکنش اولیه معتبر و موفق برای برگشت وجه پیدا نشد.");
                }

                var wallet = await _walletRepository.Table
                    .FirstOrDefaultAsync(x =>
                        x.UserId == userId && x.Active);

                if (wallet == null)
                    throw new InvalidOperationException(
                        "کیف پول فعال کاربر وجود ندارد.");

                if (payment.Type == PaymentType.OrderPayment)
                {
                    if (wallet.Amount > long.MaxValue - refundAmount)
                        throw new ArgumentException(
                            "مبلغ برگشت وجه از محدوده مجاز بیشتر است.");

                    wallet.Amount += refundAmount;
                }
                else
                {
                    if (wallet.Amount < refundAmount)
                        throw new InvalidOperationException(
                            "موجودی کیف پول برای برگشت شارژ کافی نیست.");

                    wallet.Amount -= refundAmount;
                }

                var refundTransaction = new TransactionHistory
                {
                    UserId = userId,
                    Amount = refundAmount,
                    Description = description,
                    Type = TransactionType.Refund,
                    PaymentId = paymentId
                };

                await _transactionHistoryService.AddTransactionAsync(
                    refundTransaction, false);

                refundTransaction.Status = TransactionStatus.Success;

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