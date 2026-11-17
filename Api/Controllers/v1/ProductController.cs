using API.Models;

using AutoMapper;
using AutoMapper.QueryableExtensions;

using Common.Utilities;

using Data.Repositories;

using Entities;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using WebFramework.Api;

namespace API.Controllers.v1
{
    [ApiVersion("1")]
    [Authorize(Roles ="Admin")]
    public class ProductController : BaseController
    {

        private readonly IRepository<Product> ProductRepo;
        private readonly IMapper mapper;

        public ProductController(IRepository<Product> productRepo, IMapper mapper)
        {
            ProductRepo = productRepo;
            this.mapper = mapper;
        }


        [HttpPost("[action]")]
        public async Task<ApiResult> AddEdit(ProductDto Dto)
        {
            Dto.TotalPrice = Dto.RawPrice - Dto.Discount;


            if (Dto.Id == Guid.Empty)
            {
                //add

                var model = Dto.ToEntity(mapper);
                await ProductRepo.AddAsync(model, default);
            }
            else
            {
                //edit

                var model = await ProductRepo.AllTableNoTracking.FirstOrDefaultAsync(x => x.Id == Dto.Id, default);

                if (Dto.ImageFile == null)
                {
                    Dto.ImageUrl = model.ImageUrl;
                }
                else
                {
                    Dto.ImageUrl = await UploadImage.SaveImage(Dto.ImageFile, "productimages");
                }

                model = Dto.ToEntity(mapper, model);

                await ProductRepo.UpdateAsync(model, default);
            }

            return Ok();
        }

        [HttpGet("[action]")]
        public async Task<ApiResult<List<ProductDto>>> GetAll()
        {
            var model = await ProductRepo.AllTableNoTracking.ProjectTo<ProductDto>(mapper.ConfigurationProvider).ToListAsync(default);
            return Ok(model);
        }

        [HttpPost("[action]")]
        public async Task<ApiResult<List<ProductDto>>> SearchFilter(ProductSearchDto dto)
        {
            //toye proje ghabli mvc mishe in => Db.Products.AsQueryable()
            var query = ProductRepo.TableNoTracking;

            if (dto.Text.HasValue())
            {
                query = query.Where(x => x.Name.Contains(dto.Text));
            }

            if (dto.FromPrice > 0)
            {
                query = query.Where(x => x.TotalPrice >= dto.FromPrice);
            }

            if (dto.ToPrice > 0)
            {
                query = query.Where(x => x.TotalPrice <= dto.ToPrice);
            }

            if (dto.FromCount > 0)
            {
                query = query.Where(x => x.Count >= dto.FromCount);
            }

            if (dto.ToCount > 0)
            {
                query = query.Where(x => x.Count <= dto.ToCount);
            }

            if (dto.CategoryId != Guid.Empty)
            {
                query = query.Where(x => x.ProductCategoryId == dto.CategoryId);
            }


            var model = await query.ProjectTo<ProductDto>(mapper.ConfigurationProvider).ToListAsync();

            return Ok(model);
        }

        [HttpGet("[action]")]
        public async Task<ApiResult<ProductDto>> GetById(Guid Id)
        {
            var model = await ProductRepo.TableNoTracking.ProjectTo<ProductDto>(mapper.ConfigurationProvider).FirstOrDefaultAsync(x => x.Id == Id);
            return Ok(model);
        }

    }
}
