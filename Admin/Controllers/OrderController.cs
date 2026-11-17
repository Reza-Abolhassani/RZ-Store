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

        public OrderController(IRepository<Order> orderRepo, UserManager<User> userManager, IToastNotification notification, IMapper mapper, IRepository<OrderItem> orderItemRepo, IRepository<Product> productRepo)
        {
            OrderRepo = orderRepo;
            this.userManager = userManager;
            this.notification = notification;
            this.mapper = mapper;
            OrderItemRepo = orderItemRepo;
            ProductRepo = productRepo;
        }

        //public async Task<IActionResult> Add()
        //{
        //    var model = new OrderDto();

        //    model.Users = await userManager.Users.ProjectTo<UserDto>(mapper.ConfigurationProvider)
        //        .ToListAsync();

        //    return View(model);
        //}

        public async Task<IActionResult> Add()
        {
            var model = new OrderDto();

            model.Users = await userManager.Users
                .ProjectTo<UserDto>(mapper.ConfigurationProvider)
                .ToListAsync();

            return View(model);
        }
        //[HttpPost]
        //public async Task<IActionResult> Add(OrderDto dto)
        //{
        //    var model = dto.ToEntity(mapper);

        //    model.CreationDateTime = DateTime.Now;

        //    OrderRepo.Add(model);

        //    notification.AddSuccessToastMessage("سفارش ثبت شد لطفا اقلام رو اضافه کنید");

        //    return RedirectToAction("List");
        //}
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

        public async Task<IActionResult> List()
        {
            var model = await OrderRepo.TableNoTracking.ProjectTo<OrderDto>(mapper.ConfigurationProvider)
                .ToListAsync();

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

        //public async Task<IActionResult> Update(Guid Id)
        //{
        //    var model = await OrderRepo.AllTableNoTracking
        //        .ProjectTo<OrderDto>(mapper.ConfigurationProvider)
        //        .FirstOrDefaultAsync(x => x.Id == Id);

        //    if (model == null)
        //    {
        //        notification. AddErrorToastMessage("سفارش مورد نظر یافت نشد");
        //        return RedirectToAction("List");

        //    }


        //    var users = await userManager.Users
        //        .ProjectTo<UserDto>(mapper.ConfigurationProvider)
        //        .ToListAsync();

        //    model.Users = users;

        //    return View(model);
        //}

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

        //[HttpPost]
        //public async Task<IActionResult> Update(OrderDto dto)
        //{
        //    if (dto.Id != Guid.Empty)
        //    {
        //        var model = await OrderRepo.AllTableNoTracking//دریافت سفارش از دیتابیس
        //            .FirstOrDefaultAsync(x => x.Id == dto.Id);

        //        if (model == null)//بررسی وجود سفارش
        //        {
        //            notification.AddErrorToastMessage("سفارش مورد نظر یافت نشد");
        //            return RedirectToAction("List");
        //        }

        //        model.UserId = dto.UserId;//مقداردهی Propertyها
        //        model.Code = dto.Code;
        //        model.IsPaid = dto.IsPaid;
        //        model.Shipping = dto.Shipping;
        //        model.ShippingPrice = dto.ShippingPrice;
        //        model.RawPrice = dto.RawPrice;
        //        model.Discount = dto.Discount;
        //        model.Total = dto.Total;

        //        OrderRepo.Update(model);//ذخیره تغییرات
        //    }

        //    return RedirectToAction("List");
        //}

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid Id)
        {
            var order = await OrderRepo.AllTableNoTracking
                .FirstOrDefaultAsync(x => x.Id == Id);

            if (order == null)
            {
                notification.AddErrorToastMessage("سفارش مورد نظر یافت نشد");
                return RedirectToAction("List");
            }

            // دریافت تمام آیتم‌های سفارش
            var items = await OrderItemRepo.AllTableNoTracking
                .Where(x => x.OrderId == order.Id)
                .ToListAsync();

            // بازگرداندن موجودی محصولات و حذف آیتم‌ها
            foreach (var item in items)
            {
                var product = await ProductRepo.AllTableNoTracking
                    .FirstOrDefaultAsync(x => x.Id == item.ProductId);

                if (product != null)
                {
                    // بازگرداندن تعداد محصول به انبار
                    product.Count += item.Count;

                    ProductRepo.Update(product);
                }

                // حذف آیتم سفارش
                OrderItemRepo.Delete(item);
            }

            // حذف خود سفارش
            OrderRepo.Delete(order);

            notification.AddSuccessToastMessage(
                "سفارش و آیتم‌های آن با موفقیت حذف شدند");

            return RedirectToAction("List");
        }

        //public async Task<IActionResult> Delete(Guid Id)
        //{
        //    var order = await OrderRepo.AllTableNoTracking
        //        .FirstOrDefaultAsync(x => x.Id == Id);

        //    if (order == null)
        //    {
        //        notification.AddErrorToastMessage("سفارش مورد نظر یافت نشد");
        //        return RedirectToAction("List");
        //    }

        //    OrderRepo.Delete(order);

        //    notification.AddSuccessToastMessage("سفارش با موفقیت حذف شد");

        //    return RedirectToAction("List");
        //}
    }
}
