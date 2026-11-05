using System.Diagnostics;
using HealthTrack.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HealthTrack.Controllers
{
    // Entry point that sends each user to the dashboard for their role.
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly UserManager<Person> _userManager;

        public HomeController(ILogger<HomeController> logger, UserManager<Person> userManager)
        {
            _logger = logger;
            _userManager = userManager;
        }

        // Redirects signed-in users to their dashboard and others to sign-in.
        public IActionResult Index()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction(nameof(Dashboard));
            }
            return RedirectToAction("Login", "Account");
        }

        // Redirects to the dashboard controller that matches the user's role.
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

        // Shows the privacy policy.
        public IActionResult Privacy()
        {
            var configuration = HttpContext.RequestServices.GetRequiredService<IConfiguration>();
            ViewData["ContactEmail"] = configuration["Privacy:ContactEmail"] ?? configuration["AdminAccount:Email"];
            ViewData["RetentionMonths"] = configuration.GetValue<int>("Retention:InactiveClientMonths");
            return View();
        }

        // Shows the generic error page.
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
