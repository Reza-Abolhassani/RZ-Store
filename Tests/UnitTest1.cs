using Data;
using Data.Repositories;
using Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Services;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Tests
{
    public class TransactionHistoryTests
    {
        private Mock<IRepository<TransactionHistory>> CreateRepository()
        {
            return new Mock<IRepository<TransactionHistory>>();
        }

        private Mock<UserManager<User>> CreateUserManager()
        {
            return new Mock<UserManager<User>>(
                Mock.Of<IUserStore<User>>(),
                Options.Create(new IdentityOptions()),
                new PasswordHasher<User>(),
                Array.Empty<IUserValidator<User>>(),
                Array.Empty<IPasswordValidator<User>>(),
                new UpperInvariantLookupNormalizer(),
                new IdentityErrorDescriber(),
                null,
                Mock.Of<ILogger<UserManager<User>>>());
        }

        [Fact]
        public async Task AddTransactionAsync_ShouldThrowException_WhenAmountIsZero()
        {
            var repository = CreateRepository();
            var userManager = CreateUserManager();
            var service = new TransactionHistoryService(repository.Object, userManager.Object);

            var transaction = new TransactionHistory
            {
                Amount = 0,
                UserId = Guid.NewGuid(),
                Type = TransactionType.Deposit
            };

            await Assert.ThrowsAsync<ArgumentException>(
                () => service.AddTransactionAsync(transaction));

            repository.Verify(x => x.AddAsync(
                It.IsAny<TransactionHistory>(),
                It.IsAny<System.Threading.CancellationToken>(),
                true), Times.Never);
        }

        [Fact]
        public async Task AddTransactionAsync_ShouldThrowException_WhenAmountIsNegative()
        {
            var repository = CreateRepository();
            var userManager = CreateUserManager();
            var service = new TransactionHistoryService(repository.Object, userManager.Object);

            var transaction = new TransactionHistory
            {
                Amount = -1000,
                UserId = Guid.NewGuid(),
                Type = TransactionType.Deposit
            };

            await Assert.ThrowsAsync<ArgumentException>(
                () => service.AddTransactionAsync(transaction));

            repository.Verify(x => x.AddAsync(
                It.IsAny<TransactionHistory>(),
                It.IsAny<System.Threading.CancellationToken>(),
                true), Times.Never);
        }

        [Fact]
        public async Task AddTransactionAsync_ShouldThrowException_WhenTransactionIsNull()
        {
            var repository = CreateRepository();
            var userManager = CreateUserManager();
            var service = new TransactionHistoryService(repository.Object, userManager.Object);

            await Assert.ThrowsAsync<ArgumentNullException>(
                () => service.AddTransactionAsync(null));

            repository.Verify(x => x.AddAsync(
                It.IsAny<TransactionHistory>(),
                It.IsAny<System.Threading.CancellationToken>(),
                true), Times.Never);
        }

        [Fact]
        public async Task AddTransactionAsync_ShouldThrowException_WhenUserIdIsEmpty()
        {
            var repository = CreateRepository();
            var userManager = CreateUserManager();
            var service = new TransactionHistoryService(repository.Object, userManager.Object);

            var transaction = new TransactionHistory
            {
                Amount = 10000,
                UserId = Guid.Empty,
                Type = TransactionType.Deposit
            };

            await Assert.ThrowsAsync<ArgumentException>(
                () => service.AddTransactionAsync(transaction));

            repository.Verify(x => x.AddAsync(
                It.IsAny<TransactionHistory>(),
                It.IsAny<System.Threading.CancellationToken>(),
                true), Times.Never);
        }

        [Fact]
        public async Task AddTransactionAsync_ShouldThrowException_WhenTransactionTypeIsInvalid()
        {
            var repository = CreateRepository();
            var userManager = CreateUserManager();
            var service = new TransactionHistoryService(repository.Object, userManager.Object);

            var transaction = new TransactionHistory
            {
                Amount = 10000,
                UserId = Guid.NewGuid(),
                Type = (TransactionType)99
            };

            await Assert.ThrowsAsync<ArgumentException>(
                () => service.AddTransactionAsync(transaction));

            repository.Verify(x => x.AddAsync(
                It.IsAny<TransactionHistory>(),
                It.IsAny<System.Threading.CancellationToken>(),
                true), Times.Never);
        }

        [Fact]
        public async Task AddTransactionAsync_ShouldThrowException_WhenUserDoesNotExist()
        {
            var repository = CreateRepository();
            var userManager = CreateUserManager();
            var userId = Guid.NewGuid();

            userManager.Setup(x => x.FindByIdAsync(userId.ToString()))
                .ReturnsAsync((User)null);

            var service = new TransactionHistoryService(repository.Object, userManager.Object);

            var transaction = new TransactionHistory
            {
                Amount = 10000,
                UserId = userId,
                Type = TransactionType.Deposit
            };

            await Assert.ThrowsAsync<ArgumentException>(
                () => service.AddTransactionAsync(transaction));

            repository.Verify(x => x.AddAsync(
                It.IsAny<TransactionHistory>(),
                It.IsAny<System.Threading.CancellationToken>(),
                true), Times.Never);
        }

        [Fact]
        public async Task AddTransactionAsync_ShouldSaveTransaction_WhenDataIsValid()
        {
            var repository = CreateRepository();
            var userManager = CreateUserManager();
            var userId = Guid.NewGuid();

            userManager.Setup(x => x.FindByIdAsync(userId.ToString()))
                .ReturnsAsync(new User());

            var service = new TransactionHistoryService(repository.Object, userManager.Object);

            var transaction = new TransactionHistory
            {
                Amount = 10000,
                UserId = userId,
                Type = TransactionType.Deposit
            };

            await service.AddTransactionAsync(transaction);

            Assert.Equal(TransactionStatus.Pending, transaction.Status);

            repository.Verify(x => x.AddAsync(
                It.Is<TransactionHistory>(t =>
                    t.Amount == 10000 &&
                    t.UserId == userId &&
                    t.Type == TransactionType.Deposit &&
                    t.Status == TransactionStatus.Pending),
                It.IsAny<System.Threading.CancellationToken>(),
                true), Times.Once);
        }

        [Fact]
        public async Task GetUserTransactionsAsync_ShouldReturnOnlyTransactionsOfRequestedUser()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            using var dbContext = new ApplicationDbContext(options);

            var userId = Guid.NewGuid();
            var anotherUserId = Guid.NewGuid();

            dbContext.Set<TransactionHistory>().AddRange(
                new TransactionHistory { Amount = 10000, UserId = userId, Type = TransactionType.Deposit },
                new TransactionHistory { Amount = 20000, UserId = userId, Type = TransactionType.Withdrawal },
                new TransactionHistory { Amount = 30000, UserId = anotherUserId, Type = TransactionType.Deposit });

            await dbContext.SaveChangesAsync();

            var repository = new Repository<TransactionHistory>(dbContext);
            var userManager = CreateUserManager();
            var service = new TransactionHistoryService(repository, userManager.Object);

            var result = await service.GetUserTransactionsAsync(userId);

            Assert.Equal(2, result.Count);
            Assert.All(result, x => Assert.Equal(userId, x.UserId));
        }

        [Fact]
        public async Task GetTransactionByIdAsync_ShouldReturnTransaction_WhenTransactionExists()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            using var dbContext = new ApplicationDbContext(options);

            var transactionId = Guid.NewGuid();

            dbContext.Set<TransactionHistory>().Add(
                new TransactionHistory
                {
                    Id = transactionId,
                    Amount = 15000,
                    UserId = Guid.NewGuid(),
                    Type = TransactionType.Deposit,
                    Status = TransactionStatus.Pending
                });

            await dbContext.SaveChangesAsync();

            var repository = new Repository<TransactionHistory>(dbContext);
            var userManager = CreateUserManager();
            var service = new TransactionHistoryService(repository, userManager.Object);

            var result = await service.GetTransactionByIdAsync(transactionId);

            Assert.NotNull(result);
            Assert.Equal(transactionId, result.Id);
            Assert.Equal(15000, result.Amount);
        }
    }
}