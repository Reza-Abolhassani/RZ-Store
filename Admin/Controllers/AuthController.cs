using AutoMapper;

using Entities;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

using NToastNotify;

using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace Admin.Controllers
{
    [AllowAnonymous]
    public class AuthController : Controller
    {
        private readonly UserManager<User> userManager;
        private readonly SignInManager<User> signInManager;
        private readonly IMapper mapper;
        private readonly IToastNotification notification;

        public AuthController(UserManager<User> userManager, SignInManager<User> signInManager, IMapper mapper, IToastNotification notification)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.mapper = mapper;
            this.notification = notification;
        }

        public async Task<IActionResult> Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login([Required] string UserName, [Required] string Password)
        {
            if (!ModelState.IsValid)
            {
                notification.AddErrorToastMessage("لطفاً نام کاربری و رمز عبور را وارد کنید");
                return RedirectToAction(nameof(Login));
            }

            var status = await signInManager.PasswordSignInAsync(
                UserName,
                Password,
                true,
                true
            );

            if (!status.Succeeded)
            {
                notification.AddErrorToastMessage("اطلاعات ورود اشتباه است");
                return RedirectToAction(nameof(Login));
            }

            // پیدا کردن کاربر واردشده
            var user = await userManager.FindByNameAsync(UserName);

            if (user != null)
            {
                // بررسی نقش مدیر
                var isAdmin = await userManager.IsInRoleAsync(user, "Admin");

                if (isAdmin)
                {
                    return RedirectToAction("Index", "AdminDashboard");
                }
            }

            // انتقال کاربران عادی به صفحه اصلی سایت
            return RedirectToAction("Index", "Home");
        }
        //[HttpPost]
        //public async Task<IActionResult> Login([Required] string UserName, [Required] string Password)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        notification.AddErrorToastMessage("کاربر یافت نشد");
        //        return RedirectToAction();
        //    }
        //    var Status = await signInManager.PasswordSignInAsync(UserName, Password, true, true);
        //    if (Status.Succeeded == false)
        //    {
        //        notification.AddErrorToastMessage("اطلاعات ورود اشتباه است");
        //        return RedirectToAction();
        //    }
        //    return RedirectToAction("index", "home");
        //}

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LogOut()
        {
            await signInManager.SignOutAsync();

            return RedirectToAction(nameof(Login));
        }


        //public async Task<IActionResult> LogOut()
        //{
        //    await signInManager.SignOutAsync();

        //    return RedirectToAction(nameof(Login));
        //}
    }
}
