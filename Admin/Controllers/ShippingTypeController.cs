using Admin.Models;
using AutoMapper;
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
    public class ShippingTypeController : Controller
    {
        private readonly IRepository<ShippingType> shippingTypeRepo;
        private readonly IMapper mapper;
        private readonly IToastNotification notification;

        public ShippingTypeController(
            IRepository<ShippingType> shippingTypeRepo,
            IMapper mapper,
            IToastNotification notification)
        {
            this.shippingTypeRepo = shippingTypeRepo;
            this.mapper = mapper;
            this.notification = notification;
        }

        // نمایش لیست روش های ارسال
        public async Task<IActionResult> List()
        {
            var shippingTypes = await shippingTypeRepo
                .AllTableNoTracking
                .ToListAsync();

            var model = mapper.Map<List<ShippingTypeDto>>(shippingTypes);

            return View(model);
        }

        // نمایش فرم افزودن
        [HttpGet]
        public IActionResult Add()
        {
            return View(new ShippingTypeDto());
        }

        // ثبت روش ارسال
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(ShippingTypeDto dto)
        {
            if (!ModelState.IsValid ||
                string.IsNullOrWhiteSpace(dto.Name) ||
                dto.Price < 0)
            {
                notification.AddErrorToastMessage(
                    "اطلاعات وارد شده معتبر نیست.");

                return View(dto);
            }

            var model = mapper.Map<ShippingType>(dto);

            await shippingTypeRepo.AddAsync(
                model, HttpContext.RequestAborted);

            notification.AddSuccessToastMessage(
                "روش ارسال با موفقیت ثبت شد.");

            return RedirectToAction("List");
        }

        // نمایش فرم ویرایش
        [HttpGet]
        public async Task<IActionResult> Update(Guid id)
        {
            var shippingType = await shippingTypeRepo
                .TableNoTracking
                .FirstOrDefaultAsync(x => x.Id == id);

            if (shippingType == null)
            {
                notification.AddErrorToastMessage(
                    "روش ارسال مورد نظر پیدا نشد.");

                return RedirectToAction("List");
            }

            var model = mapper.Map<ShippingTypeDto>(shippingType);

            return View(model);
        }

        // ثبت تغییرات
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(ShippingTypeDto dto)
        {
            if (!ModelState.IsValid ||
                string.IsNullOrWhiteSpace(dto.Name) ||
                dto.Price < 0)
            {
                notification.AddErrorToastMessage(
                    "اطلاعات وارد شده معتبر نیست.");

                return View(dto);
            }

            var model = await shippingTypeRepo.Table
                .FirstOrDefaultAsync(x => x.Id == dto.Id);

            if (model == null)
            {
                notification.AddErrorToastMessage(
                    "روش ارسال مورد نظر پیدا نشد.");

                return RedirectToAction("List");
            }

            model.Name = dto.Name;
            model.Price = dto.Price;

            await shippingTypeRepo.UpdateAsync(
                model, HttpContext.RequestAborted);

            notification.AddSuccessToastMessage(
                "روش ارسال با موفقیت ویرایش شد.");

            return RedirectToAction("List");
        }

        // حذف روش ارسال
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var model = await shippingTypeRepo.Table
                .FirstOrDefaultAsync(x => x.Id == id);

            if (model == null)
            {
                notification.AddErrorToastMessage(
                    "روش ارسال مورد نظر پیدا نشد.");

                return RedirectToAction("List");
            }

            await shippingTypeRepo.DeleteAsync(
                model, HttpContext.RequestAborted);

            notification.AddSuccessToastMessage(
                "روش ارسال با موفقیت حذف شد.");

            return RedirectToAction("List");
        }
    }
}