using Client.Dto;
using Client.Models;
using Data.Repositories;
using Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NToastNotify;
using System;
using System.Linq;
using System.Threading.Tasks;

using Services.Interfaces;

using Data;

namespace Client.Controllers
{
    public class ShopCartController : Controller
    {
        private readonly IRepository<ShopCart> ShopCartRepo;
        private readonly IRepository<ShopCardItem> ShopCardItemRepo;
        private readonly IRepository<Product> ProductRepo;

        private readonly UserManager<User> userManager;
        private readonly IToastNotification notification;

        private readonly IRepository<ShippingType> ShippingTypeRepo;

        private readonly IRepository<UserAddress> UserAddressRepo;
        private readonly IRepository<Order> OrderRepo;
        private readonly IRepository<OrderItem> OrderItemRepo;

        private readonly ApplicationDbContext _context;

        private readonly IPaymentService _paymentService;

        public ShopCartController(IRepository<ShopCart> shopCartRepo, IRepository<ShopCardItem> shopCardItemRepo, IRepository<Product> productRepo, UserManager<User> userManager, IToastNotification notification, IRepository<ShippingType> shippingTypeRepo, IRepository<UserAddress> userAddressRepo, IRepository<Order> orderRepo, IRepository<OrderItem> orderItemRepo, ApplicationDbContext context, IPaymentService paymentService)
        {
            ShopCartRepo = shopCartRepo;
            ShopCardItemRepo = shopCardItemRepo;
            ProductRepo = productRepo;
            this.userManager = userManager;
            this.notification = notification;
            ShippingTypeRepo = shippingTypeRepo;
            UserAddressRepo = userAddressRepo;
            OrderRepo = orderRepo;
            OrderItemRepo = orderItemRepo;
            _context = context;
            _paymentService = paymentService;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(Guid ProductId, int count, bool mobin)
        {

            


            if (User.Identity.IsAuthenticated == false)
            {
                return RedirectToAction("Login", "ClientLogin");
            }

            var user = await userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction("Login", "ClientLogin");
            }

            var callbackUrl = Url.Action(
                "Callback",
                "Payment",
                null,
                Request.Scheme);

            if (string.IsNullOrWhiteSpace(callbackUrl))
            {
                notification.AddErrorToastMessage(
                    "ساخت آدرس بازگشت از درگاه پرداخت امکان‌پذیر نبود.");

                return RedirectToAction("Index", "Home");
            }




            var shopCart = await ShopCartRepo.AllTableNoTracking
                .FirstOrDefaultAsync(x => x.UserId == user.Id);

            if (shopCart == null)
            {
                shopCart = new ShopCart()
                {
                    UserId = user.Id,
                    TotalRawPrice = 0,
                    TotalPrice = 0,
                    DiscountPrice = 0
                };

                ShopCartRepo.Add(shopCart);
            }



            var product = await ProductRepo.AllTableNoTracking
                .FirstOrDefaultAsync(x => x.Id == ProductId);

            if (product == null)
            {
                notification.AddErrorToastMessage("محصول مورد نظر یافت نشد");
                return RedirectToAction("Index", "Shop");
            }

            if (count <= 0 || count > product.Count)
            {
                notification.AddErrorToastMessage("تعداد محصول نامعتبر است");
                return RedirectToAction("ProductDetail", "Shop", new { Id = ProductId });
            }




            var item = await ShopCardItemRepo.AllTableNoTracking
                .FirstOrDefaultAsync(x => x.ProductId == ProductId && x.ShopCartId == shopCart.Id);

            if (item != null)
            {
                notification.AddErrorToastMessage("این محصول قبلاً به سبد خرید شما اضافه شده است.");
                return RedirectToAction("ShopCart", "ShopCart");
            }

            long rawPrice = product.RawPrice * count;
            long totalPrice = product.Total * count;
            long discountPrice = rawPrice - totalPrice;

            var shopCartItem = new ShopCardItem()
            {
                ProductId = ProductId,
                ShopCartId = shopCart.Id,
                count = count,
                TotalPrice = totalPrice
            };

            ShopCardItemRepo.Add(shopCartItem);

            shopCart.TotalRawPrice += rawPrice;
            shopCart.TotalPrice += totalPrice;
            shopCart.DiscountPrice += discountPrice;

            ShopCartRepo.Update(shopCart);

            notification.AddSuccessToastMessage("محصول به سبد خرید اضافه شد");

            if (mobin)
            {
                return RedirectToAction("ProductDetail", "Shop", new { Id = ProductId });
            }

            return RedirectToAction("ShopCart", "ShopCart");
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(Guid Id, int count)
        {
            if (User.Identity.IsAuthenticated == false)
            {
                return RedirectToAction("Login", "ClientLogin");
            }

            var user = await userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction("Login", "ClientLogin");
            }

            var shopCart = await ShopCartRepo.AllTableNoTracking
                .FirstOrDefaultAsync(x => x.UserId == user.Id);

            if (shopCart == null)
            {
                notification.AddErrorToastMessage("سبد خرید یافت نشد");
                return RedirectToAction("ShopCart");
            }


            var item = await ShopCardItemRepo.AllTableNoTracking
                .FirstOrDefaultAsync(x => x.Id == Id && x.ShopCartId == shopCart.Id);

            if (item == null)
            {
                notification.AddErrorToastMessage("محصول مورد نظر در سبد خرید یافت نشد");
                return RedirectToAction("ShopCart");
            }

            var product = await ProductRepo.AllTableNoTracking
                .FirstOrDefaultAsync(x => x.Id == item.ProductId);

            if (product == null)
            {
                notification.AddErrorToastMessage("محصول یافت نشد");
                return RedirectToAction("ShopCart");
            }

            if (count <= 0 || count > product.Count)
            {
                notification.AddErrorToastMessage("تعداد درخواستی نامعتبر است یا موجودی کافی نیست");
                return RedirectToAction("ShopCart");
            }

            // محاسبه قیمت قبلی
            long oldRawPrice = product.RawPrice * item.count;
            long oldTotalPrice = item.TotalPrice;
            long oldDiscountPrice = oldRawPrice - oldTotalPrice;

            // حذف قیمت قبلی از جمع سبد
            shopCart.TotalRawPrice -= oldRawPrice;
            shopCart.TotalPrice -= oldTotalPrice;
            shopCart.DiscountPrice -= oldDiscountPrice;

            // محاسبه قیمت جدید
            long newRawPrice = product.RawPrice * count;
            long newTotalPrice = product.Total * count;
            long newDiscountPrice = newRawPrice - newTotalPrice;

            // تغییر تعداد و قیمت آیتم
            item.count = count;
            item.TotalPrice = newTotalPrice;

            // اضافه کردن قیمت جدید به جمع سبد
            shopCart.TotalRawPrice += newRawPrice;
            shopCart.TotalPrice += newTotalPrice;
            shopCart.DiscountPrice += newDiscountPrice;

            ShopCardItemRepo.Update(item);
            ShopCartRepo.Update(shopCart);

            notification.AddSuccessToastMessage("تعداد محصول با موفقیت ویرایش شد");

            return RedirectToAction("ShopCart");
        }


