using Admin.Models;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Data.Repositories;
using EfCoreSetUp1618.Tools;
using Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NToastNotify;
using System;
using System.Threading.Tasks;

using System.IO;

namespace Admin.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ProductController : Controller
    {
        private readonly IRepository<Product> ProductRepo;
        private readonly IRepository<Category> CategoryRepo;
        private readonly IMapper mapper;
        private readonly IToastNotification notification;

        private readonly IWebHostEnvironment environment;

        private string SharedImagesPath =>
    Path.GetFullPath(
        Path.Combine(environment.ContentRootPath, "..", "SharedImages")
    );

        public ProductController(IRepository<Product> productRepo, IRepository<Category> categoryRepo, IMapper mapper, IToastNotification notification, IWebHostEnvironment environment)
        {
            ProductRepo = productRepo;
            CategoryRepo = categoryRepo;
            this.mapper = mapper;
            this.notification = notification;
            this.environment = environment;
        }

        public async Task<IActionResult> Add()
        {
            var model = new ProductDto();

            model.categories = await CategoryRepo.TableNoTracking
                .ProjectTo<CategoryDto>(mapper.ConfigurationProvider).ToListAsync();


            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> Add(ProductDto dto)
        {
            var model = dto.ToEntity(mapper);

            if (dto.ImageFile != null)
            {
                //model.ImageUrl = FileTool.SaveFile(dto.ImageFile, "ProductImages");

                model.ImageUrl = FileTool.SaveFile(dto.ImageFile, "ProductImage", SharedImagesPath);
            }

            model.CreationDateTime = DateTime.Now;

            ProductRepo.Add(model);

            notification.AddSuccessToastMessage("محصول اضافه شد");

            return RedirectToAction("List");
        }

        public async Task<IActionResult> List()
        {
            var model = await ProductRepo.TableNoTracking
                .ProjectTo<ProductDto>(mapper.ConfigurationProvider).ToListAsync();

           

            return View(model);
        }
        
        public async Task<IActionResult> Update(Guid Id)
        {
            var model = await ProductRepo.TableNoTracking
                .ProjectTo<ProductDto>(mapper.ConfigurationProvider)
                .FirstOrDefaultAsync(x => x.Id == Id);

            if (model == null)
            {
                notification.AddErrorToastMessage("محصول مورد نظر یافت نشد");
                return RedirectToAction("List");

            }

            var category = await CategoryRepo.TableNoTracking
                .ProjectTo<CategoryDto>(mapper.ConfigurationProvider).ToListAsync();

            model.categories = category;

            return View(model);

        }
        [HttpPost]
        public async Task<IActionResult> Update(ProductDto dto)
        {
            if (dto.Id != Guid.Empty)
            {
                var model = await ProductRepo.AllTableNoTracking
                    .FirstOrDefaultAsync(x => x.Id == dto.Id);

                if (model == null)
                {
                    notification.AddErrorToastMessage("محصول مورد نظر یافت نشد");
                    return RedirectToAction("List");
                }

                // مقداردهی Propertyهای محصول
                model.Title = dto.Title;
                model.Count = dto.Count;
                model.Code = dto.Code;
                model.Description = dto.Description;
                model.RawPrice = dto.RawPrice;
                model.Discount = dto.Discount;
                model.Total = dto.Total;
                model.SKU = dto.SKU;
                model.CategoryId = dto.CategoryId;

                // بررسی و ذخیره تصویر جدید
                if (dto.ImageFile != null)
                {
                    model.ImageUrl = FileTool.SaveFile(dto.ImageFile, "ProductImage", SharedImagesPath);
                }

                // به‌روزرسانی محصول
                ProductRepo.Update(model);

                notification.AddSuccessToastMessage("محصول با موفقیت ویرایش شد");
            }

            return RedirectToAction("List");
        }

        public async Task<IActionResult> Delete(Guid Id)
        {
            var model = await ProductRepo.AllTableNoTracking
                .FirstOrDefaultAsync(x => x.Id == Id);

            if (model == null)
            {
                notification.AddErrorToastMessage("محصول مورد نظر یافت نشد");
                return RedirectToAction("List");
            }

            ProductRepo.Delete(model);

            notification.AddSuccessToastMessage("محصول با موفقیت حذف شد");

            return RedirectToAction("List");
        }
    }
}
