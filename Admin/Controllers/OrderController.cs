using Admin.Models;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Data.Repositories;
using Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NToastNotify;
using System;
using System.Linq;
using System.Threading.Tasks;

using Data;

namespace Admin.Controllers
{
    [Authorize(Roles = "Admin")]
    public class OrderController : Controller
    {
        private readonly IRepository<Order> OrderRepo;
        private readonly UserManager<User> userManager;
        private readonly IToastNotification notification;
        private readonly IMapper mapper;

        private readonly IRepository<OrderItem> OrderItemRepo;
        private readonly IRepository<Product> ProductRepo;
        private readonly ApplicationDbContext _context;

        public OrderController(IRepository<Order> orderRepo, UserManager<User> userManager, IToastNotification notification, IMapper mapper, IRepository<OrderItem> orderItemRepo, IRepository<Product> productRepo, ApplicationDbContext context)
        {
            OrderRepo = orderRepo;
            this.userManager = userManager;
            this.notification = notification;
            this.mapper = mapper;
            OrderItemRepo = orderItemRepo;
            ProductRepo = productRepo;
            _context = context;
        }

        

        public async Task<IActionResult> Add()
        {
            var model = new OrderDto();

            model.Users = await userManager.Users
                .ProjectTo<UserDto>(mapper.ConfigurationProvider)
                .ToListAsync();

            return View(model);
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(OrderDto dto)
        {
            if (dto.UserId == Guid.Empty)
            {
                notification.AddErrorToastMessage("لطفاً مشتری را انتخاب کنید");
                return RedirectToAction("Add");
            }

            var user = await userManager.FindByIdAsync(dto.UserId.ToString());

            if (user == null)
            {
                notification.AddErrorToastMessage("مشتری مورد نظر پیدا نشد");
                return RedirectToAction("Add");
            }

            if (dto.ShippingPrice < 0)
            {
                notification.AddErrorToastMessage("هزینه ارسال نمی‌تواند منفی باشد");
                return RedirectToAction("Add");
            }

            var model = dto.ToEntity(mapper);

            model.CreationDateTime = DateTime.Now;

            // اطلاعات اولیه سفارش
            model.IsPaid = false;
            model.Status = OrderStatus.Pending;

            // هنوز محصولی به سفارش اضافه نشده است
            model.RawPrice = 0;
            model.Discount = 0;
            model.Total = 0;

            // اطلاعات ارسال
            model.ShippingPrice = dto.ShippingPrice;
            model.Shipping = dto.Shipping;

            OrderRepo.Add(model);

            notification.AddSuccessToastMessage(
                "سفارش ثبت شد؛ لطفاً اقلام سفارش را اضافه کنید");

            return RedirectToAction("List");
        }

        // نمایش لیست سفارشات همراه با مجموع تعداد کالاهای هر سفارش
        public async Task<IActionResult> List()
        {
            var model = await OrderRepo.TableNoTracking
                .ProjectTo<OrderDto>(mapper.ConfigurationProvider)
                .ToListAsync();

            foreach (var order in model)
            {
                order.ProductCount = await OrderItemRepo.TableNoTracking
                    .Where(x => x.OrderId == order.Id)
                    .SumAsync(x => (int?)x.Count) ?? 0;
            }

            return View(model);
        }

        

        public async Task<IActionResult> Update(Guid Id)
        {
            var model = await OrderRepo.AllTableNoTracking
                .ProjectTo<OrderDto>(mapper.ConfigurationProvider)
                .FirstOrDefaultAsync(x => x.Id == Id);

            if (model == null)
            {
                notification.AddErrorToastMessage("سفارش مورد نظر یافت نشد");
                return RedirectToAction("List");
            }

            model.Users = await userManager.Users
                .ProjectTo<UserDto>(mapper.ConfigurationProvider)
                .ToListAsync();

            return View(model);
        }

        

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(OrderDto dto)
        {
            if (dto.Id == Guid.Empty)
            {
                notification.AddErrorToastMessage("شناسه سفارش معتبر نیست");
                return RedirectToAction("List");
            }

            var model = await OrderRepo.AllTableNoTracking
                .FirstOrDefaultAsync(x => x.Id == dto.Id);

            if (model == null)
            {
                notification.AddErrorToastMessage("سفارش مورد نظر یافت نشد");
                return RedirectToAction("List");
            }

            if (dto.UserId == Guid.Empty)
            {
                notification.AddErrorToastMessage("لطفاً مشتری را انتخاب کنید");
                return RedirectToAction("Update", new { Id = dto.Id });
            }

            var user = await userManager.FindByIdAsync(dto.UserId.ToString());

            if (user == null)
            {
                notification.AddErrorToastMessage("مشتری مورد نظر پیدا نشد");
                return RedirectToAction("Update", new { Id = dto.Id });
            }

            if (dto.ShippingPrice < 0)
            {
                notification.AddErrorToastMessage("هزینه ارسال نمی‌تواند منفی باشد");
                return RedirectToAction("Update", new { Id = dto.Id });
            }

            if (!Enum.IsDefined(typeof(OrderStatus), dto.Status))
            {
                notification.AddErrorToastMessage("وضعیت سفارش معتبر نیست");
                return RedirectToAction("Update", new { Id = dto.Id });
            }

            // دریافت تمام آیتم‌های سفارش
            var items = await OrderItemRepo.TableNoTracking
                .Where(x => x.OrderId == model.Id)
                .ToListAsync();

            // محاسبه مجدد مبلغ خام و تخفیف
            long rawPrice = 0;
            long discountAmount = 0;

            foreach (var item in items)
            {
                long itemRawPrice = (long)item.ProductPrice * item.Count;

                rawPrice += itemRawPrice;

                discountAmount += itemRawPrice * item.Discount / 100;
            }

            // جلوگیری از ثبت مبالغ خارج از محدوده int
            if (rawPrice > int.MaxValue || discountAmount > int.MaxValue)
            {
                notification.AddErrorToastMessage("مبلغ سفارش بیش از حد مجاز است");
                return RedirectToAction("Update", new { Id = dto.Id });
            }

            // اطلاعات اصلی سفارش
            model.UserId = dto.UserId;
            model.Code = dto.Code;
            model.IsPaid = dto.IsPaid;
            model.Status = dto.Status;

            // اطلاعات ارسال
            model.Shipping = dto.Shipping;
            model.ShippingPrice = dto.ShippingPrice;

            // مبالغ محاسبه‌شده از روی آیتم‌های سفارش
            model.RawPrice = (int)rawPrice;
            model.Discount = (int)discountAmount;

            model.Total = model.RawPrice
                          - model.Discount
                          + model.ShippingPrice;

            OrderRepo.Update(model);

            notification.AddSuccessToastMessage("سفارش با موفقیت ویرایش شد");

            return RedirectToAction("List");
        }

        

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid Id)
        {
            // شروع تراکنش
            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            // دریافت سفارش به صورت Tracked
            var order = await OrderRepo.Table
                .FirstOrDefaultAsync(x => x.Id == Id);

            if (order == null)
            {
                notification.AddErrorToastMessage("سفارش مورد نظر یافت نشد");
                return RedirectToAction("List");
            }

            // دریافت آیتم‌های سفارش
            var items = await OrderItemRepo.Table
                .Where(x => x.OrderId == order.Id)
                .ToListAsync();

            // اگر سفارش قبلاً لغو نشده باشد، موجودی را برگردان
            if (order.Status != OrderStatus.Cancelled)
            {
                foreach (var item in items)
                {
                    // دریافت محصول به صورت Tracked
                    var product = await ProductRepo.Table
                        .FirstOrDefaultAsync(x => x.Id == item.ProductId);

                    if (product == null)
                    {
                        notification.AddErrorToastMessage(
                            "یکی از محصولات سفارش یافت نشد و حذف انجام نشد");

                        return RedirectToAction("List");
                    }

                    // بازگرداندن موجودی محصول
                    product.Count += item.Count;

                    ProductRepo.Update(product, saveNow: false);
                }
            }

            // حذف آیتم‌های سفارش بدون ذخیره جداگانه
            foreach (var item in items)
            {
                OrderItemRepo.Delete(item, saveNow: false);
            }

            // حذف خود سفارش بدون ذخیره جداگانه
            OrderRepo.Delete(order, saveNow: false);

            // ذخیره تمام تغییرات
            await _context.SaveChangesAsync();

            // نهایی کردن تراکنش
            await transaction.CommitAsync();

            notification.AddSuccessToastMessage(
                "سفارش و آیتم‌های آن با موفقیت حذف شدند");

            return RedirectToAction("List");
        }




