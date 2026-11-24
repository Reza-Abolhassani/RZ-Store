using Client.Models;
using Data;
using Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace Client.Controllers
{
    public class ProfileController : Controller
    {
        private readonly UserManager<User> userManager;
        private readonly ApplicationDbContext _context;

        public ProfileController(
            UserManager<User> userManager,
            ApplicationDbContext context)
        {
            this.userManager = userManager;
            _context = context;
        }

        // نمایش پروفایل مشتری
        public async Task<IActionResult> Index()
        {
            if (User.Identity?.IsAuthenticated != true)
                return RedirectToAction("Login", "ClientLogin");

            var user = await userManager.GetUserAsync(User);

            if (user == null)
                return RedirectToAction("Login", "ClientLogin");

            var model = new UserDto
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            if (User.Identity?.IsAuthenticated != true)
                return RedirectToAction("Login", "ClientLogin");

            var user = await userManager.GetUserAsync(User);

            if (user == null)
                return RedirectToAction("Login", "ClientLogin");

            var model = new UserDto
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UserDto model)
        {
            if (User.Identity?.IsAuthenticated != true)
                return RedirectToAction("Login", "ClientLogin");

            var user = await userManager.GetUserAsync(User);

            if (user == null)
                return RedirectToAction("Login", "ClientLogin");

            if (string.IsNullOrWhiteSpace(model.FirstName) ||
                string.IsNullOrWhiteSpace(model.LastName))
            {
                ModelState.AddModelError("", "نام و نام خانوادگی را وارد کنید.");
                return View(model);
            }

            user.FirstName = model.FirstName.Trim();
            user.LastName = model.LastName.Trim();

            await _context.SaveChangesAsync();

            TempData["Success"] = "اطلاعات شما با موفقیت ویرایش شد.";

            return RedirectToAction(nameof(Index));
        }
    }
}