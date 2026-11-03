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
            if (result.RequiresTwoFactor)
            {
                return RedirectToAction(nameof(LoginWith2fa), new { returnUrl, rememberMe = model.RememberMe });
            }
            if (user != null) await RecordLoginAsync(user, result.Succeeded);
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

            return await FinishSignInAsync(user!, returnUrl);
        }

        [HttpGet]
        public async Task<IActionResult> LoginWith2fa(string? returnUrl = null, bool rememberMe = false)
        {
            if (await _signInManager.GetTwoFactorAuthenticationUserAsync() == null) return RedirectToAction(nameof(Login));
            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginWith2faViewModel { RememberMe = rememberMe });
        }

        [HttpPost]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> LoginWith2fa(LoginWith2faViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null) return RedirectToAction(nameof(Login));
            if (!ModelState.IsValid) return View(model);

            var entered = model.Code.Trim().Replace(" ", string.Empty);
            var digits = entered.Replace("-", string.Empty);
            var result = digits.Length == 6 && digits.All(char.IsDigit)
                ? await _signInManager.TwoFactorAuthenticatorSignInAsync(digits, model.RememberMe, rememberClient: false)
                : await _signInManager.TwoFactorRecoveryCodeSignInAsync(entered);
            await RecordLoginAsync(user, result.Succeeded);
            if (result.IsLockedOut)
            {
                ModelState.AddModelError(string.Empty, "This account is locked. Try again later or contact an administrator.");
                return View(model);
            }
            if (!result.Succeeded)
            {
                ModelState.AddModelError(nameof(model.Code), "That code is not valid. Check your authenticator app and try again.");
                return View(model);
            }
            return await FinishSignInAsync(user, returnUrl);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> EnableAuthenticator()
        {
            var user = await UserManager.GetUserAsync(User);
            if (user == null) return RedirectToAction(nameof(Login));
            if (user.TwoFactorEnabled) return RedirectToAction(nameof(ChangePassword));
            return View(await BuildAuthenticatorModelAsync(user));
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> EnableAuthenticator(EnableAuthenticatorViewModel model)
        {
            var user = await UserManager.GetUserAsync(User);
            if (user == null) return RedirectToAction(nameof(Login));
            if (ModelState.IsValid)
            {
                var code = model.Code.Replace(" ", string.Empty).Replace("-", string.Empty);
                if (await UserManager.VerifyTwoFactorTokenAsync(user, UserManager.Options.Tokens.AuthenticatorTokenProvider, code))
                {
                    await UserManager.SetTwoFactorEnabledAsync(user, true);
                    var codes = await UserManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
                    await _signInManager.RefreshSignInAsync(user);
                    _logger.LogInformation("User {UserId} turned on two-factor sign-in", user.Id);
                    return View("RecoveryCodes", codes?.ToArray() ?? Array.Empty<string>());
                }
                ModelState.AddModelError(nameof(model.Code), "That code is not valid. Check that the time on your phone is correct and try again.");
            }
            var page = await BuildAuthenticatorModelAsync(user);
            page.Code = model.Code;
            return View(page);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> GenerateRecoveryCodes()
        {
            var user = await UserManager.GetUserAsync(User);
            if (user == null) return RedirectToAction(nameof(Login));
            if (!user.TwoFactorEnabled) return RedirectToAction(nameof(ChangePassword));
            var codes = await UserManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
            return View("RecoveryCodes", codes?.ToArray() ?? Array.Empty<string>());
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> DisableTwoFactor()
        {
            var user = await UserManager.GetUserAsync(User);
            if (user == null) return RedirectToAction(nameof(Login));
            if (user is Admin && (await SystemSettings.LoadAsync(Context)).RequireAdminTwoFactor)
            {
                TempData["Error"] = "Two-factor sign-in is required for administrators, so it can't be turned off.";
                return RedirectToAction(nameof(ChangePassword));
            }
            await UserManager.SetTwoFactorEnabledAsync(user, false);
            await UserManager.ResetAuthenticatorKeyAsync(user);
            await _signInManager.RefreshSignInAsync(user);
            _logger.LogInformation("User {UserId} turned off two-factor sign-in", user.Id);
            TempData["Success"] = "Two-factor sign-in turned off.";
            return RedirectToAction(nameof(ChangePassword));
        }

        private async Task<EnableAuthenticatorViewModel> BuildAuthenticatorModelAsync(Person user)
        {
            var key = await UserManager.GetAuthenticatorKeyAsync(user);
            if (string.IsNullOrEmpty(key))
            {
                await UserManager.ResetAuthenticatorKeyAsync(user);
                await _signInManager.RefreshSignInAsync(user);
                key = await UserManager.GetAuthenticatorKeyAsync(user) ?? string.Empty;
            }
            var label = Uri.EscapeDataString("HealthTrack:" + user.Email);
            var uri = $"otpauth://totp/{label}?secret={key}&issuer=HealthTrack&digits=6";
            using var generator = new QRCoder.QRCodeGenerator();
            using var data = generator.CreateQrCode(uri, QRCoder.QRCodeGenerator.ECCLevel.Q);
            var png = new QRCoder.PngByteQRCode(data).GetGraphic(5);
            return new EnableAuthenticatorViewModel
            {
                SharedKey = string.Join(" ", key.ToLowerInvariant().Chunk(4).Select(c => new string(c))),
                QrCodeDataUri = "data:image/png;base64," + Convert.ToBase64String(png)
            };
        }

        private async Task RecordLoginAsync(Person user, bool succeeded)
        {
            var agent = Request.Headers.UserAgent.ToString();
            Context.LoginHistories.Add(new LoginHistory
            {
                PersonId = user.Id,
                LoggedInAt = DateTime.Now,
                Succeeded = succeeded,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = agent.Length > 300 ? agent[..300] : agent
            });
            await Context.SaveChangesAsync();
        }

        private async Task<IActionResult> FinishSignInAsync(Person user, string? returnUrl)
        {
            user.LastLoginAt = DateTime.Now;
            await UserManager.UpdateAsync(user);
            if (user.MustChangePassword) return RedirectToAction(nameof(ChangePassword));
            if (Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
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
