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
using System.Linq;
using System.Threading.Tasks;

namespace Admin.Controllers
{
    [Authorize(Roles = "Admin")]
    public class HomeBannerController : Controller
    {
        private readonly IRepository<HomeBanner> HomeBannerRepo;
        private readonly IMapper mapper;
        private readonly IToastNotification notification;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public HomeBannerController(
            IRepository<HomeBanner> homeBannerRepo,
            IMapper mapper,
            IToastNotification notification,
            IWebHostEnvironment webHostEnvironment)
        {
            HomeBannerRepo = homeBannerRepo;
            this.mapper = mapper;
            this.notification = notification;
            _webHostEnvironment = webHostEnvironment;
        }

        // Add - Get
        public IActionResult Add()
        {
            return View();
        }

        // Add - Post
        [HttpPost]
        public IActionResult Add(CreateHomeBannerDto dto)
        {
            if (!ModelState.IsValid)
                return View(dto);

            var model = dto.ToEntity(mapper);

            if (dto.ImageFile != null)
            {
                model.ImageUrl = FileTool.SaveFile(dto.ImageFile, "HomeBannerImage", _webHostEnvironment.WebRootPath);
            }

            HomeBannerRepo.Add(model);

            notification.AddSuccessToastMessage("بنر با موفقیت ثبت شد.");

            return RedirectToAction("List");
        }

        // List
        public IActionResult List()
        {
            var model = HomeBannerRepo.AllTableNoTracking
                .OrderBy(x => x.Position)
                .ThenBy(x => x.Order)
                .ToList();

            var dto = mapper.Map<List<HomeBannerDto>>(model);

            return View(dto);
        }

        // Update - Get
        public async Task<IActionResult> Update(Guid id)
        {
            var model = await HomeBannerRepo.AllTableNoTracking.FirstOrDefaultAsync(x => x.Id == id);

            if (model == null)
                return NotFound();

            var dto = EditHomeBannerDto.FromEntity(mapper, model);

            return View(dto);
        }

        // Update - Post
        [HttpPost]
        public async Task<IActionResult> Update(EditHomeBannerDto dto)
        {
            if (!ModelState.IsValid)
                return View(dto);

            var model = await HomeBannerRepo.AllTableNoTracking
                .FirstOrDefaultAsync(x => x.Id == dto.Id);

            if (model == null)
                return NotFound();

            model.Title = dto.Title;
            model.Description = dto.Description;
            model.Link = dto.Link;
            model.ButtonText = dto.ButtonText;
            model.Position = dto.Position;
            model.Order = dto.Order;
            model.IsActive = dto.IsActive;

            if (dto.ImageFile != null)
            {
                model.ImageUrl = FileTool.SaveFile(
                    dto.ImageFile,
                    "HomeBannerImage",
                    _webHostEnvironment.WebRootPath
                );
            }

            HomeBannerRepo.Update(model);

            notification.AddSuccessToastMessage("بنر با موفقیت ویرایش شد.");

            return RedirectToAction("List");
        }

        // Delete
        public async Task<IActionResult> Delete(Guid id)
        {
            var model = await HomeBannerRepo.AllTableNoTracking
                .FirstOrDefaultAsync(x => x.Id == id);

            if (model == null)
                return NotFound();

            HomeBannerRepo.Delete(model);

            notification.AddSuccessToastMessage("بنر با موفقیت حذف شد.");

            return RedirectToAction("List");
        }
    }
}