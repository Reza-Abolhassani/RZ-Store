using Admin.Models;
using AutoMapper;
using Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Admin.ViewComponents
{
    public class UserProfileViewComponent : ViewComponent
    {
        private readonly UserManager<User> userManager;
        private readonly IMapper mapper;

        public UserProfileViewComponent(UserManager<User> userManager, IMapper mapper)
        {
            this.userManager = userManager;
            this.mapper = mapper;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            //var finduser = await userManager.FindByNameAsync(HttpContext.User.Identity.Name);

            //var model = mapper.Map<UserDto>(finduser);

            //return View("UserProfile", model);


            // بررسی لاگین بودن کاربر
            if (HttpContext.User?.Identity?.IsAuthenticated != true)
            {
                return Content("");
            }

            // گرفتن نام کاربری
            var userName = HttpContext.User.Identity?.Name;

            if (string.IsNullOrEmpty(userName))
            {
                return Content("");
            }

            // پیدا کردن کاربر
            var finduser = await userManager.FindByNameAsync(userName);

            if (finduser == null)
            {
                return Content("");
            }

            // تبدیل User به UserDto
            var model = mapper.Map<UserDto>(finduser);

            if (model == null)
            {
                return Content("");
            }

            return View("UserProfile", model);
        }
    }
}
