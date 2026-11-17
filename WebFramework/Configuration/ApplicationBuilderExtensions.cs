using Common;
using Common.Utilities;

using Data;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using Services.DataInitializer;

namespace WebFramework.Configuration
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseHsts(this IApplicationBuilder app, IWebHostEnvironment env)
        {
            Assert.NotNull(app, nameof(app));
            Assert.NotNull(env, nameof(env));

            if (!env.IsDevelopment())
                app.UseHsts();

            return app;
        }

        public static IApplicationBuilder IntializeDatabase(this IApplicationBuilder app)
        {
            return InitializeDatabase(app);
        }

        public static IApplicationBuilder InitializeDatabase(this IApplicationBuilder app)
        {
            Assert.NotNull(app, nameof(app));

            using var scope = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope();
            var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
            var siteSettings = scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<SiteSettings>>().Value;
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            if (env.IsDevelopment() || siteSettings.DatabaseSettings?.AutoMigrate == true)
            {
                dbContext.Database.Migrate();
            }

            var dataInitializers = scope.ServiceProvider.GetServices<IDataInitializer>();
            foreach (var dataInitializer in dataInitializers)
                dataInitializer.InitializeDataAsync().GetAwaiter().GetResult();

            return app;
        }
    }
}