        public async Task<IActionResult> ShopCart()
        {
            if (User.Identity.IsAuthenticated == false)
            {
                return RedirectToAction("Login", "ClientLogin");
            }

            var user = await userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction("Login", "ClientLogin");
            }

            var shopCart = await ShopCartRepo.TableNoTracking
                .FirstOrDefaultAsync(x => x.UserId == user.Id);

            if (shopCart == null)
            {
                notification.AddErrorToastMessage("سبد خرید شما یافت نشد");
                return View(new ShopCartDto());
            }

            var items = await ShopCardItemRepo.TableNoTracking
    .Where(x => x.ShopCartId == shopCart.Id)
    .Select(x => new ShopCardItemDto
    {
        Id = x.Id,
        ProductId = x.ProductId,
        ProductTitle = x.Product.Title,
        ImageUrl = x.Product.ImageUrl,

        RawPrice = x.Product.RawPrice,
        Price = x.Product.Total,

        TotalPrice = x.TotalPrice,
        count = x.count
    })
    .ToListAsync();

            var model = new ShopCartDto
            {
                Id = shopCart.Id,
                TotalRawPrice = shopCart.TotalRawPrice,
                TotalPrice = shopCart.TotalPrice,
                DiscountPrice = shopCart.DiscountPrice,
                Items = items
            };

            return View(model);
        }


        // دریافت اطلاعات آدرس و ذخیره آن برای کاربر واردشده
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAddress(UserAddressDto model)
        {
            if (User.Identity.IsAuthenticated == false)
            {
                return RedirectToAction("Login", "ClientLogin");
            }

            var user = await userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction("Login", "ClientLogin");
            }

            if (string.IsNullOrWhiteSpace(model.ReceiverName) ||
                string.IsNullOrWhiteSpace(model.ReceiverPhone) ||
                string.IsNullOrWhiteSpace(model.Province) ||
                string.IsNullOrWhiteSpace(model.City) ||
                string.IsNullOrWhiteSpace(model.Address) ||
                string.IsNullOrWhiteSpace(model.PostalCode))
            {
                notification.AddErrorToastMessage("لطفاً تمام اطلاعات آدرس را وارد کنید");
                return RedirectToAction("Checkout");
            }

