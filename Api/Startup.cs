using Autofac;

using Common;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using System;
using System.Threading.RateLimiting;

using Serilog;

using WebFramework.Configuration;
using WebFramework.CustomMapping;
using WebFramework.Middlewares;
using WebFramework.Swagger;
using Microsoft.AspNetCore.RateLimiting;

namespace MyApi
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

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<SiteSettings>(Configuration.GetSection(nameof(SiteSettings)));
            services.ConfigureValidatedSiteSettings(Configuration);

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
            services.AddSmsSender(_siteSetting);
            services.AddHealthChecks();
            services.AddRateLimiter(options =>
            {
                options.AddFixedWindowLimiter("auth", limiterOptions =>
                {
                    limiterOptions.PermitLimit = 10;
                    limiterOptions.Window = TimeSpan.FromMinutes(1);
                    limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    limiterOptions.QueueLimit = 0;
                });
            });

            services.AddCustomIdentity(_siteSetting.IdentitySettings);

            services.AddMinimalMvc();
            services.AddElmahCore(Configuration, _siteSetting);

            services.AddJwtAuthentication(_siteSetting.JwtSettings);
            services.AddAuthorization();

            services.AddCustomApiVersioning();

            services.AddSwagger();

            //            services.AddAuthentication().AddGoogle(options =>
            //{
            //    options.ClientId = _siteSetting.GoogleSettings.ClientId;
            //    options.ClientSecret = _siteSetting.GoogleSettings.ClientSecret;
            //});

            // Don't create a ContainerBuilder for Autofac here, and don't call builder.Populate()
            // That happens in the AutofacServiceProviderFactory for you.

        }

        // ConfigureContainer is where you can register things directly with Autofac. 
        // This runs after ConfigureServices so the things ere will override registrations made in ConfigureServices.
        // Don't build the container; that gets done for you by the factory.
        public void ConfigureContainer(ContainerBuilder builder)
        {
            //Register Services to Autofac ContainerBuilder
            builder.AddServices();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // (status code, elapsed, ...)
            app.UseSerilogRequestLogging();

            app.InitializeDatabase();

            app.UseCustomExceptionHandler();

            app.UseHsts(env);
            app.UseHttpsRedirection();


            app.UseSwaggerAndUI();

            app.UseRouting();
            app.UseCors();
            app.UseRateLimiter();
            app.UseStaticFiles();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseElmahCore(_siteSetting);

            app.UseEndpoints(config =>
            {
                config.MapHealthChecks("/health/live").AllowAnonymous();
                config.MapHealthChecks("/health/ready").AllowAnonymous();
                config.MapControllers();
                config.MapDefaultControllerRoute();
            });
        }

    }
}
