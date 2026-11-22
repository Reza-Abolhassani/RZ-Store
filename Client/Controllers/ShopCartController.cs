using Client.Dto;
using Data.Repositories;
using Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NToastNotify;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Client.Controllers
{
    public class ShopCartController : Controller
    {
        private readonly IRepository<ShopCart> ShopCartRepo;
        private readonly IRepository<ShopCardItem> ShopCardItemRepo;
        private readonly IRepository<Product> ProductRepo;

        private readonly UserManager<User> userManager;
        private readonly IToastNotification notification;

        public ShopCartController(
            IRepository<ShopCart> shopCartRepo,
            IRepository<ShopCardItem> shopCardItemRepo,
            IRepository<Product> productRepo,
            UserManager<User> userManager,
            IToastNotification notification)
        {
            ShopCartRepo = shopCartRepo;
            ShopCardItemRepo = shopCardItemRepo;
            ProductRepo = productRepo;
            this.userManager = userManager;
            this.notification = notification;
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

            int rawPrice = product.RawPrice * count;
            int totalPrice = product.Total * count;
            int discountPrice = rawPrice - totalPrice;

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
            int oldRawPrice = product.RawPrice * item.count;
            int oldTotalPrice = item.TotalPrice;
            int oldDiscountPrice = oldRawPrice - oldTotalPrice;

            // حذف قیمت قبلی از جمع سبد
            shopCart.TotalRawPrice -= oldRawPrice;
            shopCart.TotalPrice -= oldTotalPrice;
            shopCart.DiscountPrice -= oldDiscountPrice;

            // محاسبه قیمت جدید
            int newRawPrice = product.RawPrice * count;
            int newTotalPrice = product.Total * count;
            int newDiscountPrice = newRawPrice - newTotalPrice;

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
            int rawPrice = product.RawPrice * item.count;
            int discountPrice = rawPrice - item.TotalPrice;

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
    }
}