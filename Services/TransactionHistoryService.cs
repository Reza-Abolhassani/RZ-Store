using Common;
using Data.Repositories;
using Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Services
{
    public class TransactionHistoryService : ITransactionHistoryService, IScopedDependency
    {
        private readonly IRepository<TransactionHistory> _repository;
        private readonly UserManager<User> _userManager;

        public TransactionHistoryService(
            IRepository<TransactionHistory> repository,
            UserManager<User> userManager)
        {
            _repository = repository;
            _userManager = userManager;
        }

        public async Task AddTransactionAsync(TransactionHistory transaction, bool saveNow = true)
        {
            if (transaction == null)
                throw new ArgumentNullException(nameof(transaction));

            if (transaction.Amount <= 0)
                throw new ArgumentException("مبلغ تراکنش باید بزرگ‌تر از صفر باشد.");

            if (transaction.UserId == Guid.Empty)
                throw new ArgumentException("شناسه کاربر معتبر نیست.");

            if (!Enum.IsDefined(typeof(TransactionType), transaction.Type))
                throw new ArgumentException("نوع تراکنش معتبر نیست.");

            var user = await _userManager.FindByIdAsync(transaction.UserId.ToString());

            if (user == null)
                throw new ArgumentException("کاربر موردنظر وجود ندارد.");

            transaction.Status = TransactionStatus.Pending;

            await _repository.AddAsync(transaction, CancellationToken.None, saveNow);
        }

        public async Task<List<TransactionHistory>> GetUserTransactionsAsync(Guid userId)
        {
            return await _repository.TableNoTracking
                .Where(x => x.UserId == userId)
                .ToListAsync();
        }

        public async Task<TransactionHistory> GetTransactionByIdAsync(Guid transactionId)
        {
            return await _repository.TableNoTracking
                .FirstOrDefaultAsync(x => x.Id == transactionId);
        }

        public async Task<TransactionHistory> GetUserTransactionByIdAsync(Guid transactionId, Guid userId)
        {
            return await _repository.TableNoTracking
                .FirstOrDefaultAsync(x => x.Id == transactionId && x.UserId == userId);
        }

        public async Task<List<TransactionHistory>> GetAllTransactionsAsync()
        {
            return await _repository.TableNoTracking
                .ToListAsync();
        }
    }
}