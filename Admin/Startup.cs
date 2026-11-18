using Autofac;

using Common;

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Microsoft.Extensions.FileProviders;
using System.IO;

using System;

using WebFramework.Configuration;
using WebFramework.CustomMapping;
using WebFramework.Swagger;

namespace Admin
{
    public class Startup
    {
        private readonly SiteSettings _siteSetting;
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;

            _siteSetting = configuration.GetSection(nameof(SiteSettings)).Get<SiteSettings>();
        }
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<SiteSettings>(Configuration.GetSection(nameof(SiteSettings)));
            services.AddCors(options => options.AddDefaultPolicy(builder =>
            {
                builder.AllowAnyHeader().AllowAnyMethod();

                var allowedOrigins = _siteSetting.CorsSettings?.AllowedOrigins ?? Array.Empty<string>();
                if (allowedOrigins.Length > 0)
                    builder.WithOrigins(allowedOrigins);
                else
                    builder.SetIsOriginAllowed(_ => false);
            }));

            services.InitializeAutoMapper();

            services.AddDbContext(Configuration);

            services.AddCustomIdentity(_siteSetting.IdentitySettings);
            services.AddAuthorization();

            services.AddElmahCore(Configuration, _siteSetting);

            services.ConfigureApplicationCookie(delegate (CookieAuthenticationOptions options)
            {
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Strict;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
                options.SlidingExpiration = true;
                options.LoginPath = "/auth/Login";
                options.LogoutPath = "/auth/Logout";
                options.AccessDeniedPath = "/auth/Logout";
                options.Cookie.Name = "YourAppCookieName";
                options.ReturnUrlParameter = CookieAuthenticationDefaults.ReturnUrlParameter;
            });

            services.AddControllersWithViews(options =>
            {
                options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
            }).AddNToastNotifyNoty(new NToastNotify.NotyOptions
            {
                Timeout = 1000,
                ProgressBar = true,
                Modal = true,
                Layout = "topCenter",
                Theme = "metroui"
            })
                .AddControllersAsServices().AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.PropertyNameCaseInsensitive = false;
                    options.JsonSerializerOptions.PropertyNamingPolicy = null;
                });
        }
        public void ConfigureContainer(ContainerBuilder builder)
        {
            builder.AddServices();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {

            if (env.EnvironmentName == "Development")
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }
            app.InitializeDatabase();
            app.UseHsts(env);
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            var sharedImagesPath = Path.GetFullPath(Path.Combine(env.ContentRootPath, "..", "SharedImages"));

            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(sharedImagesPath),
                RequestPath = "/images"
            });

            app.UseNToastNotify();
            app.UseRouting();
            app.UseCors();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseElmahCore(_siteSetting);
            app.UseEndpoints(config =>
            {
                config.MapControllers().RequireAuthorization();
                config.MapDefaultControllerRoute().RequireAuthorization();
            });
        }
    }
}