        // لغو سفارش و بازگرداندن موجودی محصولات به انبار، بدون حذف سفارش
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(Guid Id)
        {
            // شروع تراکنش
            await using var transaction = await _context.Database.BeginTransactionAsync();

            // پیدا کردن سفارش
            var order = await OrderRepo.Table
                .FirstOrDefaultAsync(x => x.Id == Id);

            if (order == null)
            {
                notification.AddErrorToastMessage("سفارش مورد نظر یافت نشد");
                return RedirectToAction("List");
            }

            // بررسی اینکه سفارش قبلاً لغو نشده باشد
            if (order.Status == OrderStatus.Cancelled)
            {
                notification.AddWarningToastMessage("این سفارش قبلاً لغو شده است");
                return RedirectToAction("List");
            }

            // بررسی وضعیت مجاز برای لغو سفارش
            if (order.Status != OrderStatus.NotPayed &&
                order.Status != OrderStatus.Pending)
            {
                notification.AddErrorToastMessage("این سفارش در وضعیت فعلی قابل لغو نیست");
                return RedirectToAction("List");
            }

            // بررسی پرداخت
            if (order.IsPaid)
            {
                notification.AddErrorToastMessage("سفارش پرداخت شده قابل لغو نیست");
                return RedirectToAction("List");
            }

            // دریافت آیتم‌های سفارش
            var orderItems = await OrderItemRepo.TableNoTracking
                .Where(x => x.OrderId == order.Id)
                .ToListAsync();

            if (orderItems.Count == 0)
            {
                notification.AddErrorToastMessage("آیتمی برای این سفارش یافت نشد");
                return RedirectToAction("List");
            }

            // بازگرداندن تعداد محصولات سفارش به موجودی انبار
            foreach (var item in orderItems)
            {
                var product = await ProductRepo.Table
                    .FirstOrDefaultAsync(x => x.Id == item.ProductId);

                if (product == null)
                {
                    notification.AddErrorToastMessage("یکی از محصولات سفارش یافت نشد");
                    return RedirectToAction("List");
                }

                product.Count += item.Count;

                ProductRepo.Update(product, saveNow: false);
            }

            // تغییر وضعیت سفارش به لغو شده
            order.Status = OrderStatus.Cancelled;

            // ذخیره تغییرات داخل تراکنش
            OrderRepo.Update(order, saveNow: false);

            // ذخیره تمام تغییرات دیتابیس
            await _context.SaveChangesAsync();

            // نهایی کردن تراکنش
            await transaction.CommitAsync();

            notification.AddSuccessToastMessage("سفارش با موفقیت لغو شد و موجودی به انبار برگشت");

            // ادامه عملیات لغو در مرحله بعد

            return RedirectToAction("List");
        }
    }
}
