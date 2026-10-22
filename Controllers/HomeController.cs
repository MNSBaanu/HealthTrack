using System.Diagnostics;
using HealthTrack.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HealthTrack.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly UserManager<Person> _userManager;

        public HomeController(ILogger<HomeController> logger, UserManager<Person> userManager)
        {
            _logger = logger;
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction(nameof(Dashboard));
            }
            return RedirectToAction("Login", "Account");
        }

        [Authorize]
        public async Task<IActionResult> Dashboard()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }
            return RedirectToAction("Index", user.DashboardController);
        }

        public IActionResult Privacy()
        {
            var configuration = HttpContext.RequestServices.GetRequiredService<IConfiguration>();
            ViewData["ContactEmail"] = configuration["Privacy:ContactEmail"] ?? configuration["AdminAccount:Email"];
            ViewData["RetentionMonths"] = configuration.GetValue<int>("Retention:InactiveClientMonths");
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
