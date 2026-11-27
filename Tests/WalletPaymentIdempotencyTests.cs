using Data;
using Data.Repositories;
using Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Data.Sqlite;
using Moq;
using Services;
using Services.Interfaces;
using System;
using System.Threading.Tasks;
using Xunit;
using System.Linq;
using Entities.Payments;

namespace Tests
{
    public class WalletPaymentIdempotencyTests
    {
        // تست‌های جلوگیری از تراکنش مالی تکراری
        // در همین کلاس قرار خواهند گرفت.
        [Fact]
        public async Task DepositAsync_ShouldNotIncreaseBalance_WhenPaymentIdIsRepeated()
        {
            // Arrange
            await using var connection =
                new SqliteConnection("Data Source=:memory:");

            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(connection)
                .Options;

            await using var dbContext =
                new SqliteTestApplicationDbContext(options);

            await dbContext.Database.EnsureCreatedAsync();

            var userId = Guid.NewGuid();
            var paymentId = Guid.NewGuid();

            var user = new User
            {
                Id = userId,
                UserName = "test-user",
                NormalizedUserName = "TEST-USER"
            };

            var wallet = new UserWallet
            {
                UserId = userId,
                Amount = 50000,
                RowVersion = new byte[8]
            };

            dbContext.Users.Add(user);
            dbContext.Set<UserWallet>().Add(wallet);

            var payment = new Payment
            {
                Id = paymentId,
                UserId = userId,
                Amount = 20000,
                WalletAmount = 0,
                GatewayAmount = 20000,
                Type = PaymentType.WalletCharge,
                Method = PaymentMethod.Gateway,
                Status = PaymentStatus.Pending,
                Description = "تست پرداخت"
            };

            dbContext.Set<Payment>().Add(payment);

            await dbContext.SaveChangesAsync();

            var userManager = new Mock<UserManager<User>>(
                Mock.Of<IUserStore<User>>(),
                Options.Create(new IdentityOptions()),
                new PasswordHasher<User>(),
                Array.Empty<IUserValidator<User>>(),
                Array.Empty<IPasswordValidator<User>>(),
                new UpperInvariantLookupNormalizer(),
                new IdentityErrorDescriber(),
                null,
                Mock.Of<ILogger<UserManager<User>>>());

            userManager
                .Setup(x => x.FindByIdAsync(userId.ToString()))
                .ReturnsAsync(user);

            var walletRepository =
                new Repository<UserWallet>(dbContext);

            var transactionRepository =
                new Repository<TransactionHistory>(dbContext);

            var transactionHistoryService =
                new TransactionHistoryService(
                    transactionRepository,
                    userManager.Object);

            var service = new WalletService(
                walletRepository,
                transactionHistoryService,
                dbContext,
                userManager.Object);

            // Act
            await service.DepositAsync(
                userId,
                20000,
                "تست شارژ تکراری",
                paymentId);

            await service.DepositAsync(
                userId,
                20000,
                "تست شارژ تکراری",
                paymentId);

            // Assert
            var savedWallet = await dbContext.Set<UserWallet>()
                .AsNoTracking()
                .SingleAsync(x => x.UserId == userId);

            var transactions = await dbContext.Set<TransactionHistory>()
                .AsNoTracking()
                .Where(x => x.PaymentId == paymentId)
                .ToListAsync();

            Assert.Equal(70000, savedWallet.Amount);

            Assert.Single(transactions);

            Assert.Equal(20000, transactions[0].Amount);
            Assert.Equal(TransactionType.Deposit, transactions[0].Type);
            Assert.Equal(TransactionStatus.Success, transactions[0].Status);
            Assert.Equal(userId, transactions[0].UserId);
        }

        [Fact]
        public async Task DepositAsync_ShouldThrowException_WhenPaymentIdIsReusedWithDifferentAmount()
        {
            // Arrange
            await using var connection =
                new SqliteConnection("Data Source=:memory:");

            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(connection)
                .Options;

            await using var dbContext =
                new SqliteTestApplicationDbContext(options);

            await dbContext.Database.EnsureCreatedAsync();

            var userId = Guid.NewGuid();
            var paymentId = Guid.NewGuid();

            var user = new User
            {
                Id = userId,
                UserName = "test-user",
                NormalizedUserName = "TEST-USER"
            };

            var wallet = new UserWallet
            {
                UserId = userId,
                Amount = 50000,
                RowVersion = new byte[8]
            };

            var payment = new Payment
            {
                Id = paymentId,
                UserId = userId,
                Amount = 20000,
                WalletAmount = 0,
                GatewayAmount = 20000,
                Type = PaymentType.WalletCharge,
                Method = PaymentMethod.Gateway,
                Status = PaymentStatus.Pending,
                Description = "تست پرداخت"
            };

            dbContext.Users.Add(user);
            dbContext.Set<UserWallet>().Add(wallet);
            dbContext.Set<Payment>().Add(payment);

            await dbContext.SaveChangesAsync();

            var userManager = new Mock<UserManager<User>>(
                Mock.Of<IUserStore<User>>(),
                Options.Create(new IdentityOptions()),
                new PasswordHasher<User>(),
                Array.Empty<IUserValidator<User>>(),
                Array.Empty<IPasswordValidator<User>>(),
                new UpperInvariantLookupNormalizer(),
                new IdentityErrorDescriber(),
                null,
                Mock.Of<ILogger<UserManager<User>>>());

            userManager
                .Setup(x => x.FindByIdAsync(userId.ToString()))
                .ReturnsAsync(user);

            var walletRepository =
                new Repository<UserWallet>(dbContext);

            var transactionRepository =
                new Repository<TransactionHistory>(dbContext);

            var transactionHistoryService =
                new TransactionHistoryService(
                    transactionRepository,
                    userManager.Object);

            var service = new WalletService(
                walletRepository,
                transactionHistoryService,
                dbContext,
                userManager.Object);

            // Act
            await service.DepositAsync(
                userId,
                20000,
                "شارژ اولیه",
                paymentId);

            // Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await service.DepositAsync(
                    userId,
                    30000,
                    "شارژ با مبلغ متفاوت",
                    paymentId));

            var savedWallet = await dbContext.Set<UserWallet>()
                .AsNoTracking()
                .SingleAsync(x => x.UserId == userId);

            var transactions = await dbContext.Set<TransactionHistory>()
                .AsNoTracking()
                .Where(x => x.PaymentId == paymentId)
                .ToListAsync();

