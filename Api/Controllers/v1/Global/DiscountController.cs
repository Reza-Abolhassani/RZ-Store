using API.Models.Globals;

using AutoMapper;

using Data.Repositories;

using Entities.Setting;

using Microsoft.AspNetCore.Mvc;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using WebFramework.Api;

namespace API.Controllers.v1.Global
{
    public class DiscountController : CrudController<DiscountCodeDto, DiscountCode>
    {
        private readonly IRepository<DiscountCode> repository;
        private readonly IMapper mapper;
        public DiscountController(IRepository<DiscountCode> repository, IMapper mapper) : base(repository, mapper)
        {
            this.repository = repository;
            this.mapper = mapper;
        }

       

    }
}
