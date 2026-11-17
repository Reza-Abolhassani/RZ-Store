using Admin.Models;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Data.Repositories;
using Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NToastNotify;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Admin.Controllers
{
    [Authorize(Roles = "Admin")]
    public class OrderItemController : Controller
    {
        private readonly IRepository<Order> OrderRepo;
        private readonly IRepository<Product> ProductRepo;
        private readonly IRepository<OrderItem> OrderItemRepo;
        private readonly IMapper mapper;
        private readonly IToastNotification notification;

        public OrderItemController(IRepository<Order> orderRepo, IRepository<Product> productRepo, IRepository<OrderItem> orderItemRepo, IMapper mapper, IToastNotification notification)
        {
            OrderRepo = orderRepo;
            ProductRepo = productRepo;
            OrderItemRepo = orderItemRepo;
            this.mapper = mapper;
            this.notification = notification;
        }

        public async Task<IActionResult> Add(Guid OId)
        {
            var model = new OrderItemDto();

            model.OrderId = OId;
            model.Discount = 0;

            model.products = await ProductRepo.TableNoTracking
                .ProjectTo<ProductDto>(mapper.ConfigurationProvider)
                .ToListAsync();

            model.orders = await OrderRepo.TableNoTracking
                .ProjectTo<OrderDto>(mapper.ConfigurationProvider)
                .ToListAsync();

            return View(model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(OrderItemDto dto)
        {
            // بررسی تعداد
            if (dto.Count <= 0)
            {
                notification.AddErrorToastMessage("تعداد باید بیشتر از صفر باشد");
                return RedirectToAction("Add", new { OId = dto.OrderId });
            }

            // بررسی درصد تخفیف
            if (dto.Discount < 0 || dto.Discount > 100)
            {
                notification.AddErrorToastMessage("درصد تخفیف باید بین صفر تا صد باشد");
                return RedirectToAction("Add", new { OId = dto.OrderId });
            }

            // دریافت محصول
            var product = await ProductRepo.TableNoTracking
                .FirstOrDefaultAsync(x => x.Id == dto.ProductId);

            if (product == null)
            {
                notification.AddErrorToastMessage("محصول مورد نظر یافت نشد");
                return RedirectToAction("Add", new { OId = dto.OrderId });
            }

            // بررسی موجودی محصول
            if (product.Count < dto.Count)
            {
                notification.AddErrorToastMessage("موجودی کافی نیست");
                return RedirectToAction("Add", new { OId = dto.OrderId });
            }

            // دریافت سفارش
            var order = await OrderRepo.TableNoTracking
                .FirstOrDefaultAsync(x => x.Id == dto.OrderId);

            if (order == null)
            {
                notification.AddErrorToastMessage("سفارش مورد نظر یافت نشد");
                return RedirectToAction("List");
            }

            // تبدیل DTO به Entity
            var model = dto.ToEntity(mapper);

            // قیمت واحد محصول
            model.ProductPrice = product.Total;

            // ذخیره درصد تخفیف
            model.Discount = dto.Discount;

            // محاسبه مبلغ اولیه
            var rawPrice = product.Total * dto.Count;

            // محاسبه مبلغ تخفیف
            var discountAmount = (int)((long)rawPrice * dto.Discount / 100);

            // محاسبه مبلغ نهایی آیتم
            model.Total = rawPrice - discountAmount;

            // ثبت آیتم سفارش
            OrderItemRepo.Add(model);

            // کاهش موجودی محصول
            product.Count -= dto.Count;
            ProductRepo.Update(product);

            // به روزرسانی مبالغ سفارش
            order.RawPrice += rawPrice;
            order.Discount += discountAmount;

            order.Total = order.RawPrice
                          - order.Discount
                          + order.ShippingPrice;

            OrderRepo.Update(order);

            notification.AddSuccessToastMessage(
                "محصول با موفقیت به سفارش اضافه شد");

            return RedirectToAction("List", new { OId = dto.OrderId });
        }

        //public async Task<IActionResult> Add()
        //{

        //    var model = new OrderItemDto();

        //    model.products = await ProductRepo.TableNoTracking
        //        .ProjectTo<ProductDto>(mapper.ConfigurationProvider)
        //        .ToListAsync();

        //    model.orders = await OrderRepo.TableNoTracking
        //        .ProjectTo<OrderDto>(mapper.ConfigurationProvider)
        //        .ToListAsync();

        //    return View(model);
        //}
        //[HttpPost]
        //public async Task<IActionResult> Add(OrderItemDto dto)
        //{
        //    if (dto.Count <= 0)
        //    {
        //        notification.AddErrorToastMessage("تعداد باید بیشتر از صفر باشد");
        //        return RedirectToAction("Add");
        //    }

        //    var model = dto.ToEntity(mapper);

        //    var product = await ProductRepo.TableNoTracking
        //        .FirstOrDefaultAsync(x => x.Id == dto.ProductId);

        //    if (product == null)
        //    {
        //        notification.AddErrorToastMessage("محصول مورد نظر یافت نشد");
        //        return RedirectToAction("Add");
        //    }

        //    if (product.Count<dto.Count)
        //    {
        //        notification.AddErrorToastMessage("موجودی کافی نیست");

        //        return RedirectToAction();
        //    }

        //    model.ProductPrice = product.Total;

        //    model.Total = product.Total * dto.Count;

        //    OrderItemRepo.Add(model);

        //    // کاهش موجودی محصول بعد از اضافه شدن به سفارش
        //    product.Count -= dto.Count;

        //    ProductRepo.Update(product);

        //    var order = await OrderRepo.TableNoTracking
        //        .FirstOrDefaultAsync(x => x.Id == dto.OrderId);

        //    if (order == null)
        //    {
        //        notification.AddErrorToastMessage("سفارش مورد نظر یافت نشد");
        //        return RedirectToAction("List");
        //    }

        //    order.Total += model.Total;

        //    OrderRepo.Update(order);

        //    notification.AddSuccessToastMessage("محصول با موفقیت به سفارش اضافه شد");

        //    return RedirectToAction("List");

        //}


        public async Task<IActionResult> List(Guid OId)
        {
            ViewBag.OrderId = OId;

            var model = await OrderItemRepo.TableNoTracking
                .Where(x => x.OrderId == OId)
                .ProjectTo<OrderItemDto>(mapper.ConfigurationProvider)
                .ToListAsync();

            return View(model);
        }
        //public async Task<IActionResult> List(Guid OId)
        //{
        //    var model = await OrderItemRepo.TableNoTracking
        //        .Where(x=>x.OrderId == OId)
        //        .ProjectTo<OrderItemDto>(mapper.ConfigurationProvider).ToListAsync();

        //    return View(model);
        //}

        public async Task<IActionResult> Update(Guid Id)
        {
            var model = await OrderItemRepo.AllTableNoTracking
                .ProjectTo<OrderItemDto>(mapper.ConfigurationProvider)
                .FirstOrDefaultAsync(x => x.Id == Id);

            if (model == null)
            {
                notification.AddErrorToastMessage("آیتم سفارش یافت نشد");
                return RedirectToAction("List");
            }

            var product = await ProductRepo.TableNoTracking
                .FirstOrDefaultAsync(x => x.Id == model.ProductId);

            if (product != null)
            {
                model.ProductTitle = product.Title;
            }

            return View(model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(OrderItemDto dto)
        {
            // دریافت آیتم سفارش
            var model = await OrderItemRepo.AllTableNoTracking
                .FirstOrDefaultAsync(x => x.Id == dto.Id);

            if (model == null)
            {
                notification.AddErrorToastMessage("آیتم سفارش یافت نشد");
                return RedirectToAction("List");
            }

            // بررسی تعداد
            if (dto.Count <= 0)
            {
                notification.AddErrorToastMessage("تعداد باید بیشتر از صفر باشد");
                return RedirectToAction("List", new { OId = model.OrderId });
            }

            // دریافت محصول
            var product = await ProductRepo.TableNoTracking
                .FirstOrDefaultAsync(x => x.Id == model.ProductId);

            if (product == null)
            {
                notification.AddErrorToastMessage("محصول مورد نظر یافت نشد");
                return RedirectToAction("List", new { OId = model.OrderId });
            }

            // دریافت سفارش
            var order = await OrderRepo.TableNoTracking
                .FirstOrDefaultAsync(x => x.Id == model.OrderId);

            if (order == null)
            {
                notification.AddErrorToastMessage("سفارش مورد نظر یافت نشد");
                return RedirectToAction("List");
            }

            // محاسبه اختلاف تعداد
            var oldCount = model.Count;
            var difference = dto.Count - oldCount;

            // بررسی موجودی در صورت افزایش تعداد
            if (difference > 0 && product.Count < difference)
            {
                notification.AddErrorToastMessage("موجودی کافی نیست");
                return RedirectToAction("List", new { OId = model.OrderId });
            }

            // بررسی درصد تخفیف
            if (dto.Discount < 0 || dto.Discount > 100)
            {
                notification.AddErrorToastMessage("درصد تخفیف باید بین صفر تا صد باشد");
                return RedirectToAction("List", new { OId = model.OrderId });
            }

            // محاسبه مبلغ قبلی آیتم
            var oldRawPrice = (long)model.ProductPrice * model.Count;

            var oldDiscountAmount =
                oldRawPrice * model.Discount / 100;

            // اصلاح موجودی محصول
            product.Count -= difference;
            ProductRepo.Update(product);

            // محاسبه مبلغ جدید آیتم
            var newRawPrice = (long)product.Total * dto.Count;

            var newDiscountAmount =
                newRawPrice * dto.Discount / 100;

            // به روزرسانی اطلاعات آیتم
            model.Count = dto.Count;
            model.ProductPrice = product.Total;
            model.Discount = dto.Discount;

            model.Total = (int)(newRawPrice - newDiscountAmount);

            // اصلاح مبالغ سفارش
            order.RawPrice = (int)(
                order.RawPrice - oldRawPrice + newRawPrice);

            order.Discount = (int)(
                order.Discount - oldDiscountAmount + newDiscountAmount);

            order.Total = order.RawPrice
                          - order.Discount
                          + order.ShippingPrice;

            // ذخیره تغییرات
            OrderItemRepo.Update(model);
            OrderRepo.Update(order);

            notification.AddSuccessToastMessage(
                "آیتم سفارش با موفقیت ویرایش شد");

            return RedirectToAction("List", new { OId = model.OrderId });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid Id)
        {
            var model = await OrderItemRepo.AllTableNoTracking
                .FirstOrDefaultAsync(x => x.Id == Id);

            if (model == null)
            {
                notification.AddErrorToastMessage("آیتم سفارش پیدا نشد");
                return RedirectToAction("List");
            }

            var order = await OrderRepo.AllTableNoTracking
                .FirstOrDefaultAsync(x => x.Id == model.OrderId);

            if (order == null)
            {
                notification.AddErrorToastMessage("سفارش پیدا نشد");
                return RedirectToAction("List");
            }

            var product = await ProductRepo.AllTableNoTracking
                .FirstOrDefaultAsync(x => x.Id == model.ProductId);

            // محاسبه مبلغ خام آیتم
            long rawPrice = (long)model.ProductPrice * model.Count;

            // محاسبه مبلغ تخفیف آیتم
            long discountAmount = rawPrice * model.Discount / 100;

            // اصلاح مبالغ سفارش
            order.RawPrice = (int)(order.RawPrice - rawPrice);
            order.Discount = (int)(order.Discount - discountAmount);

            order.Total = order.RawPrice
                          - order.Discount
                          + order.ShippingPrice;

            // بازگرداندن موجودی محصول به انبار
            if (product != null)
            {
                product.Count += model.Count;
                ProductRepo.Update(product);
            }

            // ذخیره تغییرات سفارش
            OrderRepo.Update(order);

            // حذف آیتم سفارش
            OrderItemRepo.Delete(model);

            notification.AddSuccessToastMessage("آیتم سفارش با موفقیت حذف شد");

            return RedirectToAction("List", new { OId = model.OrderId });
        }
    }
}