            var userAddresses = await UserAddressRepo.TableNoTracking
                .Where(x => x.UserId == user.Id)
                .ToListAsync();

            bool isDefault = model.IsDefault || !userAddresses.Any();

            if (isDefault)
            {
                foreach (var address in userAddresses)
                {
                    if (address.IsDefault)
                    {
                        address.IsDefault = false;
                        UserAddressRepo.Update(address);
                    }
                }
            }

            var newAddress = new UserAddress
            {
                UserId = user.Id,
                ReceiverName = model.ReceiverName,
                ReceiverPhone = model.ReceiverPhone,
                Province = model.Province,
                City = model.City,
                Address = model.Address,
                PostalCode = model.PostalCode,
                IsDefault = isDefault
            };

            UserAddressRepo.Add(newAddress);

            notification.AddSuccessToastMessage("آدرس شما با موفقیت ثبت شد");

            return RedirectToAction("Checkout");
        }


        // نمایش صفحه ثبت سفارش و دریافت اطلاعات سبد خرید و روش‌های ارسال
        public async Task<IActionResult> Checkout()
        {
            if (User.Identity.IsAuthenticated == false)
            {
                return RedirectToAction("Login", "ClientLogin");
            }

            var user = await userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction("Login", "ClientLogin");
            }

            var shopCart = await ShopCartRepo.TableNoTracking
                .FirstOrDefaultAsync(x => x.UserId == user.Id);

            if (shopCart == null)
            {
                notification.AddErrorToastMessage("سبد خرید شما یافت نشد");
                return RedirectToAction("ShopCart");
            }

            var items = await ShopCardItemRepo.TableNoTracking
                .Where(x => x.ShopCartId == shopCart.Id)
                .Select(x => new ShopCardItemDto
                {
                    Id = x.Id,
                    ProductId = x.ProductId,
                    ProductTitle = x.Product.Title,
                    ImageUrl = x.Product.ImageUrl,
                    RawPrice = x.Product.RawPrice,
                    Price = x.Product.Total,
                    TotalPrice = x.TotalPrice,
                    count = x.count
                })
                .ToListAsync();

            if (!items.Any())
            {
                notification.AddErrorToastMessage("سبد خرید شما خالی است");
                return RedirectToAction("ShopCart");
            }

            var shopCartDto = new ShopCartDto
            {
                Id = shopCart.Id,
                TotalRawPrice = shopCart.TotalRawPrice,
                TotalPrice = shopCart.TotalPrice,
                DiscountPrice = shopCart.DiscountPrice,
                Items = items
            };

            // دریافت آدرس‌های ذخیره‌شده کاربر جاری
            var userAddresses = await UserAddressRepo.TableNoTracking
                .Where(x => x.UserId == user.Id)
                .Select(x => new UserAddressDto
                {
                    Id = x.Id,
                    ReceiverName = x.ReceiverName,
                    ReceiverPhone = x.ReceiverPhone,
                    Province = x.Province,
                    City = x.City,
                    Address = x.Address,
                    PostalCode = x.PostalCode,
                    IsDefault = x.IsDefault
                })
                .ToListAsync();

            var shippingTypes = await ShippingTypeRepo.TableNoTracking
                .ToListAsync();

            var defaultShippingType = shippingTypes.FirstOrDefault();

            var model = new CheckoutDto
            {
                ShopCart = shopCartDto,

                ShippingTypeId = defaultShippingType?.Id ?? Guid.Empty,

                ShippingTypes = shippingTypes.Select(x => new ShippingTypeDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Price = x.Price

                }).ToList(),

                UserAddresses = userAddresses

                


            };

            return View(model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(Guid Id)
        {
            if (User.Identity.IsAuthenticated == false)
            {
                return RedirectToAction("Login", "ClientLogin");
            }

            var user = await userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction("Login", "ClientLogin");
            }

            var shopCart = await ShopCartRepo.AllTableNoTracking
                .FirstOrDefaultAsync(x => x.UserId == user.Id);

            if (shopCart == null)
            {
                notification.AddErrorToastMessage("سبد خرید یافت نشد");
                return RedirectToAction("ShopCart");
            }

            var item = await ShopCardItemRepo.AllTableNoTracking
                .FirstOrDefaultAsync(x => x.Id == Id && x.ShopCartId == shopCart.Id);

            if (item == null)
            {
                notification.AddErrorToastMessage("محصول مورد نظر یافت نشد");
                return RedirectToAction("ShopCart");
            }

            var product = await ProductRepo.AllTableNoTracking
                .FirstOrDefaultAsync(x => x.Id == item.ProductId);

            if (product == null)
            {
                notification.AddErrorToastMessage("اطلاعات محصول یافت نشد");
                return RedirectToAction("ShopCart");
            }

            // محاسبه مبلغ آیتم
            long rawPrice = product.RawPrice * item.count;
            long discountPrice = rawPrice - item.TotalPrice;

            // کم کردن مبالغ از سبد خرید
            shopCart.TotalRawPrice -= rawPrice;
            shopCart.TotalPrice -= item.TotalPrice;
            shopCart.DiscountPrice -= discountPrice;

            // حذف آیتم از سبد خرید
            ShopCardItemRepo.Delete(item);

            // ذخیره تغییرات سبد خرید
            ShopCartRepo.Update(shopCart);

            notification.AddSuccessToastMessage("محصول با موفقیت از سبد خرید حذف شد");

            return RedirectToAction("ShopCart");
        }



        // دریافت و بررسی آدرس و روش ارسال و ثبت سفارش مشتری
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(CheckoutDto model)
        {
            if (User.Identity.IsAuthenticated == false)
            {
                return RedirectToAction("Login", "ClientLogin");
            }

            var user = await userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction("Login", "ClientLogin");
            }

            // ساخت آدرس بازگشت از درگاه پرداخت
            var callbackUrl = Url.Action(
                "Callback",
                "Payment",
                null,
                Request.Scheme);

            if (string.IsNullOrWhiteSpace(callbackUrl))
            {
                notification.AddErrorToastMessage(
                    "ساخت آدرس بازگشت از درگاه پرداخت امکان‌پذیر نبود.");

                return RedirectToAction("Index", "Home");
            }

            // بررسی انتخاب آدرس توسط مشتری
            if (model.UserAddressId == Guid.Empty)
            {
                notification.AddErrorToastMessage("لطفاً آدرس خود را انتخاب کنید");
                return RedirectToAction("Checkout");
            }

            var address = await UserAddressRepo.TableNoTracking
                .FirstOrDefaultAsync(x =>
                    x.Id == model.UserAddressId &&
                    x.UserId == user.Id);

            if (address == null)
            {
                notification.AddErrorToastMessage("آدرس انتخاب‌شده معتبر نیست");
                return RedirectToAction("Checkout");
            }

            // بررسی روش ارسال انتخاب‌شده
            var shipping = await ShippingTypeRepo.TableNoTracking
                .FirstOrDefaultAsync(x => x.Id == model.ShippingTypeId);

            if (shipping == null || shipping.Price < 0)
            {
                notification.AddErrorToastMessage("روش ارسال معتبر نیست");
                return RedirectToAction("Checkout");
            }

            // دریافت سبد خرید مشتری
            var shopCart = await ShopCartRepo.TableNoTracking
                .FirstOrDefaultAsync(x => x.UserId == user.Id);

            if (shopCart == null)
            {
                notification.AddErrorToastMessage("سبد خرید شما یافت نشد");
                return RedirectToAction("ShopCart");
            }

            // شروع تراکنش برای جلوگیری از ثبت سفارش هم‌زمان
            using var transaction = await _context.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable);

            // دریافت مجدد اقلام سبد خرید داخل تراکنش
            var cartItems = await ShopCardItemRepo.TableNoTracking
                .Where(x => x.ShopCartId == shopCart.Id)
                .ToListAsync();

            // بررسی خالی بودن سبد خرید
            if (!cartItems.Any())
            {
                notification.AddErrorToastMessage("سبد خرید شما خالی است");
                return RedirectToAction("ShopCart");
            }

            // ایجاد کد سفارش
            var orderCode = "RZ-" + DateTime.Now.ToString("yyyyMMddHHmmss");

            // ایجاد سفارش اولیه
            var order = new Order
            {
                Code = orderCode,
                IsPaid = false,

                Shipping = shipping.Name,
                ShippingPrice = shipping.Price,

                RawPrice = 0,
                Discount = 0,
                Total = 0,

                UserId = user.Id,

                ReceiverName = address.ReceiverName,
                ReceiverPhone = address.ReceiverPhone,

                Province = address.Province,
                City = address.City,
                Address = address.Address,
                PostalCode = address.PostalCode
            };

            OrderRepo.Add(order);

            // متغیرهای محاسبه مبلغ نهایی سفارش
            long totalRawPrice = 0;
            long totalDiscount = 0;
            long totalProductsPrice = 0;

            // پیمایش محصولات سبد خرید و ثبت اقلام سفارش
            foreach (var cartItem in cartItems)
            {
                // دریافت محصول به صورت Tracked
                var product = await ProductRepo.Table
                    .FirstOrDefaultAsync(x => x.Id == cartItem.ProductId);

                if (product == null)
                {
                    notification.AddErrorToastMessage(
                        "یکی از محصولات سبد خرید دیگر موجود نیست");

                    return RedirectToAction("ShopCart");
                }

                // بررسی تعداد درخواستی مشتری و موجودی انبار
                if (cartItem.count <= 0 || cartItem.count > product.Count)
                {
                    notification.AddErrorToastMessage(
                        $"موجودی محصول {product.Title} کافی نیست");

                    return RedirectToAction("ShopCart");
                }

                // بررسی معتبر بودن قیمت محصول
                if (product.RawPrice < 0 ||
                    product.Discount < 0 ||
                    product.Discount > 100 ||
                    product.Total < 0)
                {
                    notification.AddErrorToastMessage(
                        $"اطلاعات قیمت محصول {product.Title} معتبر نیست");

                    return RedirectToAction("ShopCart");
                }

                // محاسبه مبلغ تخفیف یک واحد محصول
                long discountPerUnit = (long)(
                    product.RawPrice * product.Discount / 100m);

                // محاسبه قیمت نهایی یک واحد محصول
                long expectedTotal = product.RawPrice - discountPerUnit;

                // بررسی تطابق قیمت نهایی با قیمت و تخفیف
                if (product.Total != expectedTotal)
                {
                    notification.AddErrorToastMessage(
                        $"قیمت نهایی محصول {product.Title} با اطلاعات تخفیف مطابقت ندارد");

                    return RedirectToAction("ShopCart");
                }

                // محاسبه مبالغ کل سفارش
                totalRawPrice += product.RawPrice * cartItem.count;
                totalDiscount += discountPerUnit * cartItem.count;
                totalProductsPrice += product.Total * cartItem.count;

                // ایجاد قلم سفارش
                var orderItem = new OrderItem
                {
                    OrderId = order.Id,
                    ProductId = product.Id,

                    Count = cartItem.count,

                    ProductPrice = product.RawPrice,
                    Discount = discountPerUnit,

                    Total = product.Total * cartItem.count
                };

                OrderItemRepo.Add(orderItem);

                // کاهش موجودی محصول
                product.Count -= cartItem.count;

                ProductRepo.Update(product);
            }

            // به‌روزرسانی مبالغ نهایی سفارش
            order.RawPrice = totalRawPrice;
            order.Discount = totalDiscount;
            order.Total = totalProductsPrice + shipping.Price;

            OrderRepo.Update(order);

            // حذف اقلام سبد خرید پس از ثبت سفارش
            foreach (var item in cartItems)
            {
                ShopCardItemRepo.Delete(item, saveNow: false);
            }

            // صفر کردن مبالغ سبد خرید
            shopCart.TotalRawPrice = 0;
            shopCart.TotalPrice = 0;
            shopCart.DiscountPrice = 0;

            ShopCartRepo.Update(shopCart, saveNow: false);

            // ذخیره تغییرات دیتابیس
            await _context.SaveChangesAsync();

            // نهایی کردن تراکنش دیتابیس
            await transaction.CommitAsync();

            // ارسال درخواست پرداخت به درگاه
            var paymentResult = await _paymentService.CreateOrderPaymentAsync(
                order.Id,
                user.Id,
                callbackUrl);

            // بررسی آدرس پرداخت
            if (!string.IsNullOrWhiteSpace(paymentResult.PaymentUrl))
            {
                if (Uri.TryCreate(
                    paymentResult.PaymentUrl,
                    UriKind.Absolute,
                    out var paymentUri) &&
                    (paymentUri.Scheme == Uri.UriSchemeHttps ||
                     paymentUri.Scheme == Uri.UriSchemeHttp))
                {
                    return Redirect(paymentUri.ToString());
                }

                notification.AddErrorToastMessage(
                    "آدرس درگاه پرداخت معتبر نیست.");
            }
            else if (paymentResult.IsSuccess)
            {
                notification.AddSuccessToastMessage(
                    paymentResult.Message ?? "این سفارش قبلاً پرداخت شده است.");
            }
            else
            {
                notification.AddErrorToastMessage(
                    paymentResult.Message ??
                    "در حال حاضر امکان شروع پرداخت وجود ندارد. وضعیت سفارش حفظ شده است.");
            }

            return RedirectToAction("Index", "Home");
        }
    }
}