using AutoMapper;
using AutoMapper.QueryableExtensions;
using Client.Models;
using Data;
using Data.Repositories;
using Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Client.Controllers
{
    public class OrderController : Controller
    {
        private readonly IRepository<Order> OrderRepo;
        private readonly UserManager<User> userManager;
        private readonly IMapper mapper;
        private readonly IRepository<OrderItem> OrderItemRepo;
        private readonly ApplicationDbContext _context;

        public OrderController(
            IRepository<Order> orderRepo,
            UserManager<User> userManager,
            IMapper mapper,
            IRepository<OrderItem> orderItemRepo,
            ApplicationDbContext context)
        {
            OrderRepo = orderRepo;
            this.userManager = userManager;
            this.mapper = mapper;
            OrderItemRepo = orderItemRepo;
            _context = context;
        }

        // نمایش سفارش‌های مشتری
        public async Task<IActionResult> Index()
        {
            if (User.Identity?.IsAuthenticated != true)
                return RedirectToAction("Login", "ClientLogin");

            var user = await userManager.GetUserAsync(User);

            if (user == null)
                return RedirectToAction("Login", "ClientLogin");

            var orders = await OrderRepo.TableNoTracking
                .Where(x => x.UserId == user.Id)
                .OrderByDescending(x => x.CreationDateTime)
                .ProjectTo<OrderDto>(
                    mapper.ConfigurationProvider)
                .ToListAsync();

            return View(orders);
        }

        // نمایش جزئیات سفارش
        public async Task<IActionResult> Details(Guid id)
        {
            if (User.Identity?.IsAuthenticated != true)
                return RedirectToAction("Login", "ClientLogin");

            var user = await userManager.GetUserAsync(User);

            if (user == null)
                return RedirectToAction("Login", "ClientLogin");

            var order = await OrderRepo.TableNoTracking
                .FirstOrDefaultAsync(x =>
                    x.Id == id && x.UserId == user.Id);

            if (order == null)
                return NotFound();

            var orderDto = mapper.Map<OrderDto>(order);

            var items = await OrderItemRepo.TableNoTracking
                .Where(x => x.OrderId == id)
                .Select(x => new OrderItemDto
                {
                    Id = x.Id,
                    OrderId = x.OrderId,
                    ProductId = x.ProductId,
                    ProductTitle = x.Product.Title,
                    Count = x.Count,
                    ProductPrice = x.ProductPrice,
                    Discount = x.Discount,
                    Total = x.Total
                })
                .ToListAsync();

            ViewBag.OrderItems = items;

            return View(orderDto);
        }

        // لغو سفارش
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(Guid id)
        {
            if (User.Identity?.IsAuthenticated != true)
                return RedirectToAction("Login", "ClientLogin");

            var user = await userManager.GetUserAsync(User);

            if (user == null)
                return RedirectToAction("Login", "ClientLogin");

            using var transaction = await _context.Database
                .BeginTransactionAsync(
                    System.Data.IsolationLevel.Serializable);

            // دریافت سفارش متعلق به کاربر جاری
            var order = await OrderRepo.Table
                .FirstOrDefaultAsync(x =>
                    x.Id == id && x.UserId == user.Id);

            if (order == null)
                return NotFound();

            // فقط سفارش پرداخت نشده یا در انتظار پردازش قابل لغو است
            if (order.Status != OrderStatus.NotPayed &&
                order.Status != OrderStatus.Pending)
            {
                TempData["Error"] = "این سفارش قابل لغو نیست.";

                return RedirectToAction(nameof(Details), new { id });
            }

            // دریافت اقلام سفارش همراه با محصول
            var orderItems = await OrderItemRepo.Table
                .Include(x => x.Product)
                .Where(x => x.OrderId == id)
                .ToListAsync();

            // بررسی وجود محصولات
            if (orderItems.Any(x => x.Product == null))
                return BadRequest("اطلاعات یکی از محصولات سفارش موجود نیست.");

            // بازگرداندن موجودی محصولات به انبار
            foreach (var item in orderItems)
            {
                item.Product.Count += item.Count;
            }

            // تغییر وضعیت سفارش
            order.Status = OrderStatus.Cancelled;
            order.LastModifiedDateTime = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            TempData["Success"] = "سفارش با موفقیت لغو شد.";

            return RedirectToAction(nameof(Details), new { id });
        }
    }
}