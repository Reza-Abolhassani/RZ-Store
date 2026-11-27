using Data;
using Entities;
using Entities.Payments;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Services;
using Services.Interfaces.Payments;
using Services.Payments;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Tests
{
    public class PaymentServiceTests
    {
        [Fact]
        public async Task CreateOrderPaymentAsync_ShouldCreatePaymentSuccessfully_WhenDataIsValid()
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
            var orderId = Guid.NewGuid();

            var user = new User
            {
                Id = userId,
                UserName = "testuser",
                NormalizedUserName = "TESTUSER",
                Email = "test@example.com",
                PhoneNumber = "09120000000"
            };

            var order = new Order
            {
                Id = orderId,
                UserId = userId,
                User = user,
                Code = "RZ-TEST-001",
                IsPaid = false,
                Status = OrderStatus.NotPayed,
                Total = 125000,
                RawPrice = 125000,
                Discount = 0,
                Shipping = "Test Shipping",
                ShippingPrice = 0,
                ReceiverName = "Test User",
                ReceiverPhone = "09120000000",
                Province = "Tehran",
                City = "Tehran",
                Address = "Test Address",
                PostalCode = "1234567890"
            };

            await dbContext.Set<User>().AddAsync(user);
            await dbContext.Set<Order>().AddAsync(order);

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

            var paymentGateway = new Mock<IPaymentGateway>();

            const string authority =
                "A00000000000000000000000000000000000";

            const string paymentUrl =
                "https://sandbox.zarinpal.com/pg/StartPay/A00000000000000000000000000000000000";

            paymentGateway
                .Setup(x => x.RequestPaymentAsync(
                    It.IsAny<GatewayPaymentRequest>()))
                .ReturnsAsync(new GatewayPaymentRequestResult
                {
                    IsSuccess = true,
                    Authority = authority,
                    PaymentUrl = paymentUrl
                });

            var paymentService = new PaymentService(
                dbContext,
                paymentGateway.Object,
                userManager.Object);

            const string callbackUrl =
                "https://example.com/payment/callback";

            // Act
            var result = await paymentService.CreateOrderPaymentAsync(
                orderId,
                userId,
                callbackUrl);

            // Assert - نتیجه عملیات
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.PaymentId);
            Assert.Equal(paymentUrl, result.PaymentUrl);

            // Assert - اطلاعات پرداخت ذخیره‌شده
            var payment = await dbContext.Set<Payment>()
                .Include(x => x.PaymentAttempts)
                .SingleAsync(x => x.Id == result.PaymentId);

            Assert.Equal(userId, payment.UserId);
            Assert.Equal(orderId, payment.OrderId);
            Assert.Equal(125000, payment.Amount);
            Assert.Equal(0, payment.WalletAmount);
            Assert.Equal(125000, payment.GatewayAmount);
            Assert.Equal(PaymentType.OrderPayment, payment.Type);
            Assert.Equal(PaymentMethod.Gateway, payment.Method);
            Assert.Equal(PaymentStatus.Pending, payment.Status);

            // Assert - اطلاعات تلاش پرداخت
            var attempt = Assert.Single(payment.PaymentAttempts);

            Assert.Equal("ZarinPal", attempt.GatewayName);
            Assert.Equal(125000, attempt.Amount);
            Assert.Equal(PaymentAttemptStatus.Pending, attempt.Status);
            Assert.Equal(authority, attempt.GatewayRequestId);
            Assert.Equal(paymentUrl, attempt.PaymentUrl);

            // Assert - بررسی درخواست ارسالی به درگاه
            paymentGateway.Verify(
                x => x.RequestPaymentAsync(
                    It.Is<GatewayPaymentRequest>(request =>
                        request.Amount == 125000 &&
                        request.CallbackUrl == callbackUrl &&
                        request.Description == "پرداخت سفارش RZ-TEST-001" &&
                        request.Mobile == user.PhoneNumber &&
                        request.Email == user.Email)),
                Times.Once);
        }

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