using Entities;
using System;
using System.Collections.Generic;
using WebFramework.Api;

namespace Admin.Models
{
    public class AddressDto : BaseDto<AddressDto, Address>
    {
        public Guid CityId { get; set; }
        public string CitiesName { get; set; }
        public string ProvincesName { get; set; }
        public List<CityDto> Cities { get; set; }
        public List<ProvinceDto> Provinces { get; set; }
        public Guid ProvinceId { get; set; }


        public Guid UserId { get; set; }
        public string UsersName { get; set; }
        public List<UserDto> Users { get; set; }

        public string FullAddress { get; set; }
        public String PostalCode { get; set; }
        public String Title { get; set; }



        public AddressDto()
        {
            Users = new List<UserDto>();
            Cities = new List<CityDto>();
            Provinces = new List<ProvinceDto>();
        }
    }
}
