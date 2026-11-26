using Data;
using Data.Repositories;
using Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Services;
using Services.Interfaces;
using System;
using System.Threading.Tasks;
using Xunit;
using Microsoft.Data.Sqlite;

namespace Tests
{
    public class WalletServiceTests
    {
        [Fact]
        public async Task DepositAsync_ShouldThrowException_WhenAmountIsZero()
        {
            // Arrange
            var walletRepository = new Mock<IRepository<UserWallet>>();
            var transactionHistoryService = new Mock<ITransactionHistoryService>();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            using var dbContext = new ApplicationDbContext(options);

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

            var service = new WalletService(
                walletRepository.Object,
                transactionHistoryService.Object,
                dbContext,
                userManager.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => service.DepositAsync(
                    Guid.NewGuid(),
                    0,
                    "تست شارژ کیف پول"));

            transactionHistoryService.Verify(
                x => x.AddTransactionAsync(
                    It.IsAny<TransactionHistory>(),
                    It.IsAny<bool>()),
                Times.Never);
        }

        [Fact]
        public async Task DepositAsync_ShouldThrowException_WhenAmountIsNegative()
        {
            // Arrange
            var walletRepository = new Mock<IRepository<UserWallet>>();
            var transactionHistoryService = new Mock<ITransactionHistoryService>();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            using var dbContext = new ApplicationDbContext(options);

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

            var service = new WalletService(
                walletRepository.Object,
                transactionHistoryService.Object,
                dbContext,
                userManager.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => service.DepositAsync(
                    Guid.NewGuid(),
                    -1000,
                    "تست شارژ کیف پول"));

            transactionHistoryService.Verify(
                x => x.AddTransactionAsync(
                    It.IsAny<TransactionHistory>(),
                    It.IsAny<bool>()),
                Times.Never);
        }

        [Fact]
        public async Task DepositAsync_ShouldThrowException_WhenUserIdIsEmpty()
        {
            // Arrange
            var walletRepository = new Mock<IRepository<UserWallet>>();
            var transactionHistoryService = new Mock<ITransactionHistoryService>();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            using var dbContext = new ApplicationDbContext(options);

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

            var service = new WalletService(
                walletRepository.Object,
                transactionHistoryService.Object,
                dbContext,
                userManager.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => service.DepositAsync(
                    Guid.Empty,
                    10000,
                    "تست شارژ کیف پول"));

            transactionHistoryService.Verify(
                x => x.AddTransactionAsync(
                    It.IsAny<TransactionHistory>(),
                    It.IsAny<bool>()),
                Times.Never);
        }

        [Fact]
        public async Task DepositAsync_ShouldThrowException_WhenActiveWalletDoesNotExist()
        {
            // Arrange
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();

            try
            {
                var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseSqlite(connection)
                    .Options;

                await using var dbContext =
                    new SqliteTestApplicationDbContext(options);

                await dbContext.Database.EnsureCreatedAsync();

                var walletRepository =
                    new Repository<UserWallet>(dbContext);

                var transactionHistoryService =
                    new Mock<ITransactionHistoryService>();

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

                var service = new WalletService(
                    walletRepository,
                    transactionHistoryService.Object,
                    dbContext,
                    userManager.Object);

                var userId = Guid.NewGuid();

                // Act & Assert
                var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                    () => service.DepositAsync(
                        userId,
                        10000,
                        "تست نبود کیف پول"));

                Assert.Equal(
                    "کیف پول فعال کاربر وجود ندارد.",
                    exception.Message);

                transactionHistoryService.Verify(
                    x => x.AddTransactionAsync(
                        It.IsAny<TransactionHistory>(),
                        It.IsAny<bool>()),
                    Times.Never);
            }
            finally
            {
                await connection.DisposeAsync();
            }
        }

