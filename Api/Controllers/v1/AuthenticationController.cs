using API.Controllers.v1.Models.TokenRequest;
using API.Models.Authentication;

using Common;
using Common.Sms;
using Common.Utilities;

using Data.Repositories;

using DNTPersianUtils.Core;

using Entities;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

using Services;

using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using WebFramework.Api;

namespace MyApi.Controllers.v1
{
    [ApiVersion("1")]
    [EnableRateLimiting("auth")]
    public class AuthController : BaseController
    {
        private readonly IJwtService jwtService;
        private readonly UserManager<User> userManager;
        private readonly SignInManager<User> signInManager;
        private readonly IRepository<LoginCode> LoginCodeRepo;
        private readonly ISmsSender smsSender;

        public AuthController(IJwtService jwtService, UserManager<User> userManager, SignInManager<User> signInManager, IRepository<LoginCode> loginCodeRepo, ISmsSender smsSender)
        {
            this.jwtService = jwtService;
            this.userManager = userManager;
            this.signInManager = signInManager;
            LoginCodeRepo = loginCodeRepo;
            this.smsSender = smsSender;
        }

        [HttpPost("[Action]")]
        [AllowAnonymous]
        public async Task<ActionResult<AccessToken>> FreeToken([FromForm] TokenRequest Dto)
        {
            if (Dto == null || string.IsNullOrWhiteSpace(Dto.username) || string.IsNullOrWhiteSpace(Dto.password))
                return BadRequest("نام کاربری یا رمز عبور وارد نشده است!");

            var user = await userManager.FindByNameAsync(Dto.username);
            if (user == null)
                return BadRequest("کاربر یافت نشد!");

            var passwordCheck = await signInManager.CheckPasswordSignInAsync(user, Dto.password, lockoutOnFailure: true);
            if (!passwordCheck.Succeeded || !await userManager.IsInRoleAsync(user, "Admin"))
                return BadRequest("شما ادمین نیستید!");

            var jwt = await jwtService.GenerateAsync(user);
            return new JsonResult(jwt);
        }

        [HttpPost("[Action]")]
        [AllowAnonymous]
        public async Task<IActionResult> RefreshToken(RefreshRequest dto)
        {
            if (string.IsNullOrWhiteSpace(dto.RefreshToken))
                return Unauthorized();

            // refreshToken format: userId.random
            var parts = dto.RefreshToken.Split('.', 2);
            if (parts.Length != 2)
                return Unauthorized();

            var userId = parts[0];
            var randomRaw = parts[1];

            var user = await userManager.FindByIdAsync(userId);
            if (user is null)
                return Unauthorized();

            var stored = await userManager.GetAuthenticationTokenAsync(user, "JWT", "RefreshToken");
            if (string.IsNullOrWhiteSpace(stored))
                return Unauthorized();

            var tokens = stored.Split('|', 2);
            if (tokens.Length != 2)
                return Unauthorized();

            var storedHashHex = tokens[0];
            if (!DateTime.TryParse(tokens[1], null, System.Globalization.DateTimeStyles.AdjustToUniversal, out var expiresUtc))
                return Unauthorized();

            if (DateTime.UtcNow > expiresUtc)
                return Unauthorized();

            var providedHashHex = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(randomRaw)));
            if (!CryptographicOperations.FixedTimeEquals(Convert.FromHexString(storedHashHex), Convert.FromHexString(providedHashHex)))
                return Unauthorized();

            // Rotation
            await userManager.RemoveAuthenticationTokenAsync(user, "JWT", "RefreshToken");

            var access = await jwtService.GenerateAsync(user);

