using Admin.Models;
using Data.Repositories;
using Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace Admin.Controllers
{
    public class ReportsController : Controller
    {
        private readonly IRepository<Order> _orderRepo;
        private readonly IRepository<OrderItem> _orderItemRepo;

        public ReportsController(
            IRepository<Order> orderRepo,
            IRepository<OrderItem> orderItemRepo)
        {
            _orderRepo = orderRepo;
            _orderItemRepo = orderItemRepo;
        }

        public async Task<IActionResult> Index()
        {
            // 1. دریافت سفارش‌ها به همراه اطلاعات مشتری
            var orders = await _orderRepo.TableNoTracking
                .Include(x => x.User)
                .OrderByDescending(x => x.CreationDateTime)
                .ToListAsync();

            // 2. محاسبه آمار سفارش‌ها
            var paidOrders = orders
                .Where(x => x.IsPaid)
                .ToList();

            var model = new ReportDto
            {
                TotalOrders = orders.Count,

                PaidOrders = paidOrders.Count,

                UnpaidOrders = orders.Count(x => !x.IsPaid),

                TotalSales = paidOrders.Sum(x => (long)x.Total),

                TotalDiscount = paidOrders.Sum(x => (long)x.Discount),

                // 3. نمایش 10 سفارش آخر
                RecentOrders = orders
                    .Take(10)
                    .Select(x => new RecentOrderDto
                    {
                        Id = x.Id,
                        Code = x.Code,

                        CustomerName = x.User == null
                            ? "نامشخص"
                            : (x.User.FirstName + " " + x.User.LastName),

                        Total = x.Total,
                        IsPaid = x.IsPaid,
                        CreationDateTime = x.CreationDateTime
                    })
                    .ToList()
            };

            // 4. دریافت اقلام سفارش‌های پرداخت‌شده
            var orderItems = await _orderItemRepo.TableNoTracking
                .Include(x => x.Order)
                .Include(x => x.Product)
                .Where(x => x.Order != null && x.Order.IsPaid)
                .ToListAsync();

            // 5. محاسبه 10 محصول پرفروش
            model.TopProducts = orderItems
                .GroupBy(x => new
                {
                    x.ProductId,

                    ProductName = x.Product == null
                        ? "محصول حذف‌شده"
                        : x.Product.Title
                })
                .Select(g => new TopProductDto
                {
                    ProductId = g.Key.ProductId,

                    ProductName = g.Key.ProductName,

                    Count = g.Sum(x => x.Count),

                    TotalSales = g.Sum(x => (long)x.Total)
                })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToList();

            return View(model);
        }


        public async Task<IActionResult> Sales()
        {
            // 1. دریافت سفارش‌ها به همراه مشتری
            var orders = await _orderRepo.TableNoTracking
                .Include(x => x.User)
                .OrderByDescending(x => x.CreationDateTime)
                .ToListAsync();

            // 2. دریافت سفارش‌های پرداخت‌شده
            var paidOrders = orders
                .Where(x => x.IsPaid)
                .ToList();

            // 3. آماده‌سازی اطلاعات گزارش فروش
            var model = new ReportDto
            {
                TotalOrders = orders.Count,

                PaidOrders = paidOrders.Count,

                UnpaidOrders = orders.Count(x => !x.IsPaid),

                TotalSales = paidOrders.Sum(x => (long)x.Total),

                TotalDiscount = paidOrders.Sum(x => (long)x.Discount),

                // 4. نمایش 10 فروش آخر
                RecentOrders = paidOrders
                    .OrderByDescending(x => x.CreationDateTime)
                    .Take(10)
                    .Select(x => new RecentOrderDto
                    {
                        Id = x.Id,
                        Code = x.Code,

                        CustomerName = x.User == null
                            ? "نامشخص"
                            : (x.User.FirstName + " " + x.User.LastName),

                        Total = x.Total,
                        IsPaid = x.IsPaid,
                        CreationDateTime = x.CreationDateTime
                    })
                    .ToList()
            };

            return View(model);
        }
    }
}
