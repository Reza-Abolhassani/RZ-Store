using AutoMapper;
using AutoMapper.QueryableExtensions;
using Client.Models;
using Data.Repositories;
using Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Client.Controllers
{
    public class ShopController : Controller
    {
        private readonly IRepository<Product> ProductRepo;
        private readonly IRepository<Category> CategoryRepo;
        private readonly IRepository<Gallery> GalleryRepo;
        private readonly IRepository<ShopCart> ShopCartRepo;
        private readonly IRepository<ShopCardItem> ShopCardItemRepo;
        private readonly UserManager<User> userManager;
        private readonly IMapper mapper;

        public ShopController(
            IRepository<Product> productRepo,
            IRepository<Category> categoryRepo,
            IRepository<Gallery> galleryRepo,
            IRepository<ShopCart> shopCartRepo,
            IRepository<ShopCardItem> shopCardItemRepo,
            UserManager<User> userManager,
            IMapper mapper)
        {
            ProductRepo = productRepo;
            CategoryRepo = categoryRepo;
            GalleryRepo = galleryRepo;
            ShopCartRepo = shopCartRepo;
            ShopCardItemRepo = shopCardItemRepo;
            this.userManager = userManager;
            this.mapper = mapper;
        }

        // جزئیات محصول
        public async Task<IActionResult> ProductDetail(Guid Id)
        {
            var model = await ProductRepo.TableNoTracking
                .ProjectTo<ProductDto>(mapper.ConfigurationProvider)
                .FirstOrDefaultAsync(x => x.Id == Id);

            if (model == null)
                return NotFound();

            var gallery = await GalleryRepo.TableNoTracking
                .ProjectTo<GalleryDto>(mapper.ConfigurationProvider)
                .Where(x => x.ProductId == Id)
                .ToListAsync();

            model.galleries = gallery;

            model.ShopCartItemCount = 1;

            if (User.Identity?.IsAuthenticated == true)
            {
                var user = await userManager.GetUserAsync(User);

                if (user != null)
                {
                    var shopCart = await ShopCartRepo.TableNoTracking
                        .FirstOrDefaultAsync(x => x.UserId == user.Id);

                    if (shopCart != null)
                    {
                        var shopCartItem = await ShopCardItemRepo.TableNoTracking
                            .FirstOrDefaultAsync(x =>
                                x.ProductId == Id &&
                                x.ShopCartId == shopCart.Id);

                        if (shopCartItem != null)
                            model.ShopCartItemCount = shopCartItem.count;
                    }
                }
            }

            return View("~/Views/Product/ProductDetail.cshtml", model);
        }

        // نمایش همه محصولات
        public async Task<IActionResult> AllProduct(int t, string txt)
        {
            var query = ProductRepo.TableNoTracking.AsQueryable();

            if (!string.IsNullOrEmpty(txt))
                query = query.Where(x => x.Title.Contains(txt));

            if (t == 0)
                query = query.OrderByDescending(x => x.CreationDateTime);
            else if (t == 1)
                query = query.OrderByDescending(x => x.Total);
            else if (t == 2)
                query = query.OrderBy(x => x.Total);

            var products = await query
                .ProjectTo<ProductDto>(mapper.ConfigurationProvider)
                .ToListAsync();

            var model = new AllProductDto
            {
                products = products
            };

            return View(model);
        }

        // نمایش دسته بندی ها
        public async Task<IActionResult> AllCategory(Guid MId)
        {
            var model = new AllProductDto();

            var categories = await CategoryRepo.TableNoTracking
                .Where(x => x.ParentId == MId)
                .ProjectTo<CategoryDto>(mapper.ConfigurationProvider)
                .ToListAsync();

            var products = await ProductRepo.TableNoTracking
                .Where(x => x.CategoryId == MId)
                .ProjectTo<ProductDto>(mapper.ConfigurationProvider)
                .ToListAsync();

            model.Categories = categories;
            model.products = products;

            return View(model);
        }

        // محصولات یک دسته بندی
        public async Task<IActionResult> ProductsByCategory(Guid categoryId, int t = 0)
        {
            var query = ProductRepo.TableNoTracking
                .Where(x => x.CategoryId == categoryId);

            if (t == 0)
                query = query.OrderByDescending(x => x.CreationDateTime);
            else if (t == 1)
                query = query.OrderByDescending(x => x.Total);
            else if (t == 2)
                query = query.OrderBy(x => x.Total);

            var products = await query
                .ProjectTo<ProductDto>(mapper.ConfigurationProvider)
                .ToListAsync();

            var model = new AllProductDto
            {
                products = products
            };

            return View("AllProduct", model);
        }
    }
}