            Assert.Equal(70000, savedWallet.Amount);
            Assert.Single(transactions);
            Assert.Equal(20000, transactions[0].Amount);
        }

        [Fact]
        public async Task DepositAsync_ShouldThrowException_WhenPaymentIdBelongsToAnotherUser()
        {
            // Arrange
            await using var connection =
                new SqliteConnection("Data Source=:memory:");

            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(connection)
                .Options;

            await using var dbContext =
                new SqliteTestApplicationDbContext(options);

            await dbContext.Database.EnsureCreatedAsync();

            var firstUserId = Guid.NewGuid();
            var secondUserId = Guid.NewGuid();
            var paymentId = Guid.NewGuid();

            var firstUser = new User
            {
                Id = firstUserId,
                UserName = "first-user",
                NormalizedUserName = "FIRST-USER"
            };

            var secondUser = new User
            {
                Id = secondUserId,
                UserName = "second-user",
                NormalizedUserName = "SECOND-USER"
            };

            var firstWallet = new UserWallet
            {
                Id = Guid.NewGuid(),
                UserId = firstUserId,
                Amount = 50000,
                RowVersion = new byte[8]
            };

            var secondWallet = new UserWallet
            {
                Id = Guid.NewGuid(),
                UserId = secondUserId,
                Amount = 30000,
                RowVersion = new byte[8]
            };

            var payment = new Payment
            {
                Id = paymentId,
                UserId = firstUserId,
                Amount = 20000,
                WalletAmount = 0,
                GatewayAmount = 20000,
                Type = PaymentType.WalletCharge,
                Method = PaymentMethod.Gateway,
                Status = PaymentStatus.Pending,
                Description = "تست پرداخت کاربر اول"
            };

            dbContext.Users.AddRange(firstUser, secondUser);
            dbContext.Set<UserWallet>().AddRange(firstWallet, secondWallet);
            dbContext.Set<Payment>().Add(payment);

            await dbContext.SaveChangesAsync();

            var userManager = new Mock<UserManager<User>>(
                Mock.Of<IUserStore<User>>(),
                Options.Create(new IdentityOptions()),
                new PasswordHasher<User>(),
                Array.Empty<IUserValidator<User>>(),
                Array.Empty<IPasswordValidator<User>>(),
                new UpperInvariantLookupNormalizer(),
                new IdentityErrorDescriber(),
                null,
                Mock.Of<ILogger<UserManager<User>>>());

            userManager
                .Setup(x => x.FindByIdAsync(secondUserId.ToString()))
                .ReturnsAsync(secondUser);

            var walletRepository =
                new Repository<UserWallet>(dbContext);

            var transactionRepository =
                new Repository<TransactionHistory>(dbContext);

            var transactionHistoryService =
                new TransactionHistoryService(
                    transactionRepository,
                    userManager.Object);

            var service = new WalletService(
                walletRepository,
                transactionHistoryService,
                dbContext,
                userManager.Object);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await service.DepositAsync(
                    secondUserId,
                    20000,
                    "تلاش برای استفاده از پرداخت کاربر دیگر",
                    paymentId));

            var savedFirstWallet = await dbContext.Set<UserWallet>()
                .AsNoTracking()
                .SingleAsync(x => x.UserId == firstUserId);

            var savedSecondWallet = await dbContext.Set<UserWallet>()
                .AsNoTracking()
                .SingleAsync(x => x.UserId == secondUserId);

            var transactions = await dbContext.Set<TransactionHistory>()
                .AsNoTracking()
                .Where(x => x.PaymentId == paymentId)
                .ToListAsync();

            Assert.Equal(50000, savedFirstWallet.Amount);
            Assert.Equal(30000, savedSecondWallet.Amount);
            Assert.Empty(transactions);
        }

        [Fact]
        public async Task WithdrawAsync_ShouldNotDecreaseBalance_WhenPaymentIdIsRepeated()
        {
            // Arrange
            await using var connection =
                new SqliteConnection("Data Source=:memory:");

            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(connection)
                .Options;

            await using var dbContext =
                new SqliteTestApplicationDbContext(options);

            await dbContext.Database.EnsureCreatedAsync();

            var userId = Guid.NewGuid();
            var paymentId = Guid.NewGuid();

            var user = new User
            {
                Id = userId,
                UserName = "test-user",
                NormalizedUserName = "TEST-USER"
            };

            var wallet = new UserWallet
            {
                UserId = userId,
                Amount = 50000,
                RowVersion = new byte[8]
            };

            var payment = new Payment
            {
                Id = paymentId,
                UserId = userId,
                Amount = 20000,
                WalletAmount = 20000,
                GatewayAmount = 0,
                Type = PaymentType.OrderPayment,
                Method = PaymentMethod.Wallet,
                Status = PaymentStatus.Pending,
                Description = "تست برداشت تکراری"
            };

            dbContext.Users.Add(user);
            dbContext.Set<UserWallet>().Add(wallet);
            dbContext.Set<Payment>().Add(payment);

            await dbContext.SaveChangesAsync();

            var userManager = new Mock<UserManager<User>>(
                Mock.Of<IUserStore<User>>(),
                Options.Create(new IdentityOptions()),
                new PasswordHasher<User>(),
                Array.Empty<IUserValidator<User>>(),
                Array.Empty<IPasswordValidator<User>>(),
                new UpperInvariantLookupNormalizer(),
                new IdentityErrorDescriber(),
                null,
                Mock.Of<ILogger<UserManager<User>>>());

            userManager
                .Setup(x => x.FindByIdAsync(userId.ToString()))
                .ReturnsAsync(user);

            var walletRepository =
                new Repository<UserWallet>(dbContext);

            var transactionRepository =
                new Repository<TransactionHistory>(dbContext);

            var transactionHistoryService =
                new TransactionHistoryService(
                    transactionRepository,
                    userManager.Object);

            var service = new WalletService(
                walletRepository,
                transactionHistoryService,
                dbContext,
                userManager.Object);

            // Act
            await service.WithdrawAsync(
                userId,
                20000,
                "تست برداشت تکراری",
                paymentId);

            await service.WithdrawAsync(
                userId,
                20000,
                "تست برداشت تکراری",
                paymentId);

            // Assert
            var savedWallet = await dbContext.Set<UserWallet>()
                .AsNoTracking()
                .SingleAsync(x => x.UserId == userId);

            var transactions = await dbContext.Set<TransactionHistory>()
                .AsNoTracking()
                .Where(x => x.PaymentId == paymentId)
                .ToListAsync();

            Assert.Equal(30000, savedWallet.Amount);

            Assert.Single(transactions);

            Assert.Equal(20000, transactions[0].Amount);
            Assert.Equal(TransactionType.Withdrawal, transactions[0].Type);
            Assert.Equal(TransactionStatus.Success, transactions[0].Status);
            Assert.Equal(userId, transactions[0].UserId);
        }


        [Fact]
        public async Task WithdrawAsync_ShouldThrowException_WhenPaymentIdBelongsToAnotherUser()
        {
            // Arrange
            await using var connection =
                new SqliteConnection("Data Source=:memory:");

            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(connection)
                .Options;

            await using var dbContext =
                new SqliteTestApplicationDbContext(options);

            await dbContext.Database.EnsureCreatedAsync();

            var firstUserId = Guid.NewGuid();
            var secondUserId = Guid.NewGuid();
            var paymentId = Guid.NewGuid();

            var firstUser = new User
            {
                Id = firstUserId,
                UserName = "first-user",
                NormalizedUserName = "FIRST-USER"
            };

            var secondUser = new User
            {
                Id = secondUserId,
                UserName = "second-user",
                NormalizedUserName = "SECOND-USER"
            };

            var firstWallet = new UserWallet
            {
                Id = Guid.NewGuid(),
                UserId = firstUserId,
                Amount = 50000,
                RowVersion = new byte[8]
            };

            var secondWallet = new UserWallet
            {
                Id = Guid.NewGuid(),
                UserId = secondUserId,
                Amount = 30000,
                RowVersion = new byte[8]
            };

            var payment = new Payment
            {
                Id = paymentId,
                UserId = firstUserId,
                Amount = 20000,
                WalletAmount = 20000,
                GatewayAmount = 0,
                Type = PaymentType.OrderPayment,
                Method = PaymentMethod.Wallet,
                Status = PaymentStatus.Pending,
                Description = "تست پرداخت کاربر اول"
            };

            dbContext.Users.AddRange(firstUser, secondUser);
            dbContext.Set<UserWallet>().AddRange(firstWallet, secondWallet);
            dbContext.Set<Payment>().Add(payment);

            await dbContext.SaveChangesAsync();

            var userManager = new Mock<UserManager<User>>(
                Mock.Of<IUserStore<User>>(),
                Options.Create(new IdentityOptions()),
                new PasswordHasher<User>(),
                Array.Empty<IUserValidator<User>>(),
                Array.Empty<IPasswordValidator<User>>(),
                new UpperInvariantLookupNormalizer(),
                new IdentityErrorDescriber(),
                null,
                Mock.Of<ILogger<UserManager<User>>>());

            userManager
                .Setup(x => x.FindByIdAsync(secondUserId.ToString()))
                .ReturnsAsync(secondUser);

            var walletRepository =
                new Repository<UserWallet>(dbContext);

            var transactionRepository =
                new Repository<TransactionHistory>(dbContext);

            var transactionHistoryService =
                new TransactionHistoryService(
                    transactionRepository,
                    userManager.Object);

            var service = new WalletService(
                walletRepository,
                transactionHistoryService,
                dbContext,
                userManager.Object);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await service.WithdrawAsync(
                    secondUserId,
                    20000,
                    "تلاش برای استفاده از پرداخت کاربر دیگر",
                    paymentId));

            var savedFirstWallet = await dbContext.Set<UserWallet>()
                .AsNoTracking()
                .SingleAsync(x => x.UserId == firstUserId);

            var savedSecondWallet = await dbContext.Set<UserWallet>()
                .AsNoTracking()
                .SingleAsync(x => x.UserId == secondUserId);

            var transactions = await dbContext.Set<TransactionHistory>()
                .AsNoTracking()
                .Where(x => x.PaymentId == paymentId)
                .ToListAsync();

            Assert.Equal(50000, savedFirstWallet.Amount);
            Assert.Equal(30000, savedSecondWallet.Amount);
            Assert.Empty(transactions);
        }

        [Fact]
        public async Task WithdrawAsync_ShouldThrowException_WhenPaymentTypeIsWalletCharge()
        {
            // Arrange
            await using var connection =
                new SqliteConnection("Data Source=:memory:");

            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(connection)
                .Options;

            await using var dbContext =
                new SqliteTestApplicationDbContext(options);

            await dbContext.Database.EnsureCreatedAsync();

            var userId = Guid.NewGuid();
            var paymentId = Guid.NewGuid();

            var user = new User
            {
                Id = userId,
                UserName = "test-user",
                NormalizedUserName = "TEST-USER"
            };

            var wallet = new UserWallet
            {
                UserId = userId,
                Amount = 50000,
                RowVersion = new byte[8]
            };

            var payment = new Payment
            {
                Id = paymentId,
                UserId = userId,
                Amount = 20000,
                WalletAmount = 0,
                GatewayAmount = 20000,
                Type = PaymentType.WalletCharge,
                Method = PaymentMethod.Gateway,
                Status = PaymentStatus.Pending,
                Description = "تست پرداخت شارژ کیف پول"
            };

            dbContext.Users.Add(user);
            dbContext.Set<UserWallet>().Add(wallet);
            dbContext.Set<Payment>().Add(payment);

            await dbContext.SaveChangesAsync();

            var userManager = new Mock<UserManager<User>>(
                Mock.Of<IUserStore<User>>(),
                Options.Create(new IdentityOptions()),
                new PasswordHasher<User>(),
                Array.Empty<IUserValidator<User>>(),
                Array.Empty<IPasswordValidator<User>>(),
                new UpperInvariantLookupNormalizer(),
                new IdentityErrorDescriber(),
                null,
                Mock.Of<ILogger<UserManager<User>>>());

            userManager
                .Setup(x => x.FindByIdAsync(userId.ToString()))
                .ReturnsAsync(user);

            var walletRepository =
                new Repository<UserWallet>(dbContext);

            var transactionRepository =
                new Repository<TransactionHistory>(dbContext);

            var transactionHistoryService =
                new TransactionHistoryService(
                    transactionRepository,
                    userManager.Object);

            var service = new WalletService(
                walletRepository,
                transactionHistoryService,
                dbContext,
                userManager.Object);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await service.WithdrawAsync(
                    userId,
                    20000,
                    "تلاش برای برداشت با پرداخت شارژ کیف پول",
                    paymentId));

            var savedWallet = await dbContext.Set<UserWallet>()
                .AsNoTracking()
                .SingleAsync(x => x.UserId == userId);

            var transactions = await dbContext.Set<TransactionHistory>()
                .AsNoTracking()
                .Where(x => x.PaymentId == paymentId)
                .ToListAsync();

            Assert.Equal(50000, savedWallet.Amount);
            Assert.Empty(transactions);
        }

        [Fact]
        public async Task WithdrawAsync_ShouldThrowException_WhenAmountDoesNotMatchPaymentWalletAmount()
        {
            // Arrange
            await using var connection =
                new SqliteConnection("Data Source=:memory:");

            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(connection)
                .Options;

            await using var dbContext =
                new SqliteTestApplicationDbContext(options);

            await dbContext.Database.EnsureCreatedAsync();

            var userId = Guid.NewGuid();
            var paymentId = Guid.NewGuid();

            var user = new User
            {
                Id = userId,
                UserName = "test-user",
                NormalizedUserName = "TEST-USER"
            };

            var wallet = new UserWallet
            {
                UserId = userId,
                Amount = 50000,
                RowVersion = new byte[8]
            };

            var payment = new Payment
            {
                Id = paymentId,
                UserId = userId,
                Amount = 20000,
                WalletAmount = 20000,
                GatewayAmount = 0,
                Type = PaymentType.OrderPayment,
                Method = PaymentMethod.Wallet,
                Status = PaymentStatus.Pending,
                Description = "تست مغایرت مبلغ برداشت"
            };

            dbContext.Users.Add(user);
            dbContext.Set<UserWallet>().Add(wallet);
            dbContext.Set<Payment>().Add(payment);

            await dbContext.SaveChangesAsync();

            var userManager = new Mock<UserManager<User>>(
                Mock.Of<IUserStore<User>>(),
                Options.Create(new IdentityOptions()),
                new PasswordHasher<User>(),
                Array.Empty<IUserValidator<User>>(),
                Array.Empty<IPasswordValidator<User>>(),
                new UpperInvariantLookupNormalizer(),
                new IdentityErrorDescriber(),
                null,
                Mock.Of<ILogger<UserManager<User>>>());

            userManager
                .Setup(x => x.FindByIdAsync(userId.ToString()))
                .ReturnsAsync(user);

            var walletRepository =
                new Repository<UserWallet>(dbContext);

            var transactionRepository =
                new Repository<TransactionHistory>(dbContext);

            var transactionHistoryService =
                new TransactionHistoryService(
                    transactionRepository,
                    userManager.Object);

            var service = new WalletService(
                walletRepository,
                transactionHistoryService,
                dbContext,
                userManager.Object);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await service.WithdrawAsync(
                    userId,
                    15000,
                    "تلاش برای برداشت با مبلغ متفاوت",
                    paymentId));

            var savedWallet = await dbContext.Set<UserWallet>()
                .AsNoTracking()
                .SingleAsync(x => x.UserId == userId);

            var transactions = await dbContext.Set<TransactionHistory>()
                .AsNoTracking()
                .Where(x => x.PaymentId == paymentId)
                .ToListAsync();

            Assert.Equal(50000, savedWallet.Amount);
            Assert.Empty(transactions);
        }
        [Fact]
        public async Task WithdrawAsync_ShouldDecreaseBalanceAndSaveTransaction_WhenDataIsValid()
        {
            // Arrange
            await using var connection =
                new SqliteConnection("Data Source=:memory:");

            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(connection)
                .Options;

            await using var dbContext =
                new SqliteTestApplicationDbContext(options);

            await dbContext.Database.EnsureCreatedAsync();

            var userId = Guid.NewGuid();
            var paymentId = Guid.NewGuid();

            var user = new User
            {
                Id = userId,
                UserName = "test-user",
                NormalizedUserName = "TEST-USER"
            };

            var wallet = new UserWallet
            {
                UserId = userId,
                Amount = 50000,
                RowVersion = new byte[8]
            };

            var payment = new Payment
            {
                Id = paymentId,
                UserId = userId,
                Amount = 20000,
                WalletAmount = 20000,
                GatewayAmount = 0,
                Type = PaymentType.OrderPayment,
                Method = PaymentMethod.Wallet,
                Status = PaymentStatus.Pending,
                Description = "تست برداشت موفق"
            };

            dbContext.Users.Add(user);
            dbContext.Set<UserWallet>().Add(wallet);
            dbContext.Set<Payment>().Add(payment);

            await dbContext.SaveChangesAsync();

            var userManager = new Mock<UserManager<User>>(
                Mock.Of<IUserStore<User>>(),
                Options.Create(new IdentityOptions()),
                new PasswordHasher<User>(),
                Array.Empty<IUserValidator<User>>(),
                Array.Empty<IPasswordValidator<User>>(),
                new UpperInvariantLookupNormalizer(),
                new IdentityErrorDescriber(),
                null,
                Mock.Of<ILogger<UserManager<User>>>());

            userManager
                .Setup(x => x.FindByIdAsync(userId.ToString()))
                .ReturnsAsync(user);

            var walletRepository =
                new Repository<UserWallet>(dbContext);

            var transactionRepository =
                new Repository<TransactionHistory>(dbContext);

            var transactionHistoryService =
                new TransactionHistoryService(
                    transactionRepository,
                    userManager.Object);

            var service = new WalletService(
                walletRepository,
                transactionHistoryService,
                dbContext,
                userManager.Object);

            // Act
            await service.WithdrawAsync(
                userId,
                20000,
                "تست برداشت موفق",
                paymentId);

            // Assert
            var savedWallet = await dbContext.Set<UserWallet>()
                .AsNoTracking()
                .SingleAsync(x => x.UserId == userId);

            var transactions = await dbContext.Set<TransactionHistory>()
                .AsNoTracking()
                .Where(x => x.PaymentId == paymentId)
                .ToListAsync();

            Assert.Equal(30000, savedWallet.Amount);

            Assert.Single(transactions);

            Assert.Equal(20000, transactions[0].Amount);
            Assert.Equal(TransactionType.Withdrawal, transactions[0].Type);
            Assert.Equal(TransactionStatus.Success, transactions[0].Status);
            Assert.Equal(userId, transactions[0].UserId);
        }

        [Fact]
        public async Task WithdrawAsync_ShouldThrowException_WhenPaymentIdIsRepeatedWithDifferentAmount()
        {
            // Arrange
            await using var connection =
                new SqliteConnection("Data Source=:memory:");

            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(connection)
                .Options;

            await using var dbContext =
                new SqliteTestApplicationDbContext(options);

            await dbContext.Database.EnsureCreatedAsync();

            var userId = Guid.NewGuid();
            var paymentId = Guid.NewGuid();

            var user = new User
            {
                Id = userId,
                UserName = "test-user",
                NormalizedUserName = "TEST-USER"
            };

            var wallet = new UserWallet
            {
                UserId = userId,
                Amount = 50000,
                RowVersion = new byte[8]
            };

            var payment = new Payment
            {
                Id = paymentId,
                UserId = userId,
                Amount = 20000,
                WalletAmount = 20000,
                GatewayAmount = 0,
                Type = PaymentType.OrderPayment,
                Method = PaymentMethod.Wallet,
                Status = PaymentStatus.Pending,
                Description = "تست برداشت تکراری با مبلغ متفاوت"
            };

            dbContext.Users.Add(user);
            dbContext.Set<UserWallet>().Add(wallet);
            dbContext.Set<Payment>().Add(payment);

            await dbContext.SaveChangesAsync();

            var userManager = new Mock<UserManager<User>>(
                Mock.Of<IUserStore<User>>(),
                Options.Create(new IdentityOptions()),
                new PasswordHasher<User>(),
                Array.Empty<IUserValidator<User>>(),
                Array.Empty<IPasswordValidator<User>>(),
                new UpperInvariantLookupNormalizer(),
                new IdentityErrorDescriber(),
                null,
                Mock.Of<ILogger<UserManager<User>>>());

            userManager
                .Setup(x => x.FindByIdAsync(userId.ToString()))
                .ReturnsAsync(user);

            var walletRepository =
                new Repository<UserWallet>(dbContext);

            var transactionRepository =
                new Repository<TransactionHistory>(dbContext);

            var transactionHistoryService =
                new TransactionHistoryService(
                    transactionRepository,
                    userManager.Object);

            var service = new WalletService(
                walletRepository,
                transactionHistoryService,
                dbContext,
                userManager.Object);

            // Act - برداشت اول
            await service.WithdrawAsync(
                userId,
                20000,
                "برداشت اول",
                paymentId);

            // تغییر سهم کیف پول پرداخت برای آزمایش استفاده مجدد
            payment.WalletAmount = 15000;
            payment.GatewayAmount = 5000;
            payment.Method = PaymentMethod.Combined;

            await dbContext.SaveChangesAsync();

            // Act & Assert - برداشت دوم با مبلغ متفاوت
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await service.WithdrawAsync(
                    userId,
                    15000,
                    "برداشت دوم",
                    paymentId));

            // Assert - بررسی موجودی و تراکنش‌ها
            var savedWallet = await dbContext.Set<UserWallet>()
                .AsNoTracking()
                .SingleAsync(x => x.UserId == userId);

            var transactions = await dbContext.Set<TransactionHistory>()
                .AsNoTracking()
                .Where(x => x.PaymentId == paymentId)
                .ToListAsync();

            Assert.Equal(30000, savedWallet.Amount);

            Assert.Single(transactions);

            Assert.Equal(20000, transactions[0].Amount);
            Assert.Equal(TransactionType.Withdrawal, transactions[0].Type);
            Assert.Equal(TransactionStatus.Success, transactions[0].Status);
        }

        [Fact]
        public async Task RefundAsync_ShouldNotIncreaseBalance_WhenPaymentIdIsRepeated()
        {
            // Arrange
            await using var connection =
                new SqliteConnection("Data Source=:memory:");

            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(connection)
                .Options;

            await using var dbContext =
                new SqliteTestApplicationDbContext(options);

            await dbContext.Database.EnsureCreatedAsync();

            var userId = Guid.NewGuid();
            var paymentId = Guid.NewGuid();

            var user = new User
            {
                Id = userId,
                UserName = "test-user",
                NormalizedUserName = "TEST-USER"
            };

            // موجودی فعلی کیف پول بعد از برداشت اولیه
            var wallet = new UserWallet
            {
                UserId = userId,
                Amount = 30000,
                RowVersion = new byte[8]
            };

            var payment = new Payment
            {
                Id = paymentId,
                UserId = userId,
                Amount = 20000,
                WalletAmount = 20000,
                GatewayAmount = 0,
                Type = PaymentType.OrderPayment,
                Method = PaymentMethod.Wallet,
                Status = PaymentStatus.Success,
                Description = "تست بازپرداخت تکراری"
            };

            // تراکنش برداشت اولیه که باید بازپرداخت شود
            var originalTransaction = new TransactionHistory
            {
                Id = Guid.NewGuid(), // شناسه یکتای تراکنش اولیه
                UserId = userId,
                PaymentId = paymentId,
                Amount = 20000,
                Type = TransactionType.Withdrawal,
                Status = TransactionStatus.Success,
                Description = "برداشت اولیه"
            };

            dbContext.Users.Add(user);
            dbContext.Set<UserWallet>().Add(wallet);
            dbContext.Set<Payment>().Add(payment);
            dbContext.Set<TransactionHistory>().Add(originalTransaction);

            await dbContext.SaveChangesAsync();

            var userManager = new Mock<UserManager<User>>(
                Mock.Of<IUserStore<User>>(),
                Options.Create(new IdentityOptions()),
                new PasswordHasher<User>(),
                Array.Empty<IUserValidator<User>>(),
                Array.Empty<IPasswordValidator<User>>(),
                new UpperInvariantLookupNormalizer(),
                new IdentityErrorDescriber(),
                null,
                Mock.Of<ILogger<UserManager<User>>>());

            userManager
                .Setup(x => x.FindByIdAsync(userId.ToString()))
                .ReturnsAsync(user);

            var walletRepository =
                new Repository<UserWallet>(dbContext);

            var transactionRepository =
                new Repository<TransactionHistory>(dbContext);

            var transactionHistoryService =
                new TransactionHistoryService(
                    transactionRepository,
                    userManager.Object);

            var service = new WalletService(
                walletRepository,
                transactionHistoryService,
                dbContext,
                userManager.Object);

            // Act - بازپرداخت اول
            await service.RefundAsync(
                userId,
                paymentId,
                "بازپرداخت اول");

            // Act - بازپرداخت دوم با همان PaymentId
            await service.RefundAsync(
                userId,
                paymentId,
                "بازپرداخت دوم");

            // Assert
            var savedWallet = await dbContext.Set<UserWallet>()
                .AsNoTracking()
                .SingleAsync(x => x.UserId == userId);

            var transactions = await dbContext.Set<TransactionHistory>()
                .AsNoTracking()
                .Where(x => x.PaymentId == paymentId)
                .ToListAsync();

            Assert.Equal(50000, savedWallet.Amount);

            Assert.Equal(2, transactions.Count);

            Assert.Single(
                transactions,
                x => x.Type == TransactionType.Withdrawal);

            var refundTransaction = Assert.Single(
                transactions,
                x => x.Type == TransactionType.Refund);

            Assert.Equal(20000, refundTransaction.Amount);
            Assert.Equal(TransactionStatus.Success, refundTransaction.Status);
            Assert.Equal(userId, refundTransaction.UserId);
        }
        [Fact]
        public async Task RefundAsync_ShouldThrowException_WhenPaymentBelongsToAnotherUser()
        {
            // Arrange
            await using var connection =
                new SqliteConnection("Data Source=:memory:");

            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(connection)
                .Options;

            await using var dbContext =
                new SqliteTestApplicationDbContext(options);

            await dbContext.Database.EnsureCreatedAsync();

            var userId = Guid.NewGuid();
            var ownerId = Guid.NewGuid();
            var paymentId = Guid.NewGuid();

            // کاربری که درخواست بازپرداخت می‌دهد
            var user = new User
            {
                Id = userId,
                UserName = "test-user",
                NormalizedUserName = "TEST-USER"
            };

            // صاحب واقعی پرداخت
            var owner = new User
            {
                Id = ownerId,
                UserName = "payment-owner",
                NormalizedUserName = "PAYMENT-OWNER"
            };

            var userWallet = new UserWallet
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Amount = 10000,
                RowVersion = new byte[8]
            };

            var ownerWallet = new UserWallet
            {
                Id = Guid.NewGuid(),
                UserId = ownerId,
                Amount = 30000,
                RowVersion = new byte[8]
            };

            var payment = new Payment
            {
                Id = paymentId,
                UserId = ownerId,
                Amount = 20000,
                WalletAmount = 20000,
                GatewayAmount = 0,
                Type = PaymentType.OrderPayment,
                Method = PaymentMethod.Wallet,
                Status = PaymentStatus.Success,
                Description = "پرداخت متعلق به کاربر دیگر"
            };

            var originalTransaction = new TransactionHistory
            {
                Id = Guid.NewGuid(),
                UserId = ownerId,
                PaymentId = paymentId,
                Amount = 20000,
                Type = TransactionType.Withdrawal,
                Status = TransactionStatus.Success,
                Description = "برداشت اولیه"
            };

            dbContext.Users.AddRange(user, owner);

            dbContext.Set<UserWallet>().AddRange(
                userWallet,
                ownerWallet);

            dbContext.Set<Payment>().Add(payment);
            dbContext.Set<TransactionHistory>().Add(originalTransaction);

            await dbContext.SaveChangesAsync();

            var userManager = new Mock<UserManager<User>>(
                Mock.Of<IUserStore<User>>(),
                Options.Create(new IdentityOptions()),
                new PasswordHasher<User>(),
                Array.Empty<IUserValidator<User>>(),
                Array.Empty<IPasswordValidator<User>>(),
                new UpperInvariantLookupNormalizer(),
                new IdentityErrorDescriber(),
                null,
                Mock.Of<ILogger<UserManager<User>>>());

            userManager
                .Setup(x => x.FindByIdAsync(userId.ToString()))
                .ReturnsAsync(user);

            var walletRepository =
                new Repository<UserWallet>(dbContext);

            var transactionRepository =
                new Repository<TransactionHistory>(dbContext);

            var transactionHistoryService =
                new TransactionHistoryService(
                    transactionRepository,
                    userManager.Object);

            var service = new WalletService(
                walletRepository,
                transactionHistoryService,
                dbContext,
                userManager.Object);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await service.RefundAsync(
                    userId,
                    paymentId,
                    "تلاش برای بازپرداخت پرداخت کاربر دیگر"));

            // بررسی موجودی هر دو کاربر
            var savedUserWallet = await dbContext.Set<UserWallet>()
                .AsNoTracking()
                .SingleAsync(x => x.UserId == userId);

            var savedOwnerWallet = await dbContext.Set<UserWallet>()
                .AsNoTracking()
                .SingleAsync(x => x.UserId == ownerId);

            // بررسی تراکنش‌ها
            var transactions = await dbContext.Set<TransactionHistory>()
                .AsNoTracking()
                .Where(x => x.PaymentId == paymentId)
                .ToListAsync();

            Assert.Equal(10000, savedUserWallet.Amount);
            Assert.Equal(30000, savedOwnerWallet.Amount);

            Assert.Single(transactions);

            Assert.Equal(
                TransactionType.Withdrawal,
                transactions[0].Type);

            Assert.Equal(
                TransactionStatus.Success,
                transactions[0].Status);

            Assert.Equal(ownerId, transactions[0].UserId);
        }

        [Fact]
        public async Task RefundAsync_ShouldThrowException_WhenPaymentTypeIsWalletCharge()
        {
            // Arrange
            await using var connection =
                new SqliteConnection("Data Source=:memory:");

            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(connection)
                .Options;

            await using var dbContext =
                new SqliteTestApplicationDbContext(options);

            await dbContext.Database.EnsureCreatedAsync();

            var userId = Guid.NewGuid();
            var paymentId = Guid.NewGuid();

            var user = new User
            {
                Id = userId,
                UserName = "test-user",
                NormalizedUserName = "TEST-USER"
            };

            var wallet = new UserWallet
            {
                UserId = userId,
                Amount = 30000,
                RowVersion = new byte[8]
            };

            var payment = new Payment
            {
                Id = paymentId,
                UserId = userId,
                Amount = 20000,
                WalletAmount = 0,
                GatewayAmount = 20000,
                Type = PaymentType.WalletCharge,
                Method = PaymentMethod.Gateway,
                Status = PaymentStatus.Success,
                Description = "تست بازپرداخت شارژ کیف پول"
            };

            dbContext.Users.Add(user);
            dbContext.Set<UserWallet>().Add(wallet);
            dbContext.Set<Payment>().Add(payment);

            await dbContext.SaveChangesAsync();

            var userManager = new Mock<UserManager<User>>(
                Mock.Of<IUserStore<User>>(),
                Options.Create(new IdentityOptions()),
                new PasswordHasher<User>(),
                Array.Empty<IUserValidator<User>>(),
                Array.Empty<IPasswordValidator<User>>(),
                new UpperInvariantLookupNormalizer(),
                new IdentityErrorDescriber(),
                null,
                Mock.Of<ILogger<UserManager<User>>>());

            userManager
                .Setup(x => x.FindByIdAsync(userId.ToString()))
                .ReturnsAsync(user);

            var walletRepository =
                new Repository<UserWallet>(dbContext);

            var transactionRepository =
                new Repository<TransactionHistory>(dbContext);

            var transactionHistoryService =
                new TransactionHistoryService(
                    transactionRepository,
                    userManager.Object);

            var service = new WalletService(
                walletRepository,
                transactionHistoryService,
                dbContext,
                userManager.Object);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await service.RefundAsync(
                    userId,
                    paymentId,
                    "تلاش برای بازپرداخت WalletCharge"));

            // Assert
            var savedWallet = await dbContext.Set<UserWallet>()
                .AsNoTracking()
                .SingleAsync(x => x.UserId == userId);

            var transactions = await dbContext.Set<TransactionHistory>()
                .AsNoTracking()
                .Where(x => x.PaymentId == paymentId)
                .ToListAsync();

            Assert.Equal(30000, savedWallet.Amount);
            Assert.Empty(transactions);
        }

        [Fact]
        public async Task RefundAsync_ShouldRefundOnlyWalletAmount_WhenPaymentMethodIsCombined()
        {
            // Arrange
            await using var connection =
                new SqliteConnection("Data Source=:memory:");

            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(connection)
                .Options;

            await using var dbContext =
                new SqliteTestApplicationDbContext(options);

            await dbContext.Database.EnsureCreatedAsync();

            var userId = Guid.NewGuid();
            var paymentId = Guid.NewGuid();

            var user = new User
            {
                Id = userId,
                UserName = "test-user",
                NormalizedUserName = "TEST-USER"
            };

            var wallet = new UserWallet
            {
                UserId = userId,
                Amount = 30000,
                RowVersion = new byte[8]
            };

            var payment = new Payment
            {
                Id = paymentId,
                UserId = userId,
                Amount = 50000,
                WalletAmount = 20000,
                GatewayAmount = 30000,
                Type = PaymentType.OrderPayment,
                Method = PaymentMethod.Combined,
                Status = PaymentStatus.Success,
                Description = "تست بازپرداخت پرداخت ترکیبی"
            };

            var originalTransaction = new TransactionHistory
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                PaymentId = paymentId,
                Amount = 20000,
                Type = TransactionType.Withdrawal,
                Status = TransactionStatus.Success,
                Description = "برداشت سهم کیف پول"
            };

            dbContext.Users.Add(user);
            dbContext.Set<UserWallet>().Add(wallet);
            dbContext.Set<Payment>().Add(payment);
            dbContext.Set<TransactionHistory>().Add(originalTransaction);

            await dbContext.SaveChangesAsync();

            var userManager = new Mock<UserManager<User>>(
                Mock.Of<IUserStore<User>>(),
                Options.Create(new IdentityOptions()),
                new PasswordHasher<User>(),
                Array.Empty<IUserValidator<User>>(),
                Array.Empty<IPasswordValidator<User>>(),
                new UpperInvariantLookupNormalizer(),
                new IdentityErrorDescriber(),
                null,
                Mock.Of<ILogger<UserManager<User>>>());

            userManager
                .Setup(x => x.FindByIdAsync(userId.ToString()))
                .ReturnsAsync(user);

            var walletRepository =
                new Repository<UserWallet>(dbContext);

            var transactionRepository =
                new Repository<TransactionHistory>(dbContext);

            var transactionHistoryService =
                new TransactionHistoryService(
                    transactionRepository,
                    userManager.Object);

            var service = new WalletService(
                walletRepository,
                transactionHistoryService,
                dbContext,
                userManager.Object);

            // Act
            await service.RefundAsync(
                userId,
                paymentId,
                "بازپرداخت پرداخت ترکیبی");

            // Assert
            var savedWallet = await dbContext.Set<UserWallet>()
                .AsNoTracking()
                .SingleAsync(x => x.UserId == userId);

            var transactions = await dbContext.Set<TransactionHistory>()
                .AsNoTracking()
                .Where(x => x.PaymentId == paymentId)
                .ToListAsync();

            Assert.Equal(50000, savedWallet.Amount);

            Assert.Equal(2, transactions.Count);

            Assert.Single(
                transactions.Where(x =>
                    x.Type == TransactionType.Withdrawal));

            var refundTransaction = Assert.Single(
                transactions.Where(x =>
                    x.Type == TransactionType.Refund));

            Assert.Equal(20000, refundTransaction.Amount);

            Assert.Equal(
                TransactionStatus.Success,
                refundTransaction.Status);

            Assert.Equal(userId, refundTransaction.UserId);
        }

        [Fact]
        public async Task RefundAsync_ShouldDecreaseBalanceAndSaveTransaction_WhenWalletChargeIsValid()
        {
            // Arrange
            await using var connection =
                new SqliteConnection("Data Source=:memory:");

            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(connection)
                .Options;

            await using var dbContext =
                new SqliteTestApplicationDbContext(options);

            await dbContext.Database.EnsureCreatedAsync();

            var userId = Guid.NewGuid();
            var paymentId = Guid.NewGuid();

            var user = new User
            {
                Id = userId,
                UserName = "test-user",
                NormalizedUserName = "TEST-USER"
            };

            var wallet = new UserWallet
            {
                UserId = userId,
                Amount = 50000,
                RowVersion = new byte[8]
            };

            var payment = new Payment
            {
                Id = paymentId,
                UserId = userId,
                Amount = 20000,
                WalletAmount = 0,
                GatewayAmount = 20000,
                Type = PaymentType.WalletCharge,
                Method = PaymentMethod.Gateway,
                Status = PaymentStatus.Success,
                Description = "تست بازپرداخت شارژ کیف پول"
            };

            var originalTransaction = new TransactionHistory
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                PaymentId = paymentId,
                Amount = 20000,
                Type = TransactionType.Deposit,
                Status = TransactionStatus.Success,
                Description = "شارژ اولیه کیف پول"
            };

            dbContext.Users.Add(user);
            dbContext.Set<UserWallet>().Add(wallet);
            dbContext.Set<Payment>().Add(payment);
            dbContext.Set<TransactionHistory>().Add(originalTransaction);

            await dbContext.SaveChangesAsync();

            var userManager = new Mock<UserManager<User>>(
                Mock.Of<IUserStore<User>>(),
                Options.Create(new IdentityOptions()),
                new PasswordHasher<User>(),
                Array.Empty<IUserValidator<User>>(),
                Array.Empty<IPasswordValidator<User>>(),
                new UpperInvariantLookupNormalizer(),
                new IdentityErrorDescriber(),
                null,
                Mock.Of<ILogger<UserManager<User>>>());

            userManager
                .Setup(x => x.FindByIdAsync(userId.ToString()))
                .ReturnsAsync(user);

            var walletRepository =
                new Repository<UserWallet>(dbContext);

            var transactionRepository =
                new Repository<TransactionHistory>(dbContext);

            var transactionHistoryService =
                new TransactionHistoryService(
                    transactionRepository,
                    userManager.Object);

            var service = new WalletService(
                walletRepository,
                transactionHistoryService,
                dbContext,
                userManager.Object);

            // Act
            await service.RefundAsync(
                userId,
                paymentId,
                "بازپرداخت شارژ کیف پول");

            // Assert
            var savedWallet = await dbContext.Set<UserWallet>()
                .AsNoTracking()
                .SingleAsync(x => x.UserId == userId);

            var transactions = await dbContext.Set<TransactionHistory>()
                .AsNoTracking()
                .Where(x => x.PaymentId == paymentId)
                .ToListAsync();

            Assert.Equal(30000, savedWallet.Amount);

            Assert.Equal(2, transactions.Count);

            Assert.Single(
                transactions.Where(x =>
                    x.Type == TransactionType.Deposit));

            var refundTransaction = Assert.Single(
                transactions.Where(x =>
                    x.Type == TransactionType.Refund));

            Assert.Equal(20000, refundTransaction.Amount);

            Assert.Equal(
                TransactionStatus.Success,
                refundTransaction.Status);

            Assert.Equal(userId, refundTransaction.UserId);
        }

        [Fact]
        public async Task RefundAsync_ShouldThrowException_WhenWalletBalanceIsInsufficientForWalletCharge()
        {
            // Arrange
            await using var connection =
                new SqliteConnection("Data Source=:memory:");

            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(connection)
                .Options;

            await using var dbContext =
                new SqliteTestApplicationDbContext(options);

            await dbContext.Database.EnsureCreatedAsync();

            var userId = Guid.NewGuid();
            var paymentId = Guid.NewGuid();

            var user = new User
            {
                Id = userId,
                UserName = "test-user",
                NormalizedUserName = "TEST-USER"
            };

            var wallet = new UserWallet
            {
                UserId = userId,
                Amount = 10000,
                RowVersion = new byte[8]
            };

            var payment = new Payment
            {
                Id = paymentId,
                UserId = userId,
                Amount = 20000,
                WalletAmount = 0,
                GatewayAmount = 20000,
                Type = PaymentType.WalletCharge,
                Method = PaymentMethod.Gateway,
                Status = PaymentStatus.Success,
                Description = "تست موجودی ناکافی برای بازپرداخت شارژ"
            };

            var originalTransaction = new TransactionHistory
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                PaymentId = paymentId,
                Amount = 20000,
                Type = TransactionType.Deposit,
                Status = TransactionStatus.Success,
                Description = "شارژ اولیه کیف پول"
            };

            dbContext.Users.Add(user);
            dbContext.Set<UserWallet>().Add(wallet);
            dbContext.Set<Payment>().Add(payment);
            dbContext.Set<TransactionHistory>().Add(originalTransaction);

            await dbContext.SaveChangesAsync();

            var userManager = new Mock<UserManager<User>>(
                Mock.Of<IUserStore<User>>(),
                Options.Create(new IdentityOptions()),
                new PasswordHasher<User>(),
                Array.Empty<IUserValidator<User>>(),
                Array.Empty<IPasswordValidator<User>>(),
                new UpperInvariantLookupNormalizer(),
                new IdentityErrorDescriber(),
                null,
                Mock.Of<ILogger<UserManager<User>>>());

            userManager
                .Setup(x => x.FindByIdAsync(userId.ToString()))
                .ReturnsAsync(user);

            var walletRepository =
                new Repository<UserWallet>(dbContext);

            var transactionRepository =
                new Repository<TransactionHistory>(dbContext);

            var transactionHistoryService =
                new TransactionHistoryService(
                    transactionRepository,
                    userManager.Object);

            var service = new WalletService(
                walletRepository,
                transactionHistoryService,
                dbContext,
                userManager.Object);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await service.RefundAsync(
                    userId,
                    paymentId,
                    "تلاش برای بازپرداخت با موجودی ناکافی"));

            // Assert
            var savedWallet = await dbContext.Set<UserWallet>()
                .AsNoTracking()
                .SingleAsync(x => x.UserId == userId);

            var transactions = await dbContext.Set<TransactionHistory>()
                .AsNoTracking()
                .Where(x => x.PaymentId == paymentId)
                .ToListAsync();

            Assert.Equal(10000, savedWallet.Amount);

            Assert.Single(transactions);

            Assert.Equal(
                TransactionType.Deposit,
                transactions[0].Type);

            Assert.Equal(
                TransactionStatus.Success,
                transactions[0].Status);
        }

        [Fact]
        public async Task RefundAsync_ShouldThrowException_WhenExistingRefundAmountDoesNotMatch()
        {
            // Arrange
            await using var connection =
                new SqliteConnection("Data Source=:memory:");

            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(connection)
                .Options;

            await using var dbContext =
                new SqliteTestApplicationDbContext(options);

            await dbContext.Database.EnsureCreatedAsync();

            var userId = Guid.NewGuid();
            var paymentId = Guid.NewGuid();

            var user = new User
            {
                Id = userId,
                UserName = "test-user",
                NormalizedUserName = "TEST-USER"
            };

            var wallet = new UserWallet
            {
                UserId = userId,
                Amount = 30000,
                RowVersion = new byte[8]
            };

            var payment = new Payment
            {
                Id = paymentId,
                UserId = userId,
                Amount = 20000,
                WalletAmount = 20000,
                GatewayAmount = 0,
                Type = PaymentType.OrderPayment,
                Method = PaymentMethod.Wallet,
                Status = PaymentStatus.Success,
                Description = "تست مغایرت مبلغ بازپرداخت"
            };

            var originalTransaction = new TransactionHistory
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                PaymentId = paymentId,
                Amount = 20000,
                Type = TransactionType.Withdrawal,
                Status = TransactionStatus.Success,
                Description = "برداشت اولیه"
            };

            // تراکنش بازپرداخت قبلی با مبلغ اشتباه
            var existingRefund = new TransactionHistory
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                PaymentId = paymentId,
                Amount = 15000,
                Type = TransactionType.Refund,
                Status = TransactionStatus.Success,
                Description = "بازپرداخت قبلی با مبلغ اشتباه"
            };

            dbContext.Users.Add(user);
            dbContext.Set<UserWallet>().Add(wallet);
            dbContext.Set<Payment>().Add(payment);

            dbContext.Set<TransactionHistory>().AddRange(
                originalTransaction,
                existingRefund);

            await dbContext.SaveChangesAsync();

            var userManager = new Mock<UserManager<User>>(
                Mock.Of<IUserStore<User>>(),
                Options.Create(new IdentityOptions()),
                new PasswordHasher<User>(),
                Array.Empty<IUserValidator<User>>(),
                Array.Empty<IPasswordValidator<User>>(),
                new UpperInvariantLookupNormalizer(),
                new IdentityErrorDescriber(),
                null,
                Mock.Of<ILogger<UserManager<User>>>());

            userManager
                .Setup(x => x.FindByIdAsync(userId.ToString()))
                .ReturnsAsync(user);

            var walletRepository =
                new Repository<UserWallet>(dbContext);

            var transactionRepository =
                new Repository<TransactionHistory>(dbContext);

            var transactionHistoryService =
                new TransactionHistoryService(
                    transactionRepository,
                    userManager.Object);

            var service = new WalletService(
                walletRepository,
                transactionHistoryService,
                dbContext,
                userManager.Object);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await service.RefundAsync(
                    userId,
                    paymentId,
                    "تلاش برای بازپرداخت مجدد"));

            // بررسی موجودی کیف پول
            var savedWallet = await dbContext.Set<UserWallet>()
                .AsNoTracking()
                .SingleAsync(x => x.UserId == userId);

            // بررسی تعداد و مبالغ تراکنش‌ها
            var transactions = await dbContext.Set<TransactionHistory>()
                .AsNoTracking()
                .Where(x => x.PaymentId == paymentId)
                .ToListAsync();

            Assert.Equal(30000, savedWallet.Amount);

            Assert.Equal(2, transactions.Count);

            Assert.Single(
                transactions.Where(x =>
                    x.Type == TransactionType.Withdrawal));

            var savedRefund = Assert.Single(
                transactions.Where(x =>
                    x.Type == TransactionType.Refund));

            Assert.Equal(15000, savedRefund.Amount);

            Assert.Equal(
                TransactionStatus.Success,
                savedRefund.Status);
        }



        // 2. کلاس کمکی در انتهای کلاس اصلی قرار می‌گیرد.


        private sealed class SqliteTestApplicationDbContext
            : ApplicationDbContext
        {
            public SqliteTestApplicationDbContext(
                DbContextOptions<ApplicationDbContext> options)
                : base(options)
            {
            }

            protected override void OnModelCreating(
                ModelBuilder modelBuilder)
            {
                base.OnModelCreating(modelBuilder);

                foreach (var entityType in modelBuilder.Model.GetEntityTypes())
                {
                    var idProperty = entityType.FindProperty("Id");

                    if (idProperty != null &&
                        idProperty.ClrType == typeof(Guid))
                    {
                        idProperty.SetDefaultValueSql(null);
                        idProperty.ValueGenerated = ValueGenerated.Never;
                    }
                }

                modelBuilder.Entity<UserWallet>()
                    .Property(x => x.RowVersion)
                    .HasDefaultValueSql("randomblob(8)")
                    .ValueGeneratedOnAdd()
                    .IsConcurrencyToken();
            }
        }
    }
}