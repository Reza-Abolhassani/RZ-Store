using API.Models;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Data.Repositories;
using Entities;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebFramework.Api;

namespace API.Controllers.v1
{
    public class CategoryBookController : BaseController
    {
        private readonly IRepository<CategoryBook> CategoryBookRepo;
        private readonly IMapper mapper;

        public CategoryBookController(IRepository<CategoryBook> categoryBookRepo, IMapper mapper)
        {
            CategoryBookRepo = categoryBookRepo;
            this.mapper = mapper;
        }

        [HttpPost("[action]")]

        public async Task<ApiResult> Add(CategoryBookDto dto)
        {
            var model = dto.ToEntity(mapper);

            CategoryBookRepo.Add(model);

            return Ok();
        }

        [HttpGet("[action]")]
        public async Task<ApiResult<List<CategoryBookDto>>> GetAll()
        {
            var model = CategoryBookRepo.TableNoTracking.ProjectTo<CategoryBookDto>
                (mapper.ConfigurationProvider)
                .OrderBy(x=>x.Order)
                .ToList();

            return Ok(model);
        }
    }
}
