using Client.Models;
using Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NToastNotify;
using Services;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Client.Controllers
{
    public class AuthController : Controller
    {
        private readonly IOtpService otpService;
        private readonly UserManager<User> userManager;
        private readonly SignInManager<User> signInManager;
        private readonly IToastNotification notification;

        public AuthController(
            IOtpService otpService,
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            IToastNotification notification)
        {
            this.otpService = otpService;
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.notification = notification;
        }

        // نمایش صفحه ورود
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login()
        {
            return View();
        }

        // ورود با شماره موبایل و رمز عبور
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PasswordLogin(
            LoginDto model,
            CancellationToken cancellationToken)
        {
            // بررسی اطلاعات ارسالی
            if (model == null)
            {
                return Json(new
                {
                    succeeded = false,
                    message = "اطلاعات ارسالی معتبر نیست."
                });
            }

            // بررسی اعتبار مدل
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(x => x.Errors)
                    .Select(x => x.ErrorMessage)
                    .ToList();

                return Json(new
                {
                    succeeded = false,
                    message = errors.FirstOrDefault()
                        ?? "اطلاعات واردشده معتبر نیست."
                });
            }

            // پاکسازی شماره موبایل
            var phoneNumber = model.UserName.Trim();

            // پیدا کردن کاربر با شماره موبایل
            var user = await userManager.FindByNameAsync(phoneNumber);

            if (user == null)
            {
                return Json(new
                {
                    succeeded = false,
                    message = "شماره موبایل یا رمز عبور اشتباه است."
                });
            }

            // بررسی فعال بودن حساب
            if (!user.IsActive)
            {
                return Json(new
                {
                    succeeded = false,
                    message = "حساب کاربری شما غیرفعال است."
                });
            }

            // بررسی رمز عبور و انجام ورود
            var result = await signInManager.PasswordSignInAsync(
                phoneNumber,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: true);

            if (!result.Succeeded)
            {
                return Json(new
                {
                    succeeded = false,
                    message = result.IsLockedOut
                        ? "حساب شما موقتاً مسدود شده است. کمی بعد دوباره تلاش کنید."
                        : "شماره موبایل یا رمز عبور اشتباه است."
                });
            }

            // ثبت آخرین زمان ورود
            user.LastLoginDate = DateTime.UtcNow;

            await userManager.UpdateAsync(user);

            return Json(new
            {
                succeeded = true,
                message = "ورود با موفقیت انجام شد.",
                redirectUrl = Url.Action("Index", "Home")
            });
        }



        // نمایش صفحه ثبت نام
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            return View();
        }

        // ثبت نام با شماره موبایل و رمز عبور
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(
    RegisterDto model,
    CancellationToken cancellationToken)
        {
            // 1. بررسی معتبر بودن مدل
            if (model == null)
            {
                return Json(new
                {
                    succeeded = false,
                    message = "اطلاعات ارسالی معتبر نیست."
                });
            }

            // 2. بررسی اعتبار اطلاعات فرم
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(x => x.Errors)
                    .Select(x => x.ErrorMessage)
                    .ToList();

                return Json(new
                {
                    succeeded = false,
                    message = errors.FirstOrDefault()
                        ?? "اطلاعات واردشده معتبر نیست."
                });
            }

            // 3. پاکسازی اطلاعات ورودی
            var phoneNumber = model.PhoneNumber?.Trim();
            var email = model.Email?.Trim();

            // 4. بررسی شماره موبایل
            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                return Json(new
                {
                    succeeded = false,
                    message = "لطفاً شماره موبایل را وارد کنید."
                });
            }

            // 5. بررسی ایمیل
            if (string.IsNullOrWhiteSpace(email))
            {
                return Json(new
                {
                    succeeded = false,
                    message = "لطفاً ایمیل خود را وارد کنید."
                });
            }

            // 6. بررسی تکراری نبودن شماره موبایل
            var existingUser = await userManager.FindByNameAsync(
                phoneNumber);

            if (existingUser != null)
            {
                return Json(new
                {
                    succeeded = false,
                    message = "این شماره موبایل قبلاً ثبت‌نام کرده است."
                });
            }

            // 7. بررسی تکراری نبودن ایمیل
            var existingEmail = await userManager.FindByEmailAsync(
                email);

            if (existingEmail != null)
            {
                return Json(new
                {
                    succeeded = false,
                    message = "این ایمیل قبلاً ثبت شده است."
                });
            }

            // 8. ساخت کاربر جدید
            var user = new User
            {
                UserName = phoneNumber,
                PhoneNumber = phoneNumber,
                Email = email,
                FirstName = model.FirstName?.Trim(),
                LastName = model.LastName?.Trim(),
                RegisterDate = DateTime.UtcNow,
                LastLoginDate = DateTime.UtcNow,
                IsActive = true,
                PhoneNumberConfirmed = false,
                EmailConfirmed = false
            };

            // 9. ایجاد کاربر و ذخیره رمز عبور به صورت Hash
            var createResult = await userManager.CreateAsync(
                user,
                model.Password);

            if (!createResult.Succeeded)
            {
                var errors = createResult.Errors
                    .Select(x => x.Description)
                    .ToList();

                return Json(new
                {
                    succeeded = false,
                    message = errors.FirstOrDefault()
                        ?? "ثبت‌نام انجام نشد.",
                    errors = errors
                });
            }

            // 10. ورود خودکار کاربر پس از ثبت‌نام
            await signInManager.SignInAsync(
                user,
                isPersistent: false);

            // 11. پاسخ موفقیت
            return Json(new
            {
                succeeded = true,
                message = "ثبت‌نام با موفقیت انجام شد.",
                redirectUrl = Url.Action("Index", "Home")
            });
        }

        // درخواست کد OTP
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> RequestOtp(
            RequestOtpDto model,
            CancellationToken cancellationToken)
        {
            if (model == null ||
                string.IsNullOrWhiteSpace(model.PhoneNumber))
            {
                return Json(new
                {
                    succeeded = false,
                    message = "شماره موبایل را وارد کنید."
                });
            }

            var requestIp =
                HttpContext.Connection.RemoteIpAddress?.ToString();

            var result = await otpService.SendOtpAsync(
                model.PhoneNumber,
                requestIp,
                cancellationToken);

            return Json(new
            {
                succeeded = result.Succeeded,
                message = result.Message
            });
        }

        // تأیید OTP و ورود یا ثبت نام کاربر
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyOtp(
            VerifyOtpDto model,
            CancellationToken cancellationToken)
        {
            if (model == null)
            {
                return Json(new
                {
                    succeeded = false,
                    message = "اطلاعات ارسالی معتبر نیست."
                });
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(x => x.Errors)
                    .Select(x => x.ErrorMessage)
                    .ToList();

                return Json(new
                {
                    succeeded = false,
                    message = errors.FirstOrDefault()
                        ?? "اطلاعات واردشده معتبر نیست."
                });
            }

            // بررسی کاربر
            var user = await userManager.FindByNameAsync(
                model.PhoneNumber);

            // اگر کاربر جدید است، نام و نام خانوادگی لازم است
            if (user == null &&
                (string.IsNullOrWhiteSpace(model.FirstName) ||
                 string.IsNullOrWhiteSpace(model.LastName)))
            {
                return Json(new
                {
                    succeeded = false,
                    message = "برای ثبت‌نام، نام و نام خانوادگی را وارد کنید."
                });
            }

            // بررسی فعال بودن حساب
            if (user != null && !user.IsActive)
            {
                return Json(new
                {
                    succeeded = false,
                    message = "حساب کاربری شما غیرفعال است."
                });
            }

            // اعتبارسنجی کد OTP
            var otpResult = await otpService.VerifyOtpAsync(
                model.PhoneNumber,
                model.Code,
                cancellationToken);

            if (!otpResult.Succeeded)
            {
                return Json(new
                {
                    succeeded = false,
                    message = otpResult.Message
                });
            }

            // ثبت نام کاربر جدید در صورت عدم وجود
            if (user == null)
            {
                user = new User
                {
                    UserName = model.PhoneNumber,
                    PhoneNumber = model.PhoneNumber,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    RegisterDate = DateTime.UtcNow,
                    IsActive = true,
                    PhoneNumberConfirmed = true
                };

                var createResult = await userManager.CreateAsync(user);

                if (!createResult.Succeeded)
                {
                    return Json(new
                    {
                        succeeded = false,
                        message = "ثبت‌نام انجام نشد.",
                        errors = createResult.Errors
                            .Select(x => x.Description)
                            .ToList()
                    });
                }
            }
            else
            {
                user.PhoneNumberConfirmed = true;
            }

            // ثبت آخرین زمان ورود
            user.LastLoginDate = DateTime.UtcNow;

            var updateResult = await userManager.UpdateAsync(user);

            if (!updateResult.Succeeded)
            {
                return Json(new
                {
                    succeeded = false,
                    message = "به‌روزرسانی اطلاعات کاربر انجام نشد."
                });
            }

            // ورود کاربر
            await signInManager.SignInAsync(
                user,
                isPersistent: false);

            return Json(new
            {
                succeeded = true,
                message = "ورود با موفقیت انجام شد.",
                redirectUrl = Url.Action("Index", "Home")
            });
        }

        // خروج کاربر
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await signInManager.SignOutAsync();

            return RedirectToAction("Index", "Home");
        }
    }
}