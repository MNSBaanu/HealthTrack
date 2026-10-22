using HealthTrack.Models;
using HealthTrack.Models.Data;
using HealthTrack.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HealthTrack.Controllers
{
    public class AccountController : BaseController
    {
        private readonly ILogger<AccountController> _logger;
        private readonly SignInManager<Person> _signInManager;

        public AccountController(ILogger<AccountController> logger, HealthTrackDbContext context,
            UserManager<Person> userManager, SignInManager<Person> signInManager)
            : base(context, userManager)
        {
            _logger = logger;
            _signInManager = signInManager;
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            ValidatePastDate(model.DateOfBirth, nameof(model.DateOfBirth));
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var client = new Client
            {
                UserName = model.Email,
                Email = model.Email,
                Name = model.Name.Trim(),
                DateOfBirth = model.DateOfBirth!.Value,
                HeightCm = model.HeightCm!.Value,
                FitnessGoal = model.FitnessGoal,
                LastLoginAt = DateTime.Now
            };
            client.HealthMetrics.Add(new HealthMetric { WeightKg = model.WeightKg!.Value });

            var result = await UserManager.CreateAsync(client, model.Password);
            if (!result.Succeeded)
            {
                AddErrors(result);
                return View(model);
            }

            await UserManager.AddToRoleAsync(client, AppRoles.Client);
            await _signInManager.SignInAsync(client, isPersistent: false);
            _logger.LogInformation("New client registered: {Email}", client.Email);
            return RedirectToAction("Index", client.DashboardController);
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: true);
            var user = await UserManager.FindByEmailAsync(model.Email);
            if (user != null)
            {
                var agent = Request.Headers.UserAgent.ToString();
                Context.LoginHistories.Add(new LoginHistory
                {
                    PersonId = user.Id,
                    LoggedInAt = DateTime.Now,
                    Succeeded = result.Succeeded,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = agent.Length > 300 ? agent[..300] : agent
                });
                await Context.SaveChangesAsync();
            }
            if (result.IsLockedOut)
            {
                _logger.LogWarning("Locked out account tried to sign in: {Email}", model.Email);
                ModelState.AddModelError(string.Empty, "This account is locked. Try again later or contact an administrator.");
                return View(model);
            }
            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password.");
                return View(model);
            }

            user!.LastLoginAt = DateTime.Now;
            await UserManager.UpdateAsync(user);

            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", user.DashboardController);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
