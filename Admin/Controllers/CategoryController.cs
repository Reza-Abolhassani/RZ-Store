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
using System.Collections.Generic;
using System.IO;
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



        private string SharedImagesPath =>
    Path.GetFullPath(
        Path.Combine(environment.ContentRootPath, "..", "SharedImages")
    );

        public CategoryController(
            IRepository<Category> categoryRepo,
            IMapper mapper,
            IToastNotification notification,
            IWebHostEnvironment environment)
        {
            CategoryRepo = categoryRepo;
            this.mapper = mapper;
            this.notification = notification;
            this.environment = environment;
        }

        public async Task<IActionResult> Add()
        {
            var categories = await CategoryRepo.AllTableNoTracking
                .ProjectTo<CategoryDto>(mapper.ConfigurationProvider)
                .ToListAsync();

            ViewBag.Categories = categories;

            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(CategoryDto dto)
        {
            if (!ModelState.IsValid)
            {
                var categories = await CategoryRepo.AllTableNoTracking
                    .ProjectTo<CategoryDto>(mapper.ConfigurationProvider)
                    .ToListAsync();

                ViewBag.Categories = categories;

                return View(dto);
            }

            var model = dto.ToEntity(mapper);

            if (dto.ImageFile != null)
            {
                model.ImageUrl = FileTool.SaveFile(
    dto.ImageFile,
    "CategoryImage",
    SharedImagesPath);
            }

            model.CreationDateTime = DateTime.Now;

            CategoryRepo.Add(model);

            return RedirectToAction("List");
        }
        public async Task<IActionResult> List()
        {
            var categories = await CategoryRepo.AllTableNoTracking
                .ProjectTo<CategoryDto>(mapper.ConfigurationProvider)
                .OrderBy(x => x.Order)
                .ToListAsync();

            var model = categories
                .Where(x => x.ParentId == null)
                .ToList();

            foreach (var category in model)
            {
                LoadChildren(category, categories);
            }

            return View(model);
        }

        private void LoadChildren(CategoryDto category, List<CategoryDto> categories)
        {
            category.Children = categories
                .Where(x => x.ParentId == category.Id)
                .OrderBy(x => x.Order)
                .ToList();

            foreach (var child in category.Children)
            {
                LoadChildren(child, categories);
            }
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

                    model.ImageUrl = FileTool.SaveFile(
    dto.ImageFile,
    "CategoryImage",
    SharedImagesPath);
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
