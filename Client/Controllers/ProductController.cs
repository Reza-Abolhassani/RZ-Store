using AutoMapper;
using AutoMapper.QueryableExtensions;
using Client.Models;
using Data.Repositories;
using Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace Client.Controllers
{
    public class ProductController : Controller
    {
        private readonly IRepository<Product> ProductRepo;
        private readonly IMapper mapper;

        public ProductController(IRepository<Product> productRepo, IMapper mapper)
        {
            ProductRepo = productRepo;
            this.mapper = mapper;
        }

        public async Task<IActionResult> ProductDetail(Guid Id)
        {
            var model = await ProductRepo.TableNoTracking
                .ProjectTo<ProductDto>(mapper.ConfigurationProvider)
                .FirstOrDefaultAsync(x => x.Id == Id);

            if (model == null)
                return NotFound();

            return View(model);
        }
    }
}
