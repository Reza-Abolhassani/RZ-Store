using Admin.Models;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Data.Repositories;
using EfCoreSetUp1618.Tools;
using Entities;
using Entities.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
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
    public class UserController : Controller
    {
        private readonly UserManager<User> userManager;
        private readonly IToastNotification notification;
        private readonly IMapper mapper;
        private readonly IRepository<UserWallet> userWalletRepo;
        private readonly IRepository<TransactionHistory> transactionHistoryRepo;

        private readonly IWebHostEnvironment environment;

        public UserController(UserManager<User> userManager, IToastNotification notification, IMapper mapper, IRepository<UserWallet> userWalletRepo, IRepository<TransactionHistory> transactionHistoryRepo, IWebHostEnvironment environment)
        {
            this.userManager = userManager;
            this.notification = notification;
            this.mapper = mapper;
            this.userWalletRepo = userWalletRepo;
            this.transactionHistoryRepo = transactionHistoryRepo;
            this.environment = environment;
        }

        public IActionResult Add()
        {
            var model = new UserDto();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(UserDto dto)
        {
            // تبدیل DTO به Entity
            var model = dto.ToEntity(mapper);

            // ذخیره تصویر کاربر
            if (dto.ImageFile != null)
            {
                model.ImageUrl = FileTool.SaveFile(
                    dto.ImageFile,
                    "UserImages",
                    environment.WebRootPath);
            }

            // ساخت کاربر
            var status = await userManager.CreateAsync(model, dto.Password);

            if (!status.Succeeded)
            {
                foreach (var error in status.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }

                return View(dto);
            }

            // اختصاص نقش به کاربر
            var roleStatus = await userManager.AddToRoleAsync(
                model,
                dto.Role.ToString());

            if (!roleStatus.Succeeded)
            {
                foreach (var error in roleStatus.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }

                return View(dto);
            }

            // ساخت کیف پول کاربر
            var userwallet = new UserWallet()
            {
                Amount = 10000,
                UserId = model.Id,
            };

            userWalletRepo.Add(userwallet);

            notification.AddSuccessToastMessage("کاربر با موفقیت ثبت شد");

            return RedirectToAction("List");
        }

        public async Task<IActionResult> List()
        {
            var users = await userManager.Users.ToListAsync();

            var model = mapper.Map<List<UserDto>>(users);

            foreach (var item in model)
            {
                var user = users.FirstOrDefault(x => x.Id == item.Id);

                var roles = await userManager.GetRolesAsync(user);

                if (roles.Any())
                {
                    item.Role = Enum.Parse<SystemRoles>(roles.First());
                }
            }

            return View(model);
        }

        //public async Task<IActionResult> List()
        //{
        //    var model = await userManager.Users
        //        .ProjectTo<UserDto>(mapper.ConfigurationProvider)
        //        .ToListAsync();

        //    return View(model);
        //}

        public async Task<IActionResult> Update(Guid Id)
        {
            var model = await userManager.Users
                .ProjectTo<UserDto>(mapper.ConfigurationProvider)
                .FirstOrDefaultAsync(x => x.Id == Id);

            if (model == null)
            {
                notification.AddErrorToastMessage("کاربر مورد نظر یافت نشد");
                return RedirectToAction("List");
            }

            // دریافت نقش واقعی کاربر از Identity
            var user = await userManager.FindByIdAsync(Id.ToString());

            var roles = await userManager.GetRolesAsync(user);

            if (roles.Any())
            {
                model.Role = Enum.Parse<SystemRoles>(roles.First());
            }

            return View(model);
        }


        //public async Task<IActionResult> Update(Guid Id)
        //{
        //    var model = await userManager.Users
        //        .ProjectTo<UserDto>(mapper.ConfigurationProvider)
        //        .FirstOrDefaultAsync(x => x.Id == Id);

        //    if (model == null)
        //    {
        //        notification.AddErrorToastMessage("کاربر مورد نظر یافت نشد");
        //        return RedirectToAction("List");
        //    }

        //    return View(model);
        //}


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(UserDto dto)
        {
            var model = await userManager.Users
                .FirstOrDefaultAsync(x => x.Id == dto.Id);

            if (model == null)
            {
                notification.AddErrorToastMessage("کاربر مورد نظر یافت نشد");
                return RedirectToAction("List");
            }

            // ویرایش اطلاعات کاربر
            model.FirstName = dto.FirstName;
            model.LastName = dto.LastName;
            model.PhoneNumber = dto.PhoneNumber;
            model.Email = dto.Email;
            model.UserName = dto.UserName;
            model.IsActive = dto.IsActive;

            // ذخیره تصویر جدید
            if (dto.ImageFile != null)
            {
                model.ImageUrl = FileTool.SaveFile(
                    dto.ImageFile,
                    "UserImages",
                    environment.WebRootPath);
            }

            // ذخیره تغییرات اطلاعات کاربر
            var status = await userManager.UpdateAsync(model);

            if (!status.Succeeded)
            {
                foreach (var error in status.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }

                return View(dto);
            }

            // دریافت نقش های فعلی کاربر
            var currentRoles = await userManager.GetRolesAsync(model);

            // حذف نقش های قبلی
            if (currentRoles.Any())
            {
                var removeStatus = await userManager.RemoveFromRolesAsync(
                    model,
                    currentRoles);

                if (!removeStatus.Succeeded)
                {
                    notification.AddErrorToastMessage("حذف نقش قبلی کاربر با خطا مواجه شد");
                    return RedirectToAction("List");
                }
            }

            // اختصاص نقش جدید
            var roleStatus = await userManager.AddToRoleAsync(
                model,
                dto.Role.ToString());

            if (!roleStatus.Succeeded)
            {
                foreach (var error in roleStatus.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }

                notification.AddErrorToastMessage("تغییر نقش کاربر با خطا مواجه شد");
                return View(dto);
            }

            notification.AddSuccessToastMessage("کاربر با موفقیت ویرایش شد");

            return RedirectToAction("List");
        }


        //[HttpPost]
        //public async Task<IActionResult> Update(UserDto dto)
        //{
        //    var model = await userManager.Users
        //        .FirstOrDefaultAsync(x => x.Id == dto.Id);

        //    if (model == null)
        //    {
        //        notification.AddErrorToastMessage("کاربر مورد نظر یافت نشد");
        //        return RedirectToAction("List");
        //    }

        //    model.FirstName = dto.FirstName;
        //    model.LastName = dto.LastName;
        //    model.PhoneNumber = dto.PhoneNumber;
        //    model.Email = dto.Email;
        //    model.UserName = dto.UserName;

        //    if (dto.ImageFile != null)
        //    {
        //        //model.ImageUrl = FileTool.SaveFile(dto.ImageFile, "UserImages");

        //        model.ImageUrl = FileTool.SaveFile(dto.ImageFile, "CategoryImage", environment.WebRootPath);
        //    }

        //    var status = await userManager.UpdateAsync(model);

        //    //if (status.Succeeded == false)
        //    //{
        //    //    notification.AddErrorToastMessage("ویرایش کاربر با خطا مواجه شد");
        //    //    return View(dto);
        //    //}

        //    if (!status.Succeeded)
        //    {
        //        notification.AddErrorToastMessage("ویرایش کاربر با خطا مواجه شد");
        //        return View(dto);
        //    }

        //    notification.AddSuccessToastMessage("کاربر با موفقیت ویرایش شد");

        //    return RedirectToAction("List");
        //}

        [HttpPost]
        public async Task<IActionResult> Delete(Guid id)
        {
            var user = await userManager.Users
                .FirstOrDefaultAsync(x => x.Id == id);

            if (user == null)
            {
                notification.AddErrorToastMessage("کاربر مورد نظر یافت نشد");
                return RedirectToAction("List");
            }

            var status = await userManager.DeleteAsync(user);

            if (!status.Succeeded)
            {
                notification.AddErrorToastMessage("حذف کاربر با خطا مواجه شد");
                return RedirectToAction("List");
            }

            notification.AddSuccessToastMessage("کاربر با موفقیت حذف شد");

            return RedirectToAction("List");
        }

        public async Task<IActionResult> walletcharge(Guid userid)
        {
            var model = await userWalletRepo.TableNoTracking
                .ProjectTo<UserWalletDto>(mapper.ConfigurationProvider)
                .FirstOrDefaultAsync(x => x.UserId == userid);

            if (model == null)
            {
                notification.AddErrorToastMessage("کیف پول کاربر پیدا نشد");
                return RedirectToAction("List");
            }

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> walletcharge(UserWalletDto dto)
        {
            if (dto == null)
            {
                notification.AddErrorToastMessage("اطلاعات نامعتبر است");
                return RedirectToAction("List");
            }

            var wallet = await userWalletRepo.TableNoTracking
                .FirstOrDefaultAsync(x => x.Id == dto.Id);

            if (wallet == null)
            {
                notification.AddErrorToastMessage("کیف پول پیدا نشد");
                return RedirectToAction("List");
            }

            if (dto.Amount == 0)
            {
                notification.AddErrorToastMessage("مبلغ باید غیر از صفر باشد");
                return View(dto);
            }

            wallet.Amount += dto.Amount;

            userWalletRepo.Update(wallet);

            var tr = new TransactionHistory()
            {
                Amount = dto.Amount,
                CreationDateTime = DateTime.Now,
                Description = dto.Amount < 0 ? "کسر از کیف پول" : "شارژ کیف پول",
                UserId = dto.UserId
            };

            transactionHistoryRepo.Add(tr);

            notification.AddSuccessToastMessage("عملیات کیف پول با موفقیت انجام شد");

            return RedirectToAction("List");
        }


        public async Task<IActionResult> TransactionList(Guid userid)
        {
            var model = await transactionHistoryRepo.TableNoTracking
                .Where(x => x.UserId == userid)
                .ProjectTo<TransactionHistoryDto>(mapper.ConfigurationProvider)
                .ToListAsync();

            return View(model);
        }

        public async Task<IActionResult> EditProfile()
        {
            var user = await userManager.GetUserAsync(User);

            if (user == null)
                return NotFound();

            var model = mapper.Map<UserDto>(user);

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> EditProfile(UserDto dto)
        {
            var finduser = await userManager.GetUserAsync(User);

            finduser.FirstName = dto.FirstName;
            finduser.LastName = dto.LastName;
            finduser.Email = dto.Email;
            finduser.PhoneNumber = dto.PhoneNumber;

            if (dto.ImageFile != null)
            {
                finduser.ImageUrl = FileTool.SaveFile(
                    dto.ImageFile,
                    "UserImages",
                    environment.WebRootPath
                );
            }


            var status = await userManager.UpdateAsync(finduser);

            if (status.Succeeded == false)
            {
                notification.AddErrorToastMessage("ویرایش کاربر با خطا مواجه شد");
                return View(dto);
            }
            notification.AddSuccessToastMessage("با موفقیت ویرایش شد");

            return RedirectToAction("List");
        }

        public async Task<IActionResult> ChangePassword()
        {
            return View();

        }
        [HttpPost]
        public async Task<IActionResult> ChangePassword(ChangePasswordDto dto)
        {


            if (dto.NewPassword != dto.ConfirmPassword)
            {
                notification.AddErrorToastMessage("رمز عبور جدید با تکرار آن یکسان نیست");

                return View(dto);
            }
            var FindUser = await userManager.GetUserAsync(User);


            if (FindUser == null)
            {
                return Challenge();//نتوانسته کاربر واردشده را پیدا کند
            }

            var status = await userManager.ChangePasswordAsync(FindUser, dto.CurrentPassword, dto.NewPassword);

            if (!status.Succeeded)
            {
                notification.AddErrorToastMessage(" تغییر رمز عبور با خطا مواجه شد ");

                return View();
            }


            //if (!status.Succeeded)
            //{
            //    foreach (var error in status.Errors)
            //    {
            //        notification.AddErrorToastMessage(
            //            $"{error.Code}: {error.Description}");
            //    }

            //    return View(dto);
            //}



            notification.AddSuccessToastMessage("تغییر رمز عبور با موفقیت انجام شد");

            return RedirectToAction("Index", "Home");


        }
    }
}
