using Data.Repositories;
using Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace Admin.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminDashboardController : Controller
    {
        private readonly UserManager<User> userManager;
        private readonly IRepository<Product> ProductRepo;
        private readonly IRepository<Order> OrderRepo;

        public AdminDashboardController(UserManager<User> userManager, IRepository<Product> productRepo, IRepository<Order> orderRepo)
        {
            this.userManager = userManager;
            ProductRepo = productRepo;
            OrderRepo = orderRepo;
        }

        public async Task<IActionResult> Index()
        {

            //return Content("AdminDashboard Index Action Executed Successfully");
            var userCount = await userManager.Users.CountAsync();

            var productCount = await ProductRepo.AllTableNoTracking.CountAsync();

            var orderCount = await OrderRepo.AllTableNoTracking.CountAsync();

            ViewBag.UserCount = userCount;
            ViewBag.ProductCount = productCount;
            ViewBag.OrderCount = orderCount;

            return View();
        }
    }
}
