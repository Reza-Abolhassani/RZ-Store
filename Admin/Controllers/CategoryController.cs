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
using System.Linq;
using System.Threading.Tasks;

namespace Admin.Controllers
{
    [Authorize(Roles = "Admin")]
    public class CategoryController : Controller
    {
        private readonly IRepository<Category> CategoryRepo;
        private readonly IMapper mapper;
        private readonly IToastNotification notification;

        private readonly IWebHostEnvironment environment;

        public CategoryController(IRepository<Category> categoryRepo, IMapper mapper, IToastNotification notification, IWebHostEnvironment environment)
        {
            CategoryRepo = categoryRepo;
            this.mapper = mapper;
            this.notification = notification;
            this.environment = environment;
        }

        public async Task<IActionResult> Add()
        {


            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]//این ویژگی برای محافظت از فرم در برابر درخواست‌های جعلی استفاده می‌شود.
        public async Task<IActionResult> Add(CategoryDto dto)
        {

            if (!ModelState.IsValid)
            {
                return View(dto);
            }
            var model = dto.ToEntity(mapper);

            if (dto.ImageFile != null)
            {
                //model.ImageUrl = FileTool.SaveFile(dto.ImageFile, "CategoryImage");

                model.ImageUrl = FileTool.SaveFile(dto.ImageFile,"CategoryImage",environment.WebRootPath);
            }

            model.CreationDateTime = DateTime.Now;

            CategoryRepo.Add(model);

            return RedirectToAction("List");
        }
        public async Task<IActionResult> List()
        {
            var model = await CategoryRepo.AllTableNoTracking
                .ProjectTo<CategoryDto>(mapper.ConfigurationProvider)
                .ToListAsync();

            return View(model);
        }

        public async Task<IActionResult> Update(Guid Id)
        {

            var model = await CategoryRepo.AllTableNoTracking
                .ProjectTo<CategoryDto>(mapper.ConfigurationProvider)
                .FirstOrDefaultAsync(x => x.Id == Id);

            return View(model);

        }
        [HttpPost]
        public async Task<IActionResult> Update(CategoryDto dto)
        {
            if (dto.Id != Guid.Empty)
            {
                var model = await CategoryRepo.AllTableNoTracking.FirstOrDefaultAsync(x => x.Id == dto.Id);

                model.Title = dto.Title;
                model.Order = dto.Order;
                model.Description = dto.Description;


                if (dto.ImageFile != null)
                {
                    //model.ImageUrl = FileTool.SaveFile(dto.ImageFile, "CategoryImage");

                    model.ImageUrl = FileTool.SaveFile(dto.ImageFile, "CategoryImage", environment.WebRootPath);
                }

                CategoryRepo.Update(model);
            }
            return RedirectToAction("List");

        }


        public async Task<IActionResult> Delete(Guid Id)
        {

            var category = await CategoryRepo.AllTableNoTracking
                .FirstOrDefaultAsync(x => x.Id == Id);

            //if (category == null)
            //{
            //    return NotFound();
            //}

            if (category == null)
            {
                notification.AddErrorToastMessage("دسته‌بندی موردنظر یافت نشد.");
                return RedirectToAction("List");
            }

            CategoryRepo.Delete(category);


            notification.AddSuccessToastMessage(".حذف  با موفقیت انجام شد");


            return RedirectToAction("List");
        }

    }
}
