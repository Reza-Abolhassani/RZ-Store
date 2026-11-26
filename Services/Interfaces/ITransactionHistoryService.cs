using Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Services.Interfaces
{
    public interface ITransactionHistoryService
    {
        Task AddTransactionAsync(TransactionHistory transaction, bool saveNow = true);

        Task<List<TransactionHistory>> GetUserTransactionsAsync(Guid userId);

        Task<TransactionHistory> GetTransactionByIdAsync(Guid transactionId);

        Task<TransactionHistory> GetUserTransactionByIdAsync(Guid transactionId, Guid userId);

        Task<List<TransactionHistory>> GetAllTransactionsAsync();
    }
}