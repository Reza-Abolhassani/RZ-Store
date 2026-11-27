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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Admin.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AmazingProductsController : Controller
    {
        private readonly IRepository<AmazingProduct> amazingProductRepo;
        private readonly IRepository<Product> productRepo;
        private readonly IMapper mapper;
        private readonly IToastNotification notification;

        public AmazingProductsController(
            IRepository<AmazingProduct> amazingProductRepo,
            IRepository<Product> productRepo,
            IMapper mapper,
            IToastNotification notification)
        {
            this.amazingProductRepo = amazingProductRepo;
            this.productRepo = productRepo;
            this.mapper = mapper;
            this.notification = notification;
        }

        // نمایش لیست محصولات شگفت انگیز
        public async Task<IActionResult> List()
        {
            var products = await amazingProductRepo
                .AllTableNoTracking
                .Include(x => x.Product)
                .ToListAsync();

            var model = mapper.Map<List<AmazingProductsDto>>(products);

            foreach (var item in model)
            {
                var product = products
                    .FirstOrDefault(x => x.Id == item.Id)?.Product;

                if (product != null)
                {
                    item.ProductsName = product.Title;
                    item.ProductsTotalPrice = product.Total;
                }
            }

            return View(model);
        }

        // نمایش فرم افزودن
        [HttpGet]
        public async Task<IActionResult> Add()
        {
            var model = new AmazingProductsDto();

            model.Products = await productRepo
                .AllTableNoTracking
                .ProjectTo<ProductDto>(mapper.ConfigurationProvider)
                .ToListAsync();

            return View(model);
        }

        // ثبت محصول شگفت انگیز
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(AmazingProductsDto dto)
        {
            if (!ModelState.IsValid)
            {
                dto.Products = await productRepo
                    .AllTableNoTracking
                    .ProjectTo<ProductDto>(mapper.ConfigurationProvider)
                    .ToListAsync();

                notification.AddErrorToastMessage("اطلاعات وارد شده معتبر نیست.");
                return View(dto);
            }

            if (dto.DiscountPercent < 0 || dto.DiscountPercent > 100)
            {
                dto.Products = await productRepo
                    .AllTableNoTracking
                    .ProjectTo<ProductDto>(mapper.ConfigurationProvider)
                    .ToListAsync();

                notification.AddErrorToastMessage("درصد تخفیف باید بین صفر تا صد باشد.");
                return View(dto);
            }

            var product = await productRepo.TableNoTracking
                .FirstOrDefaultAsync(x => x.Id == dto.ProductId);

            if (product == null)
            {
                dto.Products = await productRepo
                    .AllTableNoTracking
                    .ProjectTo<ProductDto>(mapper.ConfigurationProvider)
                    .ToListAsync();

                notification.AddErrorToastMessage("محصول مورد نظر پیدا نشد.");
                return View(dto);
            }

            var exists = await amazingProductRepo.TableNoTracking
                .AnyAsync(x => x.ProductId == dto.ProductId);

            if (exists)
            {
                dto.Products = await productRepo
                    .AllTableNoTracking
                    .ProjectTo<ProductDto>(mapper.ConfigurationProvider)
                    .ToListAsync();

                notification.AddErrorToastMessage("این محصول قبلاً به محصولات شگفت انگیز اضافه شده است.");
                return View(dto);
            }

            var model = mapper.Map<AmazingProduct>(dto);

            model.ToatalPrice = (long)Math.Round(
    product.Total * (100m - dto.DiscountPercent) / 100m);

            amazingProductRepo.Add(model);

            notification.AddSuccessToastMessage("محصول شگفت انگیز با موفقیت اضافه شد.");

            return RedirectToAction("List");
        }

        // نمایش فرم ویرایش
        [HttpGet]
        public async Task<IActionResult> Update(Guid id)
        {
            var amazingProduct = await amazingProductRepo.TableNoTracking
                .FirstOrDefaultAsync(x => x.Id == id);

            if (amazingProduct == null)
            {
                notification.AddErrorToastMessage("محصول شگفت انگیز پیدا نشد.");
                return RedirectToAction("List");
            }

            var model = mapper.Map<AmazingProductsDto>(amazingProduct);

            model.Products = await productRepo
                .AllTableNoTracking
                .ProjectTo<ProductDto>(mapper.ConfigurationProvider)
                .ToListAsync();

            return View(model);
        }

        // ثبت تغییرات
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(AmazingProductsDto dto)
        {
            if (!ModelState.IsValid)
            {
                dto.Products = await productRepo
                    .AllTableNoTracking
                    .ProjectTo<ProductDto>(mapper.ConfigurationProvider)
                    .ToListAsync();

                notification.AddErrorToastMessage("اطلاعات وارد شده معتبر نیست.");
                return View(dto);
            }

            if (dto.DiscountPercent < 0 || dto.DiscountPercent > 100)
            {
                dto.Products = await productRepo
                    .AllTableNoTracking
                    .ProjectTo<ProductDto>(mapper.ConfigurationProvider)
                    .ToListAsync();

                notification.AddErrorToastMessage("درصد تخفیف باید بین صفر تا صد باشد.");
                return View(dto);
            }

            var model = await amazingProductRepo.Table
                .FirstOrDefaultAsync(x => x.Id == dto.Id);

            if (model == null)
            {
                notification.AddErrorToastMessage("محصول شگفت انگیز پیدا نشد.");
                return RedirectToAction("List");
            }

            var product = await productRepo.TableNoTracking
                .FirstOrDefaultAsync(x => x.Id == dto.ProductId);

            if (product == null)
            {
                dto.Products = await productRepo
                    .AllTableNoTracking
                    .ProjectTo<ProductDto>(mapper.ConfigurationProvider)
                    .ToListAsync();

                notification.AddErrorToastMessage("محصول انتخاب شده پیدا نشد.");
                return View(dto);
            }

            var exists = await amazingProductRepo.TableNoTracking
                .AnyAsync(x => x.ProductId == dto.ProductId && x.Id != dto.Id);

            if (exists)
            {
                dto.Products = await productRepo
                    .AllTableNoTracking
                    .ProjectTo<ProductDto>(mapper.ConfigurationProvider)
                    .ToListAsync();

                notification.AddErrorToastMessage("این محصول قبلاً ثبت شده است.");
                return View(dto);
            }

            model.ProductId = dto.ProductId;
            model.DiscountPercent = dto.DiscountPercent;

            model.ToatalPrice = (long)Math.Round(
    product.Total * (100m - dto.DiscountPercent) / 100m);

            amazingProductRepo.Update(model);

            notification.AddSuccessToastMessage("اطلاعات محصول شگفت انگیز با موفقیت ویرایش شد.");

            return RedirectToAction("List");
        }

        // حذف محصول شگفت انگیز
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var model = await amazingProductRepo.Table
                .FirstOrDefaultAsync(x => x.Id == id);

            if (model == null)
            {
                notification.AddErrorToastMessage("محصول شگفت انگیز پیدا نشد.");
                return RedirectToAction("List");
            }

            amazingProductRepo.Delete(model);

            notification.AddSuccessToastMessage("محصول شگفت انگیز با موفقیت حذف شد.");

            return RedirectToAction("List");
        }
    }
}