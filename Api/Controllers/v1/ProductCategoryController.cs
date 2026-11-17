using API.Models;

using AutoMapper;

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
    [AllowAnonymous]
    [ApiVersion("1")]
    public class ProductCategoryController : BaseController
    {
        private readonly IRepository<ProductCategory> _ProductCategoryRepo;
        private readonly IMapper mapper;
        public ProductCategoryController(IRepository<ProductCategory> productCategoryRepo, IMapper mapper)
        {
            _ProductCategoryRepo = productCategoryRepo;
            this.mapper = mapper;
        }


        [HttpGet("[action]")]
        public async Task<ApiResult<List<ProductCategoryDto>>> GetAll(Guid MId)
        {
            var categories = await _ProductCategoryRepo.TableNoTracking.Where(x => x.MotherId == MId).ToListAsync();
            var model = mapper.Map<List<ProductCategoryDto>>(categories);
            return Ok(model);

            //return BadRequest();
            //return NotFound();
        }

        [HttpPost("[action]")]
        public async Task<ApiResult> Add(ProductCategoryDto dto)
        {
            var model = mapper.Map<ProductCategory>(dto);

            _ProductCategoryRepo.Add(model);

            return Ok();
        }

        [HttpGet("[action]")]
        public async Task<ApiResult<ProductCategoryDto>> GetById(Guid Id)
        {
            var category = await _ProductCategoryRepo.TableNoTracking.FirstOrDefaultAsync(x => x.Id == Id);
            var model = mapper.Map<ProductCategoryDto>(category);
            return Ok(model);
        }

        [HttpPost("[action]")]
        public async Task<ApiResult> Update(ProductCategoryDto dto)
        {
            //lambda expression - LINQ
            var find = await _ProductCategoryRepo.TableNoTracking.FirstOrDefaultAsync(x => x.Id == dto.Id);

            find.Name = dto.Name;

            _ProductCategoryRepo.Update(find);

            return Ok();
        }
        [HttpGet("[action]")]
        public async Task<ApiResult> ArchiveOrUnArchive(Guid Id)
        {
            var category = await _ProductCategoryRepo.AllTableNoTracking.FirstOrDefaultAsync(x => x.Id == Id);

            category.Active = !category.Active;

            _ProductCategoryRepo.Update(category);

            return Ok();
        }

        [HttpGet("[action]")]
        public async Task<ApiResult> Delete(Guid Id)
        {
            var category = await _ProductCategoryRepo.TableNoTracking.FirstOrDefaultAsync(x => x.Id == Id);

            _ProductCategoryRepo.Delete(category);

            return Ok();
        }

    }
}
