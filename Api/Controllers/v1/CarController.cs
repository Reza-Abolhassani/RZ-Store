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
    public class CarController : BaseController
    {
        private readonly IRepository<Car> CarRepo;
        private readonly IMapper mapper;

        public CarController(IRepository<Car> carRepo, IMapper mapper)
        {
            CarRepo = carRepo;
            this.mapper = mapper;
        }

        [HttpPost("[action]")]
        public ApiResult Add(CarDto dto)
        {
            //var model= mapper.Map<Car>(dto);
            var model = dto.ToEntity(mapper);

            CarRepo.Add(model);

            return Ok();
        }

        [HttpGet("[action]")]
        public ApiResult<List<CarDto>> GetAllCars()
        {
            var model = CarRepo.AllTableNoTracking.ProjectTo<CarDto>(mapper.ConfigurationProvider).ToList();

            //var model = mapper.Map<List<CarDto>>(cars);

            return Ok(model);


        }

    }
}
