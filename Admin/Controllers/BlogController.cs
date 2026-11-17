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
using System.Threading.Tasks;

using EfCoreSetUp1618.Tools;
using Microsoft.AspNetCore.Hosting;

namespace Admin.Controllers
{
    [Authorize(Roles = "Admin")]
    public class BlogController : Controller
    {
        private readonly IRepository<Blog> BlogRepo;
        private readonly IRepository<BlogCategory> BlogCategoryRepo;
        private readonly IMapper mapper;
        private readonly IToastNotification notification;

        private readonly IWebHostEnvironment environment;

        public BlogController(
            IRepository<Blog> blogRepo,
            IRepository<BlogCategory> blogCategoryRepo,
            IMapper mapper,
            IToastNotification notification,
            IWebHostEnvironment environment)
        {
            BlogRepo = blogRepo;
            BlogCategoryRepo = blogCategoryRepo;
            this.mapper = mapper;
            this.notification = notification;
            this.environment = environment;
        }

        public async Task<IActionResult> List()
        {
            var model = await BlogRepo.TableNoTracking
                .ProjectTo<BlogDto>(mapper.ConfigurationProvider)
                .ToListAsync();

            return View(model);
        }

        public async Task<IActionResult> Add()
        {
            var model = new BlogDto();

            model.BlogCategories = await BlogCategoryRepo.TableNoTracking
                .ProjectTo<BlogCategoryDto>(mapper.ConfigurationProvider)
                .ToListAsync();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(BlogDto dto)
        {
            if (!ModelState.IsValid)
            {
                dto.BlogCategories = await BlogCategoryRepo.TableNoTracking
                    .ProjectTo<BlogCategoryDto>(mapper.ConfigurationProvider)
                    .ToListAsync();

                return View(dto);
            }

            var model = dto.ToEntity(mapper);

            if (dto.ImageFile != null)
            {
                model.ImageUrl = FileTool.SaveFile(
                    dto.ImageFile,
                    "BlogImage",
                    environment.WebRootPath);
            }

            model.CreationDate = DateTime.Now;
            model.UpdateDate = DateTime.Now;

            BlogRepo.Add(model);

            notification.AddSuccessToastMessage(
                "مقاله با موفقیت اضافه شد");

            return RedirectToAction("List");
        }

        public async Task<IActionResult> Update(Guid Id)
        {
            var model = await BlogRepo.TableNoTracking
                .ProjectTo<BlogDto>(mapper.ConfigurationProvider)
                .FirstOrDefaultAsync(x => x.Id == Id);

            if (model == null)
            {
                notification.AddErrorToastMessage(
                    "مقاله مورد نظر یافت نشد");

                return RedirectToAction("List");
            }

            model.BlogCategories = await BlogCategoryRepo.TableNoTracking
                .ProjectTo<BlogCategoryDto>(mapper.ConfigurationProvider)
                .ToListAsync();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(BlogDto dto)
        {
            if (!ModelState.IsValid)
            {
                dto.BlogCategories = await BlogCategoryRepo.TableNoTracking
                    .ProjectTo<BlogCategoryDto>(mapper.ConfigurationProvider)
                    .ToListAsync();

                return View(dto);
            }

            if (dto.Id == Guid.Empty)
                return RedirectToAction("List");

            var model = await BlogRepo.AllTableNoTracking
                .FirstOrDefaultAsync(x => x.Id == dto.Id);

            if (model == null)
            {
                notification.AddErrorToastMessage(
                    "مقاله مورد نظر یافت نشد");

                return RedirectToAction("List");
            }

            model.Title = dto.Title;
            model.Content = dto.Content;
            model.BlogCategoryId = dto.BlogCategoryId;
            model.Author = dto.Author;
            model.UpdateDate = DateTime.Now;

            if (dto.ImageFile != null)
            {
                model.ImageUrl = FileTool.SaveFile(
                    dto.ImageFile,
                    "BlogImage",
                    environment.WebRootPath);
            }

            BlogRepo.Update(model);

            notification.AddSuccessToastMessage(
                "مقاله با موفقیت ویرایش شد");

            return RedirectToAction("List");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid Id)
        {
            var model = await BlogRepo.AllTableNoTracking
                .FirstOrDefaultAsync(x => x.Id == Id);

            if (model == null)
            {
                notification.AddErrorToastMessage(
                    "مقاله مورد نظر یافت نشد");

                return RedirectToAction("List");
            }

            BlogRepo.Delete(model);

            notification.AddSuccessToastMessage(
                "مقاله با موفقیت حذف شد");

            return RedirectToAction("List");
        }
    }
}