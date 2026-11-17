using Admin.Models;
using AutoMapper;
using Entities.Setting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using NToastNotify;
using Services;
using System.Threading.Tasks;

using EfCoreSetUp1618.Tools;


namespace Admin.Controllers
{
    [Authorize(Roles = "Admin")]
    public class StoreSettingsController : Controller
    {
        private readonly IStoreSettingsService settingsService;
        private readonly IMapper mapper;
        private readonly IToastNotification notification;
        private readonly IWebHostEnvironment environment;

        public StoreSettingsController(
            IStoreSettingsService settingsService,
            IMapper mapper,
            IToastNotification notification,
            IWebHostEnvironment environment)
        {
            this.settingsService = settingsService;
            this.mapper = mapper;
            this.notification = notification;
            this.environment = environment;
        }

        public async Task<IActionResult> Index()
        {
            var settings = await settingsService.GetSettingsAsync();

            var model = settings == null
                ? new StoreSettingsDto()
                : StoreSettingsDto.FromEntity(mapper, settings);

            return View(model);
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(StoreSettingsDto dto)
        {
            if (!ModelState.IsValid)
            {
                return View(dto);
            }

            var model = dto.ToEntity(mapper);

            var currentSettings = await settingsService.GetSettingsAsync();

            if (dto.LogoFile != null && dto.LogoFile.Length > 0)
            {
                model.Logo = FileTool.SaveFile(
                    dto.LogoFile,
                    "StoreLogo",
                    environment.WebRootPath);
            }
            else
            {
                model.Logo = currentSettings?.Logo;
            }

            await settingsService.UpdateSettingsAsync(model);

            notification.AddSuccessToastMessage(
                "تنظیمات فروشگاه با موفقیت ذخیره شد.");

            return RedirectToAction("Index");
        }
    }
}