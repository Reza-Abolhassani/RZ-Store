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
    public class DiscountController : Controller
    {
        private readonly IRepository<Discount> DiscountRepo;
        private readonly IMapper mapper;
        private readonly IToastNotification notification;

        public DiscountController(IRepository<Discount> discountRepo, IMapper mapper, IToastNotification notification)
        {
            DiscountRepo = discountRepo;
            this.mapper = mapper;
            this.notification = notification;
        }

        // نمایش فرم افزودن تخفیف
        public IActionResult Add()
        {
            var model = new DiscountDto();

            return View(model);
        }

        // ثبت تخفیف جدید
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Add(DiscountDto dto)
        {
            if (!ModelState.IsValid)
            {
                notification.AddErrorToastMessage("لطفاً اطلاعات تخفیف را به‌درستی وارد کنید");

                return View(dto);
            }

            var model = dto.ToEntity(mapper);

            model.CreationDateTime = DateTime.Now;

            DiscountRepo.Add(model);

            notification.AddSuccessToastMessage("تخفیف با موفقیت اضافه شد");

            return RedirectToAction("List");
        }

        // نمایش لیست تخفیف ها
        public async Task<IActionResult> List()
        {
            var model = await DiscountRepo.TableNoTracking
                .ProjectTo<DiscountDto>(mapper.ConfigurationProvider)
                .ToListAsync();

            return View(model);
        }

        // نمایش اطلاعات تخفیف برای ویرایش
        public async Task<IActionResult> Update(Guid Id)
        {
            var model = await DiscountRepo.TableNoTracking
                .ProjectTo<DiscountDto>(mapper.ConfigurationProvider)
                .FirstOrDefaultAsync(x => x.Id == Id);

            if (model == null)
            {
                notification.AddErrorToastMessage("تخفیف مورد نظر یافت نشد");

                return RedirectToAction("List");
            }

            return View(model);
        }

        // ویرایش تخفیف
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(DiscountDto dto)
        {
            if (!ModelState.IsValid)
            {
                notification.AddErrorToastMessage("لطفاً اطلاعات تخفیف را به‌درستی وارد کنید");

                return View(dto);
            }

            if (dto.Id != Guid.Empty)
            {
                var model = await DiscountRepo.AllTableNoTracking
                    .FirstOrDefaultAsync(x => x.Id == dto.Id);

                if (model == null)
                {
                    notification.AddErrorToastMessage("تخفیف مورد نظر یافت نشد");

                    return RedirectToAction("List");
                }

                model.Code = dto.Code;
                model.Percentage = dto.Percentage;
                model.Amount = dto.Amount;
                model.Startdate = dto.Startdate;
                model.EndDate = dto.EndDate;
                model.IsActive = dto.IsActive;

                DiscountRepo.Update(model);

                notification.AddSuccessToastMessage("تخفیف با موفقیت ویرایش شد");
            }

            return RedirectToAction("List");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid Id)
        {
            var model = await DiscountRepo.AllTableNoTracking
                .FirstOrDefaultAsync(x => x.Id == Id);

            if (model == null)
            {
                notification.AddErrorToastMessage("تخفیف مورد نظر یافت نشد");
                return RedirectToAction("List");
            }

            DiscountRepo.Delete(model);

            notification.AddSuccessToastMessage("تخفیف با موفقیت حذف شد");

            return RedirectToAction("List");
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeStatus(Guid Id)
        {
            var model = await DiscountRepo.AllTableNoTracking
                .FirstOrDefaultAsync(x => x.Id == Id);

            if (model == null)
            {
                notification.AddErrorToastMessage("تخفیف مورد نظر یافت نشد");

                return RedirectToAction("List");
            }

            model.IsActive = !model.IsActive;

            DiscountRepo.Update(model);

            notification.AddSuccessToastMessage(
                model.IsActive
                    ? "تخفیف با موفقیت فعال شد"
                    : "تخفیف با موفقیت غیرفعال شد"
            );

            return RedirectToAction("List");
        }
    }
}
