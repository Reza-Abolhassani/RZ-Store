using API.Models;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Data.Repositories;
using Entities;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using WebFramework.Api;

namespace API.Controllers.v1
{
    [ApiVersion("1")]
    public class CarTwoController : Controller
    {
        private readonly IRepository<CarTwo> CarTwoRepo;
        private readonly IMapper mapper;

        public CarTwoController(IRepository<CarTwo> carTwoRepo, IMapper mapper)
        {
            CarTwoRepo = carTwoRepo;
            this.mapper = mapper;
        }

        [HttpPost("[action]")]
        public ApiResult Add(CarTwoDto dto)
        {
            var model = dto.ToEntity(mapper);

            CarTwoRepo.Add(model);

            return Ok();
        }

        [HttpGet("[action]")]
        public ApiResult<List<CarTwoDto>> GetAllCar()
        {
            var model = CarTwoRepo.AllTableNoTracking.ProjectTo<CarTwoDto>(mapper.ConfigurationProvider).ToList();

            return Ok(model);
        }
    }
}
