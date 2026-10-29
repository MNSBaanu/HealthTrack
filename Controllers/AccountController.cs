using HealthTrack.Models;
using HealthTrack.Models.Data;
using HealthTrack.Models.ViewModels;
using HealthTrack.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HealthTrack.Controllers
{
    public class AccountController : BaseController
    {
        private readonly ILogger<AccountController> _logger;
        private readonly SignInManager<Person> _signInManager;
        private readonly TimeProvider _clock;

        public AccountController(ILogger<AccountController> logger, HealthTrackDbContext context,
            UserManager<Person> userManager, SignInManager<Person> signInManager, TimeProvider clock)
            : base(context, userManager)
        {
            _logger = logger;
            _signInManager = signInManager;
            _clock = clock;
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
        [EnableRateLimiting("auth")]
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
        public IActionResult Login(string? returnUrl = null, bool temporaryPasswordExpired = false)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (temporaryPasswordExpired)
                ModelState.AddModelError(string.Empty, "Your temporary password has expired. Ask an administrator for a new one.");
            return View();
        }

        [HttpPost]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await UserManager.FindByEmailAsync(model.Email);
            var expired = user != null && TemporaryPasswordPolicy.IsExpired(user, _clock.GetUtcNow().UtcDateTime);
            var temporaryPasswordExpired = false;
            Microsoft.AspNetCore.Identity.SignInResult result;
            if (expired)
            {
                result = await _signInManager.CheckPasswordSignInAsync(user!, model.Password, lockoutOnFailure: true);
                if (result.Succeeded) { temporaryPasswordExpired = true; result = Microsoft.AspNetCore.Identity.SignInResult.NotAllowed; }
            }
            else result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: true);
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
                ModelState.AddModelError(string.Empty, temporaryPasswordExpired
                    ? "Your temporary password has expired. Ask an administrator for a new one." : "Invalid email or password.");
                return View(model);
            }

            user!.LastLoginAt = DateTime.Now;
            await UserManager.UpdateAsync(user);
            if (user.MustChangePassword) return RedirectToAction(nameof(ChangePassword));

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
        [Authorize]
        public async Task<IActionResult> ChangePassword()
        {
            ViewData["RequiredPasswordChange"] = (await UserManager.GetUserAsync(User))?.MustChangePassword == true;
            return View(new ChangePasswordViewModel());
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            var user = await UserManager.GetUserAsync(User);
            if (user == null) return RedirectToAction(nameof(Login));
            var required = user.MustChangePassword;
            ViewData["RequiredPasswordChange"] = required;
            if (required && model.CurrentPassword == model.NewPassword)
                ModelState.AddModelError(nameof(model.NewPassword), "Choose a different password from your temporary password.");
            if (!ModelState.IsValid) return View(model);

            await using var operation = await AtomicOperation.BeginAsync(Context);
            var result = await UserManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);
                return View(model);
            }
            user.MustChangePassword = false;
            user.TemporaryPasswordExpiresAtUtc = null;
            result = await UserManager.UpdateAsync(user);
            if (!result.Succeeded) { AddErrors(result); return View(model); }
            await operation.CommitAsync();

            await _signInManager.RefreshSignInAsync(user);
            _logger.LogInformation("User {UserId} changed their password", user.Id);
            TempData["Success"] = "Password changed.";
            return required ? RedirectToAction("Index", user.DashboardController) : RedirectToAction(nameof(ChangePassword));
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
