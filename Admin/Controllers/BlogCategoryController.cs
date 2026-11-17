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

namespace Admin.Controllers
{
    [Authorize(Roles = "Admin")]
    public class BlogCategoryController : Controller
    {
        private readonly IRepository<BlogCategory> BlogCategoryRepo;
        private readonly IMapper mapper;
        private readonly IToastNotification notification;

        public BlogCategoryController(
            IRepository<BlogCategory> blogCategoryRepo,
            IMapper mapper,
            IToastNotification notification)
        {
            BlogCategoryRepo = blogCategoryRepo;
            this.mapper = mapper;
            this.notification = notification;
        }

        public async Task<IActionResult> List()
        {
            var model = await BlogCategoryRepo.TableNoTracking
                .ProjectTo<BlogCategoryDto>(
                    mapper.ConfigurationProvider)
                .ToListAsync();

            return View(model);
        }

        public IActionResult Add()
        {
            return View(new BlogCategoryDto());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(BlogCategoryDto dto)
        {
            if (!ModelState.IsValid)
                return View(dto);

            var model = dto.ToEntity(mapper);

            BlogCategoryRepo.Add(model);

            notification.AddSuccessToastMessage(
                "دسته‌بندی مقاله با موفقیت اضافه شد");

            return RedirectToAction("List");
        }

        public async Task<IActionResult> Update(Guid Id)
        {
            var model = await BlogCategoryRepo.TableNoTracking
                .ProjectTo<BlogCategoryDto>(
                    mapper.ConfigurationProvider)
                .FirstOrDefaultAsync(x => x.Id == Id);

            if (model == null)
            {
                notification.AddErrorToastMessage(
                    "دسته‌بندی مورد نظر یافت نشد");

                return RedirectToAction("List");
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(BlogCategoryDto dto)
        {
            if (!ModelState.IsValid)
                return View(dto);

            if (dto.Id == Guid.Empty)
                return RedirectToAction("List");

            var model = await BlogCategoryRepo.AllTableNoTracking
                .FirstOrDefaultAsync(x => x.Id == dto.Id);

            if (model == null)
            {
                notification.AddErrorToastMessage(
                    "دسته‌بندی مورد نظر یافت نشد");

                return RedirectToAction("List");
            }

            model.Name = dto.Name;

            BlogCategoryRepo.Update(model);

            notification.AddSuccessToastMessage(
                "دسته‌بندی با موفقیت ویرایش شد");

            return RedirectToAction("List");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid Id)
        {
            var model = await BlogCategoryRepo.AllTableNoTracking
                .FirstOrDefaultAsync(x => x.Id == Id);

            if (model == null)
            {
                notification.AddErrorToastMessage(
                    "دسته‌بندی مورد نظر یافت نشد");

                return RedirectToAction("List");
            }

            BlogCategoryRepo.Delete(model);

            notification.AddSuccessToastMessage(
                "دسته‌بندی با موفقیت حذف شد");

            return RedirectToAction("List");
        }
    }
}