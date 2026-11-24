using Client.Models;
using Data;
using Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

using System;

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

        // نمایش آدرس‌های مشتری
        public async Task<IActionResult> Addresses()
        {
            if (User.Identity?.IsAuthenticated != true)
                return RedirectToAction("Login", "ClientLogin");

            var user = await userManager.GetUserAsync(User);

            if (user == null)
                return RedirectToAction("Login", "ClientLogin");

            var addresses = await _context.Set<UserAddress>()
                .Where(x => x.UserId == user.Id)
                .OrderByDescending(x => x.IsDefault)
                .ToListAsync();

            return View(addresses);
        }

        // نمایش فرم افزودن آدرس
        [HttpGet]
        public async Task<IActionResult> CreateAddress()
        {
            if (User.Identity?.IsAuthenticated != true)
                return RedirectToAction("Login", "ClientLogin");

            var user = await userManager.GetUserAsync(User);

            if (user == null)
                return RedirectToAction("Login", "ClientLogin");

            return View();
        }


        // ذخیره آدرس جدید
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAddress(
            [Bind("ReceiverName,ReceiverPhone,Province,City,Address,PostalCode,IsDefault")]
    UserAddress model)
        {
            if (User.Identity?.IsAuthenticated != true)
                return RedirectToAction("Login", "ClientLogin");

            var user = await userManager.GetUserAsync(User);

            if (user == null)
                return RedirectToAction("Login", "ClientLogin");

            if (!ModelState.IsValid)
                return View(model);

            var addresses = _context.Set<UserAddress>()
                .Where(x => x.UserId == user.Id);

            if (!await addresses.AnyAsync())
            {
                model.IsDefault = true;
            }

            if (model.IsDefault)
            {
                var oldAddresses = await addresses.ToListAsync();

                foreach (var address in oldAddresses)
                {
                    address.IsDefault = false;
                }
            }

            model.UserId = user.Id;

            _context.Set<UserAddress>().Add(model);

            await _context.SaveChangesAsync();

            TempData["Success"] = "آدرس جدید با موفقیت ثبت شد.";

            return RedirectToAction(nameof(Addresses));
        }


        // نمایش فرم ویرایش آدرس
        [HttpGet]
        public async Task<IActionResult> EditAddress(Guid id)
        {
            if (User.Identity?.IsAuthenticated != true)
                return RedirectToAction("Login", "ClientLogin");

            var user = await userManager.GetUserAsync(User);

            if (user == null)
                return RedirectToAction("Login", "ClientLogin");

            var address = await _context.Set<UserAddress>()
                .FirstOrDefaultAsync(x => x.Id == id && x.UserId == user.Id);

            if (address == null)
                return NotFound();

            return View(address);
        }

        // ذخیره ویرایش آدرس
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAddress(UserAddress model)
        {
            if (User.Identity?.IsAuthenticated != true)
                return RedirectToAction("Login", "ClientLogin");

            var user = await userManager.GetUserAsync(User);

            if (user == null)
                return RedirectToAction("Login", "ClientLogin");

            var address = await _context.Set<UserAddress>()
                .FirstOrDefaultAsync(x => x.Id == model.Id && x.UserId == user.Id);

            if (address == null)
                return NotFound();

            if (!ModelState.IsValid)
                return View(address);

            address.ReceiverName = model.ReceiverName;
            address.ReceiverPhone = model.ReceiverPhone;
            address.Province = model.Province;
            address.City = model.City;
            address.Address = model.Address;
            address.PostalCode = model.PostalCode;

            await _context.SaveChangesAsync();

            TempData["Success"] = "آدرس با موفقیت ویرایش شد.";

            return RedirectToAction(nameof(Addresses));
        }


        // حذف آدرس مشتری
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAddress(Guid id)
        {
            if (User.Identity?.IsAuthenticated != true)
                return RedirectToAction("Login", "ClientLogin");

            var user = await userManager.GetUserAsync(User);

            if (user == null)
                return RedirectToAction("Login", "ClientLogin");

            var address = await _context.Set<UserAddress>()
                .FirstOrDefaultAsync(x => x.Id == id && x.UserId == user.Id);

            if (address == null)
                return NotFound();

            _context.Set<UserAddress>().Remove(address);

            await _context.SaveChangesAsync();

            TempData["Success"] = "آدرس با موفقیت حذف شد.";

            return RedirectToAction(nameof(Addresses));
        }
    }
}