            return Ok(access);
        }

        [HttpPost("[Action]")]
        [AllowAnonymous]
        public async Task<ApiResult<LoginSignUpDTO>> Login_SignUp(LoginSignUpDTO Dto, CancellationToken cancellationToken)
        {
            if (!Dto.UserName.IsNumeric())
            {
                return BadRequest("شماره موبایل وارد شده معتبر نیست!");
            }
            //Fix Phones without zeros
            if (!Dto.UserName.StartsWith('0'))
            {
                Dto.UserName = "0" + Dto.UserName;
            }

            //check for existing user
            var User = await userManager.FindByNameAsync(Dto.UserName);
            if (User != null)
                Dto.IsExist = true;


            //complete profile

            //check for last sent otp
            var LastOtp = await LoginCodeRepo.AllTableNoTracking.FirstOrDefaultAsync(x => x.UserName == Dto.UserName);
            if (LastOtp != null)
            {
                if (LastOtp.SentAt.AddMinutes(2) > DateTime.UtcNow)
                {
                    return BadRequest("جهت دریافت کد جدید، باید 2 دقیقه از دریافت کد قبلی گذشته باشد");
                }
                else
                {
                    var code = (await smsSender.SendOtpAsync(Dto.UserName, cancellationToken)).Code;
                    LastOtp.OTP = MeliPayamak.HashOtp(code);
                    LastOtp.ExpiresAt = DateTime.UtcNow.AddMinutes(5);
                    LastOtp.SentAt = DateTime.UtcNow;
                    LastOtp.UsedAt = DateTime.MinValue;
                    LastOtp.FailedAttempts = 0;
                    await LoginCodeRepo.UpdateAsync(LastOtp, cancellationToken);
                }
            }
            else
            {
                var code = (await smsSender.SendOtpAsync(Dto.UserName, cancellationToken)).Code;
                //create login code Used Like Temp user after verification
                LastOtp = new LoginCode()
                {
                    UserName = Dto.UserName,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(5),
                    SentAt = DateTime.UtcNow,
                    OTP = MeliPayamak.HashOtp(code),
                };
                await LoginCodeRepo.AddAsync(LastOtp, default);
            }

            Dto.OTP = null;
            return Ok(Dto);
        }

        [HttpPost("[action]")]
        [AllowAnonymous]
        public async Task<ActionResult<AccessToken>> Login_SignUpValidation(LoginSignUpDTO dto)
        {
            //fix phonenumber zeros
            if (!dto.UserName.StartsWith('0'))
            {
                dto.UserName = "0" + dto.UserName;
            }

            var LastOtp = await LoginCodeRepo.TableNoTracking.FirstOrDefaultAsync(x => x.UserName == dto.UserName);
            //validation for last otp
            if (LastOtp == null)
                return BadRequest("شما ثبت نام نکرده اید!");

            if (LastOtp.UsedAt > DateTime.MinValue)
                return BadRequest("کد یکبار مصرف قبلا استفاده شده است!");

            if (LastOtp.FailedAttempts >= 5)
                return BadRequest("تعداد تلاش‌های ناموفق بیش از حد مجاز است!");

            if (LastOtp.ExpiresAt < DateTime.UtcNow)
            {

                return BadRequest("کد یکبار مصرف منقضی شده!");
            }

            //check for wrong Code
            if (LastOtp.OTP != MeliPayamak.HashOtp(dto.OTP))
            {
                LastOtp.FailedAttempts++;
                await LoginCodeRepo.UpdateAsync(LastOtp, default);
                return BadRequest("کد وارد شده اشتباه است! دوباره تلاش کنید!");
            }


            LastOtp.UsedAt = DateTime.UtcNow;

            await LoginCodeRepo.UpdateAsync(LastOtp, default);

            //check if user exists
            var User = await userManager.FindByNameAsync(dto.UserName);
            if (User != null)
            {
                var jwt = await jwtService.GenerateAsync(User);
                return new JsonResult(jwt);
            }
            else
            {
                var CreateUser = new User()
                {
                    UserName = dto.UserName
                };

                CreateUser.PhoneNumber = dto.UserName;
                CreateUser.Email = dto.UserName + "-Patient@goljaam.com";
                var Success = await userManager.CreateAsync(CreateUser);
                if (Success.Succeeded)
                {
                    var st = await userManager.AddToRoleAsync(CreateUser, "Patient");
                    if (!st.Succeeded)
                    {
                        return BadRequest("افزودن نقش به کاربر با خطا مواجه شد!");
                    }
                    var jwt = await jwtService.GenerateAsync(CreateUser);
                    return new JsonResult(jwt);
                }
                return BadRequest("ایجاد کاربر با خطا مواجه شد!");
            }
        }

        [HttpPost("[action]")]
        [AllowAnonymous]
        public async Task<ActionResult<AccessToken>> DoctorLogin_SignUpValidation(LoginSignUpDTO dto)
        {
            //fix phonenumber zeros
            if (!dto.UserName.StartsWith('0'))
            {
                dto.UserName = "0" + dto.UserName;
            }

            var LastOtp = await LoginCodeRepo.TableNoTracking.FirstOrDefaultAsync(x => x.UserName == dto.UserName);
            //validation for last otp
            if (LastOtp == null)
                return BadRequest("شما ثبت نام نکرده اید!");

            if (LastOtp.UsedAt > DateTime.MinValue)
                return BadRequest("کد یکبار مصرف قبلا استفاده شده است!");

            if (LastOtp.FailedAttempts >= 5)
                return BadRequest("تعداد تلاش‌های ناموفق بیش از حد مجاز است!");

            if (LastOtp.ExpiresAt < DateTime.UtcNow)
            {


                return BadRequest("کد یکبار مصرف منقضی شده!");
            }

            //check for wrong Code
            if (LastOtp.OTP != MeliPayamak.HashOtp(dto.OTP))
            {
                LastOtp.FailedAttempts++;
                await LoginCodeRepo.UpdateAsync(LastOtp, default);
                return BadRequest("کد وارد شده اشتباه است! دوباره تلاش کنید!");
            }


            LastOtp.UsedAt = DateTime.UtcNow;

            await LoginCodeRepo.UpdateAsync(LastOtp, default);

            //check if user exists
            var User = await userManager.FindByNameAsync(dto.UserName);
            if (User != null)
            {
                var jwt = await jwtService.GenerateAsync(User);
                return new JsonResult(jwt);
            }
            else
            {
                var CreateUser = new User()
                {
                    UserName = dto.UserName
                };

                CreateUser.PhoneNumber = dto.UserName;
                CreateUser.Email = dto.UserName + "-Doctor@goljaam.com";
                var Success = await userManager.CreateAsync(CreateUser);
                if (Success.Succeeded)
                {
                    var st = await userManager.AddToRoleAsync(CreateUser, "Doctor");
                    if (!st.Succeeded)
                    {
                        return BadRequest("افزودن نقش به کاربر با خطا مواجه شد!");
                    }
                    var jwt = await jwtService.GenerateAsync(CreateUser);
                    return new JsonResult(jwt);
                }
                return BadRequest("ایجاد کاربر با خطا مواجه شد!");
            }
        }

        [HttpPost("[Action]")]
        [Authorize]
        public async Task<ActionResult> SignOut()
        {
            var User = await userManager.FindByNameAsync(HttpContext.User.Identity.Name);
            await userManager.UpdateSecurityStampAsync(User);
            await userManager.RemoveAuthenticationTokenAsync(User, "JWT", "RefreshToken");

            return Ok();
        }

        [HttpGet("[action]")]
        [AllowAnonymous]
        public async Task<ApiResult> Healthz()
        {
            return Ok();
        }

        [HttpPost("[action]")]
        [AllowAnonymous]
        public async Task<ActionResult<AccessToken>> LoginPassword(LoginPasswordDto dto)
        {
            var finduser = await userManager.FindByNameAsync(dto.UserName);

            if (finduser == null)
            {
                return BadRequest("کاربر یافت نشد!");
            }

            if (await userManager.IsInRoleAsync(finduser, "Admin") == false)
            {
                return BadRequest("سطح دسترسی کافی نیست!");
            }

            var Status = await signInManager.PasswordSignInAsync(finduser, dto.Password, true, true);

            if (Status.Succeeded == false)
            {
                return BadRequest("رمز عبور اشتباه است");
            }

            var token =await jwtService.GenerateAsync(finduser);

            return new JsonResult(token);

        }


    }
}
