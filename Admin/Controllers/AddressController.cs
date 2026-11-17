using Admin.Models;
using AutoMapper;
using Data.Repositories;
using Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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
    public class AddressController : Controller
    {
        private readonly IRepository<Address> addressRepo;
        private readonly UserManager<User> userManager;
        private readonly IRepository<City> cityRepo;
        private readonly IRepository<Province> provinceRepo;
        private readonly IMapper mapper;
        private readonly IToastNotification notification;

        public AddressController(IRepository<Address> addressRepo, UserManager<User> userManager, IRepository<City> cityRepo, IRepository<Province> provinceRepo, IMapper mapper, IToastNotification notification)
        {
            this.addressRepo = addressRepo;
            this.userManager = userManager;
            this.cityRepo = cityRepo;
            this.provinceRepo = provinceRepo;
            this.mapper = mapper;
            this.notification = notification;
        }



        // دریافت اطلاعات لازم برای فرم
        private async Task LoadFormDataAsync(AddressDto model)
        {
            var users = await userManager.Users
                .ToListAsync();

            model.Users = mapper.Map<List<UserDto>>(users);

            var cities = await cityRepo.AllTableNoTracking
                .ToListAsync();

            model.Cities = mapper.Map<List<CityDto>>(cities);

            var provinces = await provinceRepo.TableNoTracking
               .ToListAsync();

            model.Provinces = mapper.Map<List<ProvinceDto>>(provinces);
        }

        // نمایش لیست آدرس‌ها
        public async Task<IActionResult> List()
        {
            var addresses = await addressRepo.AllTableNoTracking
                .Include(x => x.User)
                .Include(x => x.City)
                .ThenInclude(x => x.Province)
                .ToListAsync();

            var model = mapper.Map<List<AddressDto>>(addresses);

            foreach (var item in model)
            {
                var address = addresses.FirstOrDefault(x => x.Id == item.Id);

                item.UsersName = address?.User?.UserName;
                item.CitiesName = address?.City?.Name;
                item.ProvincesName = address?.City?.Province?.Name;
            }

            return View(model);
        }

        // نمایش فرم افزودن آدرس
        [HttpGet]
        public async Task<IActionResult> Add()
        {
            var model = new AddressDto();

            await LoadFormDataAsync(model);

            return View(model);
        }

        // ثبت آدرس جدید
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(AddressDto dto)
        {
            if (!ModelState.IsValid ||
                string.IsNullOrWhiteSpace(dto.Title) ||
                string.IsNullOrWhiteSpace(dto.FullAddress) ||
                string.IsNullOrWhiteSpace(dto.PostalCode) ||
                dto.UserId == Guid.Empty ||
                dto.ProvinceId == Guid.Empty ||
                dto.CityId == Guid.Empty)
            {
                notification.AddErrorToastMessage("اطلاعات وارد شده معتبر نیست.");

                await LoadFormDataAsync(dto);
                return View(dto);
            }

            var city = await cityRepo.TableNoTracking
                .FirstOrDefaultAsync(x =>
                    x.Id == dto.CityId &&
                    x.ProvinceId == dto.ProvinceId);

            var userExists = await userManager.Users
                .AnyAsync(x => x.Id == dto.UserId);

            if (city == null || !userExists)
            {
                notification.AddErrorToastMessage("کاربر یا شهر انتخاب شده معتبر نیست.");

                await LoadFormDataAsync(dto);
                return View(dto);
            }

            var model = mapper.Map<Address>(dto);

            await addressRepo.AddAsync(
                model,
                HttpContext.RequestAborted);

            notification.AddSuccessToastMessage("آدرس با موفقیت ثبت شد.");

            return RedirectToAction("List");
        }

        // نمایش فرم ویرایش آدرس
        [HttpGet]
        public async Task<IActionResult> Update(Guid id)
        {
            var address = await addressRepo.TableNoTracking
                .Include(x => x.City)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (address == null)
            {
                notification.AddErrorToastMessage("آدرس مورد نظر پیدا نشد.");

                return RedirectToAction("List");
            }

            var model = mapper.Map<AddressDto>(address);

            model.ProvinceId = address.City?.ProvinceId ?? Guid.Empty;

            await LoadFormDataAsync(model);

            return View(model);
        }

        // ذخیره تغییرات آدرس
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(AddressDto dto)
        {
            if (!ModelState.IsValid ||
                string.IsNullOrWhiteSpace(dto.Title) ||
                string.IsNullOrWhiteSpace(dto.FullAddress) ||
                string.IsNullOrWhiteSpace(dto.PostalCode) ||
                dto.UserId == Guid.Empty ||
                dto.ProvinceId == Guid.Empty ||
                dto.CityId == Guid.Empty)
            {
                notification.AddErrorToastMessage("اطلاعات وارد شده معتبر نیست.");

                await LoadFormDataAsync(dto);
                return View(dto);
            }

            var model = await addressRepo.Table
                .FirstOrDefaultAsync(x => x.Id == dto.Id);

            if (model == null)
            {
                notification.AddErrorToastMessage("آدرس مورد نظر پیدا نشد.");

                return RedirectToAction("List");
            }

            var city = await cityRepo.TableNoTracking
                .FirstOrDefaultAsync(x =>
                    x.Id == dto.CityId &&
                    x.ProvinceId == dto.ProvinceId);

            var userExists = await userManager.Users
                .AnyAsync(x => x.Id == dto.UserId);

            if (city == null || !userExists)
            {
                notification.AddErrorToastMessage("کاربر یا شهر انتخاب شده معتبر نیست.");

                await LoadFormDataAsync(dto);
                return View(dto);
            }

            model.CityId = dto.CityId;
            model.UserId = dto.UserId;
            model.Title = dto.Title;
            model.FullAddress = dto.FullAddress;
            model.PostalCode = dto.PostalCode;

            await addressRepo.UpdateAsync(
                model,
                HttpContext.RequestAborted);

            notification.AddSuccessToastMessage("آدرس با موفقیت ویرایش شد.");

            return RedirectToAction("List");
        }

        // حذف آدرس
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var model = await addressRepo.Table
                .FirstOrDefaultAsync(x => x.Id == id);

            if (model == null)
            {
                notification.AddErrorToastMessage("آدرس مورد نظر پیدا نشد.");

                return RedirectToAction("List");
            }

            await addressRepo.DeleteAsync(
                model,
                HttpContext.RequestAborted);

            notification.AddSuccessToastMessage("آدرس با موفقیت حذف شد.");

            return RedirectToAction("List");
        }

        // دریافت شهرهای مربوط به استان انتخاب شده
        [HttpGet]
        public async Task<IActionResult> GetCitiesOfProvince(Guid provinceId)
        {
            if (provinceId == Guid.Empty)
            {
                return Json(new List<object>());
            }

            var cities = await cityRepo.TableNoTracking
                .Where(x => x.ProvinceId == provinceId)
                .OrderBy(x => x.Name)
                .Select(x => new
                {
                    id = x.Id,
                    name = x.Name
                })
                .ToListAsync();

            return Json(cities);
        }
    }
}