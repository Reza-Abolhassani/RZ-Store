using System;
using System.Collections.Generic;
using WebFramework.Api;
using Entities;

namespace Admin.Models
{
    public class CityDto : BaseDto<CityDto, Entities.City>
    {
        public string Name { get; set; }

        public Guid ProvinceId { get; set; }

        public string ProvincesName { get; set; }

        public List<ProvinceDto> Provinces { get; set; }

        public CityDto()
        {
            Provinces = new List<ProvinceDto>();
        }
    }
}