using AutoMapper;
using AutoMapper.QueryableExtensions;
using Client.Models;
using Data.Repositories;
using Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace Client.Controllers
{
    [AllowAnonymous]
    public class HomeController : Controller
    {
        private readonly IRepository<Category> CategoryRepo;
        private readonly IRepository<Product> ProductRepo;
        private readonly IMapper mapper;
        private readonly IRepository<OrderItem> OrderItemRepo;
        private readonly IRepository<HomeBanner> HomeBannerRepo;

        public HomeController(IRepository<Category> categoryRepo, IRepository<Product> productRepo, IMapper mapper, IRepository<OrderItem> orderItemRepo, IRepository<HomeBanner> homeBannerRepo)
        {
            CategoryRepo = categoryRepo;
            ProductRepo = productRepo;
            this.mapper = mapper;
            OrderItemRepo = orderItemRepo;
            HomeBannerRepo = homeBannerRepo;
        }

        public async Task<IActionResult> Index()
        {
            var model = new IndexDto();

            model.HomeBanners = await HomeBannerRepo.TableNoTracking
            .Where(x => x.Active)//فقط بنرهای فعال را دریافت می‌کند.
            .OrderBy(x => x.Position)//ابتدا بنرهای موقعیت اول را قرار می‌دهد.
            .ThenBy(x => x.Order)//سپس بنرهای موقعیت دوم را قرار می‌دهد.
            .ProjectTo<HomeBannerDto>(mapper.ConfigurationProvider)
            .ToListAsync();



            model.Categories = await CategoryRepo.TableNoTracking.Where(x => x.ParentId == null)
                .ProjectTo<CategoryDto>(mapper.ConfigurationProvider).ToListAsync();// // دسته بندی ها

            model.NewProducts = await ProductRepo.TableNoTracking.OrderByDescending(x => x.CreationDateTime).Take(8)
                .ProjectTo<ProductDto>(mapper.ConfigurationProvider).ToListAsync();// جدیدترین محصولات

            model.CheapestProducts = await ProductRepo.TableNoTracking.OrderBy(x => x.Total).Take(8)//فقط 8 محصول نشان بدهیم
                .ProjectTo<ProductDto>(mapper.ConfigurationProvider).ToListAsync(); // ارزان ترین محصولات


            model.BestSellingProducts = await OrderItemRepo.TableNoTracking
                .Where(x => x.Order.IsPaid)//فقط جزئیات سفارش‌هایی را نگه می‌دارد که پرداخت شده‌اند.
                .GroupBy(x => x.ProductId)//رکوردها را بر اساس شناسه محصول گروه‌بندی می‌کند.


                .Select(g => new
                {
                    ProductId = g.Key,
                    TotalSold = g.Sum(x => x.Count)
                })

                .OrderByDescending(x => x.TotalSold)//محصولات را از بیشترین تعداد فروش به کمترین مرتب می‌کند.
                .Take(8)
                .Join(
                    ProductRepo.TableNoTracking,
                    sold => sold.ProductId,
                    product => product.Id,
                    (sold, product) => product
                )
                .ProjectTo<ProductDto>(mapper.ConfigurationProvider)
                .ToListAsync();

            return View(model);
        }
    }
}