        [Fact]
        public async Task DepositAsync_ShouldIncreaseBalanceAndSaveTransaction_WhenDataIsValid()
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
                "تست شارژ موفق");

            // Assert
            var savedWallet = await dbContext.Set<UserWallet>()
                .AsNoTracking()
                .SingleAsync(x => x.UserId == userId);

            var savedTransaction = await dbContext.Set<TransactionHistory>()
                .AsNoTracking()
                .SingleAsync(x => x.UserId == userId);

            Assert.Equal(70000, savedWallet.Amount);
            Assert.Equal(20000, savedTransaction.Amount);
            Assert.Equal(TransactionType.Deposit, savedTransaction.Type);
            Assert.Equal(TransactionStatus.Success, savedTransaction.Status);
            Assert.Equal("تست شارژ موفق", savedTransaction.Description);
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
                "تست برداشت موفق");

            // Assert
            var savedWallet = await dbContext.Set<UserWallet>()
                .AsNoTracking()
                .SingleAsync(x => x.UserId == userId);

            var savedTransaction = await dbContext.Set<TransactionHistory>()
                .AsNoTracking()
                .SingleAsync(x => x.UserId == userId);

            Assert.Equal(30000, savedWallet.Amount);

            Assert.Equal(20000, savedTransaction.Amount);
            Assert.Equal(TransactionType.Withdrawal, savedTransaction.Type);
            Assert.Equal(TransactionStatus.Success, savedTransaction.Status);
            Assert.Equal("تست برداشت موفق", savedTransaction.Description);
        }

        [Fact]
        public async Task WithdrawAsync_ShouldThrowException_WhenBalanceIsInsufficient()
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
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.WithdrawAsync(
                    userId,
                    70000,
                    "تست موجودی ناکافی"));

            // Assert
            Assert.Equal("موجودی کیف پول کافی نیست.", exception.Message);

            var savedWallet = await dbContext.Set<UserWallet>()
                .AsNoTracking()
                .SingleAsync(x => x.UserId == userId);

            Assert.Equal(50000, savedWallet.Amount);

            var transactionCount = await dbContext.Set<TransactionHistory>()
                .CountAsync(x => x.UserId == userId);

            Assert.Equal(0, transactionCount);
        }

        [Fact]
        public async Task WithdrawAsync_ShouldThrowException_WhenAmountIsZero()
        {
            // Arrange
            var walletRepository = new Mock<IRepository<UserWallet>>();
            var transactionHistoryService = new Mock<ITransactionHistoryService>();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            using var dbContext = new ApplicationDbContext(options);

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

            var service = new WalletService(
                walletRepository.Object,
                transactionHistoryService.Object,
                dbContext,
                userManager.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => service.WithdrawAsync(
                    Guid.NewGuid(),
                    0,
                    "تست برداشت با مبلغ صفر"));

            transactionHistoryService.Verify(
                x => x.AddTransactionAsync(
                    It.IsAny<TransactionHistory>(),
                    It.IsAny<bool>()),
                Times.Never);
        }

        [Fact]
        public async Task WithdrawAsync_ShouldThrowException_WhenAmountIsNegative()
        {
            // Arrange
            var walletRepository = new Mock<IRepository<UserWallet>>();
            var transactionHistoryService = new Mock<ITransactionHistoryService>();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            using var dbContext = new ApplicationDbContext(options);

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

            var service = new WalletService(
                walletRepository.Object,
                transactionHistoryService.Object,
                dbContext,
                userManager.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => service.WithdrawAsync(
                    Guid.NewGuid(),
                    -1000,
                    "تست برداشت با مبلغ منفی"));

            transactionHistoryService.Verify(
                x => x.AddTransactionAsync(
                    It.IsAny<TransactionHistory>(),
                    It.IsAny<bool>()),
                Times.Never);
        }

        [Fact]
        public async Task WithdrawAsync_ShouldThrowException_WhenActiveWalletDoesNotExist()
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

            var walletRepository =
                new Repository<UserWallet>(dbContext);

            var transactionHistoryService =
                new Mock<ITransactionHistoryService>();

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

            var service = new WalletService(
                walletRepository,
                transactionHistoryService.Object,
                dbContext,
                userManager.Object);

            var userId = Guid.NewGuid();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.WithdrawAsync(
                    userId,
                    10000,
                    "تست برداشت بدون کیف پول"));

            Assert.Equal(
                "کیف پول فعال کاربر وجود ندارد.",
                exception.Message);

            transactionHistoryService.Verify(
                x => x.AddTransactionAsync(
                    It.IsAny<TransactionHistory>(),
                    It.IsAny<bool>()),
                Times.Never);
        }


        [Fact]
        public async Task DepositAsync_ShouldThrowException_WhenAmountCausesOverflow()
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

            var user = new User
            {
                Id = userId,
                UserName = "test-user",
                NormalizedUserName = "TEST-USER"
            };

            var wallet = new UserWallet
            {
                UserId = userId,
                Amount = long.MaxValue - 100,
                RowVersion = new byte[8]
            };

            dbContext.Users.Add(user);
            dbContext.Set<UserWallet>().Add(wallet);

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
            await Assert.ThrowsAsync<ArgumentException>(
                () => service.DepositAsync(
                    userId,
                    200,
                    "تست جلوگیری از سرریز مبلغ"));

            var savedWallet = await dbContext.Set<UserWallet>()
                .AsNoTracking()
                .SingleAsync(x => x.UserId == userId);

            Assert.Equal(long.MaxValue - 100, savedWallet.Amount);

            var transactionCount = await dbContext.Set<TransactionHistory>()
                .CountAsync(x => x.UserId == userId);

            Assert.Equal(0, transactionCount);
        }

        [Fact]
        public async Task DepositAsync_ShouldRollbackBalance_WhenTransactionHistoryFails()
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

            var transactionHistoryService =
                new Mock<ITransactionHistoryService>();

            transactionHistoryService
                .Setup(x => x.AddTransactionAsync(
                    It.IsAny<TransactionHistory>(),
                    false))
                .ThrowsAsync(new InvalidOperationException(
                    "خطا در ثبت تاریخچه تراکنش"));

            var walletRepository =
                new Repository<UserWallet>(dbContext);

            var service = new WalletService(
                walletRepository,
                transactionHistoryService.Object,
                dbContext,
                userManager.Object);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.DepositAsync(
                    userId,
                    20000,
                    "تست بازگشت عملیات شارژ"));

            Assert.Equal(
                "خطا در ثبت تاریخچه تراکنش",
                exception.Message);

            // بررسی عدم تغییر موجودی در دیتابیس
            var savedWallet = await dbContext.Set<UserWallet>()
                .AsNoTracking()
                .SingleAsync(x => x.UserId == userId);

            Assert.Equal(50000, savedWallet.Amount);

            // بررسی عدم ثبت تراکنش
            var transactionCount = await dbContext.Set<TransactionHistory>()
                .CountAsync(x => x.UserId == userId);

            Assert.Equal(0, transactionCount);

            transactionHistoryService.Verify(
                x => x.AddTransactionAsync(
                    It.IsAny<TransactionHistory>(),
                    false),
                Times.Once);
        }

        [Fact]
        public async Task DepositAsync_ShouldThrowConcurrencyException_WhenWalletWasUpdatedByAnotherRequest()
        {
            // Arrange
            await using var connection =
                new SqliteConnection("Data Source=:memory:");

            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(connection)
                .Options;

            await using var dbContext1 =
                new SqliteTestApplicationDbContext(options);

            await dbContext1.Database.EnsureCreatedAsync();

            var userId = Guid.NewGuid();

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

            dbContext1.Users.Add(user);
            dbContext1.Set<UserWallet>().Add(wallet);

            await dbContext1.SaveChangesAsync();

            // شبیه‌سازی تغییر خودکار RowVersion در SQLite
            await dbContext1.Database.ExecuteSqlRawAsync(
                @"CREATE TRIGGER UpdateWalletRowVersion
          AFTER UPDATE ON UserWallets
          BEGIN
              UPDATE UserWallets
              SET RowVersion = randomblob(8)
              WHERE Id = NEW.Id;
          END;");

            await using var dbContext2 =
                new SqliteTestApplicationDbContext(options);

            // درخواست دوم، نسخه قدیمی کیف پول را می‌خواند.
            var staleWallet = await dbContext2.Set<UserWallet>()
                .SingleAsync(x => x.UserId == userId);

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

            var historyService1 = new Mock<ITransactionHistoryService>();
            var historyService2 = new Mock<ITransactionHistoryService>();

            var service1 = new WalletService(
                new Repository<UserWallet>(dbContext1),
                historyService1.Object,
                dbContext1,
                userManager.Object);

            var service2 = new WalletService(
                new Repository<UserWallet>(dbContext2),
                historyService2.Object,
                dbContext2,
                userManager.Object);

            // Act
            // درخواست اول با موفقیت موجودی را تغییر می‌دهد.
            await service1.DepositAsync(
                userId,
                1000,
                "شارژ درخواست اول");

            // درخواست دوم با اطلاعات قدیمی تلاش به تغییر موجودی می‌کند.
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => service2.DepositAsync(
                    userId,
                    2000,
                    "شارژ درخواست دوم"));

            // Assert
            Assert.Equal(
                "موجودی کیف پول هم‌زمان تغییر کرده است. لطفاً دوباره تلاش کنید.",
                exception.Message);

            var savedWallet = await dbContext1.Set<UserWallet>()
                .AsNoTracking()
                .SingleAsync(x => x.UserId == userId);

            Assert.Equal(51000, savedWallet.Amount);

            Assert.Equal(
                new byte[8].Length,
                staleWallet.RowVersion.Length);
        }

        [Fact]
        public async Task WithdrawAsync_ShouldThrowConcurrencyException_WhenWalletWasUpdatedByAnotherRequest()
        {
            // Arrange
            await using var connection =
                new SqliteConnection("Data Source=:memory:");

            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(connection)
                .Options;

            await using var dbContext1 =
                new SqliteTestApplicationDbContext(options);

            await dbContext1.Database.EnsureCreatedAsync();

            var userId = Guid.NewGuid();

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

            dbContext1.Users.Add(user);
            dbContext1.Set<UserWallet>().Add(wallet);

            await dbContext1.SaveChangesAsync();

            await dbContext1.Database.ExecuteSqlRawAsync(
                @"CREATE TRIGGER UpdateWalletRowVersion
          AFTER UPDATE ON UserWallets
          BEGIN
              UPDATE UserWallets
              SET RowVersion = randomblob(8)
              WHERE Id = NEW.Id;
          END;");

            await using var dbContext2 =
                new SqliteTestApplicationDbContext(options);

            // درخواست دوم نسخه قدیمی کیف پول را می‌خواند.
            var staleWallet = await dbContext2.Set<UserWallet>()
                .SingleAsync(x => x.UserId == userId);

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

            var historyService1 = new Mock<ITransactionHistoryService>();
            var historyService2 = new Mock<ITransactionHistoryService>();

            var service1 = new WalletService(
                new Repository<UserWallet>(dbContext1),
                historyService1.Object,
                dbContext1,
                userManager.Object);

            var service2 = new WalletService(
                new Repository<UserWallet>(dbContext2),
                historyService2.Object,
                dbContext2,
                userManager.Object);

            // Act
            await service1.WithdrawAsync(
                userId,
                10000,
                "برداشت درخواست اول");

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => service2.WithdrawAsync(
                    userId,
                    15000,
                    "برداشت درخواست دوم"));

            // Assert
            Assert.Equal(
                "موجودی کیف پول هم‌زمان تغییر کرده است. لطفاً دوباره تلاش کنید.",
                exception.Message);

            var savedWallet = await dbContext1.Set<UserWallet>()
                .AsNoTracking()
                .SingleAsync(x => x.UserId == userId);

            Assert.Equal(40000, savedWallet.Amount);

            var transactionCount = await dbContext1.Set<TransactionHistory>()
                .CountAsync(x => x.UserId == userId);

            Assert.Equal(0, transactionCount);
        }

        [Fact]
        public async Task DepositAndWithdrawAsync_ShouldPreventConcurrentStaleUpdate()
        {
            // Arrange
            await using var connection =
                new SqliteConnection("Data Source=:memory:");

            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(connection)
                .Options;

            await using var dbContext1 =
                new SqliteTestApplicationDbContext(options);

            await dbContext1.Database.EnsureCreatedAsync();

            var userId = Guid.NewGuid();

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

            dbContext1.Users.Add(user);
            dbContext1.Set<UserWallet>().Add(wallet);

            await dbContext1.SaveChangesAsync();

            await dbContext1.Database.ExecuteSqlRawAsync(
                @"CREATE TRIGGER UpdateWalletRowVersion
          AFTER UPDATE ON UserWallets
          BEGIN
              UPDATE UserWallets
              SET RowVersion = randomblob(8)
              WHERE Id = NEW.Id;
          END;");

            await using var dbContext2 =
                new SqliteTestApplicationDbContext(options);

            // درخواست برداشت، موجودی قدیمی را می‌خواند.
            var staleWallet = await dbContext2.Set<UserWallet>()
                .SingleAsync(x => x.UserId == userId);

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

            var historyService1 = new Mock<ITransactionHistoryService>();
            var historyService2 = new Mock<ITransactionHistoryService>();

            historyService1
                .Setup(x => x.AddTransactionAsync(
                    It.IsAny<TransactionHistory>(), false))
                .Returns(Task.CompletedTask);

            historyService2
                .Setup(x => x.AddTransactionAsync(
                    It.IsAny<TransactionHistory>(), false))
                .Returns(Task.CompletedTask);

            var depositService = new WalletService(
                new Repository<UserWallet>(dbContext1),
                historyService1.Object,
                dbContext1,
                userManager.Object);

            var withdrawService = new WalletService(
                new Repository<UserWallet>(dbContext2),
                historyService2.Object,
                dbContext2,
                userManager.Object);

            // Act
            // ابتدا شارژ با موفقیت انجام می‌شود.
            await depositService.DepositAsync(
                userId,
                2000,
                "شارژ هم‌زمان");

            // سپس برداشت با نسخه قدیمی تلاش می‌شود.
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => withdrawService.WithdrawAsync(
                    userId,
                    1000,
                    "برداشت هم‌زمان"));

            // Assert
            Assert.Equal(
                "موجودی کیف پول هم‌زمان تغییر کرده است. لطفاً دوباره تلاش کنید.",
                exception.Message);

            var savedWallet = await dbContext1.Set<UserWallet>()
                .AsNoTracking()
                .SingleAsync(x => x.UserId == userId);

            Assert.Equal(52000, savedWallet.Amount);

            var currentWallet = await dbContext1.Set<UserWallet>()
                .AsNoTracking()
                .SingleAsync(x => x.UserId == userId);

            Assert.NotEqual(staleWallet.RowVersion, currentWallet.RowVersion);
        }

        [Fact]
        public async Task CreateWalletAsync_ShouldCreateWalletWithZeroBalance_WhenWalletDoesNotExist()
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

            var user = new User
            {
                Id = userId,
                UserName = "test-user",
                NormalizedUserName = "TEST-USER"
            };

            dbContext.Users.Add(user);
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

            var service = new WalletService(
                new Repository<UserWallet>(dbContext),
                Mock.Of<ITransactionHistoryService>(),
                dbContext,
                userManager.Object);

            // Act
            var createdWallet = await service.CreateWalletAsync(userId);

            // Assert
            Assert.NotNull(createdWallet);
            Assert.Equal(userId, createdWallet.UserId);
            Assert.Equal(0, createdWallet.Amount);

            var savedWallet = await dbContext.Set<UserWallet>()
                .AsNoTracking()
                .SingleAsync(x => x.UserId == userId);

            Assert.Equal(0, savedWallet.Amount);
            Assert.Equal(userId, savedWallet.UserId);
        }


        [Fact]
        public async Task CreateWalletAsync_ShouldReturnExistingWallet_WhenActiveWalletExists()
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

            var user = new User
            {
                Id = userId,
                UserName = "test-user",
                NormalizedUserName = "TEST-USER"
            };

            dbContext.Users.Add(user);

            var existingWallet = new UserWallet
            {
                UserId = userId,
                Amount = 50000,
                Active = true,
                RowVersion = new byte[8]
            };

            dbContext.Set<UserWallet>().Add(existingWallet);

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

            var service = new WalletService(
                new Repository<UserWallet>(dbContext),
                Mock.Of<ITransactionHistoryService>(),
                dbContext,
                userManager.Object);

            // Act
            var result = await service.CreateWalletAsync(userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(existingWallet.Id, result.Id);
            Assert.Equal(userId, result.UserId);
            Assert.Equal(50000, result.Amount);

            var walletCount = await dbContext.Set<UserWallet>()
                .CountAsync(x => x.UserId == userId);

            Assert.Equal(1, walletCount);
        }

        [Fact]
        public async Task CreateWalletAsync_ShouldThrowException_WhenInactiveWalletExists()
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

            var user = new User
            {
                Id = userId,
                UserName = "test-user",
                NormalizedUserName = "TEST-USER"
            };

            dbContext.Users.Add(user);

            var existingWallet = new UserWallet
            {
                UserId = userId,
                Amount = 50000,
                Active = false,
                RowVersion = new byte[8]
            };

            dbContext.Set<UserWallet>().Add(existingWallet);

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

            var service = new WalletService(
                new Repository<UserWallet>(dbContext),
                Mock.Of<ITransactionHistoryService>(),
                dbContext,
                userManager.Object);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.CreateWalletAsync(userId));

            var walletCount = await dbContext.Set<UserWallet>()
                .CountAsync(x => x.UserId == userId);

            Assert.Equal(1, walletCount);
        }

        [Fact]
        public async Task CreateWalletAsync_ShouldThrowException_WhenUserDoesNotExist()
        {
            // Arrange
            var userId = Guid.NewGuid();

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
                .ReturnsAsync((User)null);

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
    .UseInMemoryDatabase(Guid.NewGuid().ToString())
    .Options;

            using var dbContext = new ApplicationDbContext(options);

            var service = new WalletService(
                Mock.Of<IRepository<UserWallet>>(),
                Mock.Of<ITransactionHistoryService>(),
                dbContext,
                userManager.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => service.CreateWalletAsync(userId));
        }

        [Fact]
        public async Task CreateWalletAsync_ShouldThrowException_WhenUserIdIsEmpty()
        {
            // Arrange
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

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            using var dbContext = new ApplicationDbContext(options);

            var service = new WalletService(
                Mock.Of<IRepository<UserWallet>>(),
                Mock.Of<ITransactionHistoryService>(),
                dbContext,
                userManager.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => service.CreateWalletAsync(Guid.Empty));

            userManager.Verify(
                x => x.FindByIdAsync(It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task GetWalletByUserIdAsync_ShouldReturnWallet_WhenActiveWalletExists()
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
                Active = true,
                RowVersion = new byte[8]
            };

            dbContext.Users.Add(user);
            dbContext.Set<UserWallet>().Add(wallet);

            await dbContext.SaveChangesAsync();

            var walletRepository =
                new Repository<UserWallet>(dbContext);

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

            var service = new WalletService(
    walletRepository,
    Mock.Of<ITransactionHistoryService>(),
    dbContext,
    userManager.Object);

            // Act
            var result = await service.GetWalletByUserIdAsync(userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(wallet.Id, result.Id);
            Assert.Equal(userId, result.UserId);
            Assert.Equal(50000, result.Amount);
            Assert.True(result.Active);
        }

        [Fact]
        public async Task GetWalletByUserIdAsync_ShouldReturnNull_WhenWalletDoesNotExist()
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

            var user = new User
            {
                Id = userId,
                UserName = "test-user",
                NormalizedUserName = "TEST-USER"
            };

            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();

            var walletRepository =
                new Repository<UserWallet>(dbContext);

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

            var service = new WalletService(
                walletRepository,
                Mock.Of<ITransactionHistoryService>(),
                dbContext,
                userManager.Object);

            // Act
            var result = await service.GetWalletByUserIdAsync(userId);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetBalanceAsync_ShouldReturnCorrectBalance_WhenWalletExists()
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
                Active = true,
                RowVersion = new byte[8]
            };

            dbContext.Users.Add(user);
            dbContext.Set<UserWallet>().Add(wallet);

            await dbContext.SaveChangesAsync();

            var walletRepository =
                new Repository<UserWallet>(dbContext);

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

            var service = new WalletService(
                walletRepository,
                Mock.Of<ITransactionHistoryService>(),
                dbContext,
                userManager.Object);

            // Act
            var result = await service.GetBalanceAsync(userId);

            // Assert
            Assert.Equal(50000, result);
        }

        [Fact]
        public async Task GetBalanceAsync_ShouldReturnZero_WhenWalletDoesNotExist()
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

            var user = new User
            {
                Id = userId,
                UserName = "test-user",
                NormalizedUserName = "TEST-USER"
            };

            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();

            var walletRepository =
                new Repository<UserWallet>(dbContext);

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

            var service = new WalletService(
                walletRepository,
                Mock.Of<ITransactionHistoryService>(),
                dbContext,
                userManager.Object);

            // Act
            var result = await service.GetBalanceAsync(userId);

            // Assert
            Assert.Equal(0, result);
        }

        [Fact]
        public async Task GetBalanceAsync_ShouldThrowException_WhenUserIdIsEmpty()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            await using var dbContext =
                new ApplicationDbContext(options);

            var walletRepository =
                new Repository<UserWallet>(dbContext);

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

            var service = new WalletService(
                walletRepository,
                Mock.Of<ITransactionHistoryService>(),
                dbContext,
                userManager.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => service.GetBalanceAsync(Guid.Empty));
        }
        // کلاس کمکی مخصوص تست SQLite
        private sealed class SqliteTestApplicationDbContext
            : ApplicationDbContext
        {
            public SqliteTestApplicationDbContext(
                DbContextOptions<ApplicationDbContext> options)
                : base(options)
            {
            }

            protected override void OnModelCreating(ModelBuilder modelBuilder)
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