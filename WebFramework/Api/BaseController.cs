using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebFramework.Api
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]// api/v1/[controller]
    public class BaseController : ControllerBase
    {
        //public UserRepository UserRepository { get; set; } => property injection
        public bool UserIsAutheticated => HttpContext.User.Identity.IsAuthenticated;
    }
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]// api/v1/[controller]
    public class ClientApiBaseController : ControllerBase
    {
        //public UserRepository UserRepository { get; set; } => property injection
        public bool UserIsAutheticated => HttpContext.User.Identity.IsAuthenticated;
    }

    [Authorize(Roles = "Admin")]
    public class AdminBaseController : Controller
    {

    }
}
