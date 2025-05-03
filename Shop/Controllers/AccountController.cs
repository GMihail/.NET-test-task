using Microsoft.AspNetCore.Mvc;
using Shop.Services;
using Shop.Models;
using System.Threading.Tasks;
using Supabase.Gotrue;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Supabase.Postgrest.Exceptions;


namespace Shop.Controllers
{
    public class AccountController : Controller
    {
        private readonly AuthService _authService;

        public AccountController(AuthService authService)
        {
            _authService = authService;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginModel model)
        {
            if (!ModelState.IsValid) //Validation
            {
                return View(model);
            }

            try
            {
                var session = await _authService.SignIn(model.Email, model.Password);
                if (session?.User == null)
                {
                    ModelState.AddModelError(string.Empty, "Неверный email или пароль");
                    return View(model);
                }

                var profile = await _authService.GetUserProfile(session.User.Id);
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, session.User.Id),
                    new Claim(ClaimTypes.Name, profile?.Username ?? session.User.Email),
                    new Claim(ClaimTypes.Email, session.User.Email),
                    new Claim("auth_time", DateTime.UtcNow.ToString("o"))
                };

                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = model.RememberMe,
                    ExpiresUtc = DateTime.UtcNow.AddDays(5),
                    AllowRefresh = true
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)),
                    authProperties);
                return RedirectToLocal(model.ReturnUrl);
            }
            catch (PostgrestException ex) when (ex.Message.Contains("Invalid login credentials"))
            {
                ModelState.AddModelError(string.Empty, "Неверный email или пароль");
                return View(model);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Произошла ошибка при входе. Попробуйте позже.");
                return View(model);
            }
        }

        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _authService.SignUp(model.Email, model.Password, model.Username);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Ошибка при регистрации пользователя");
                return View(model);
            }
            await _authService.SignIn(model.Email, model.Password);
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            await _authService.SignOut();
            return RedirectToAction("Index", "Home");
        }

        [Authorize]
        public IActionResult Profile()
        {
            return View(new ProfileViewModel
            {
                Username = User.Identity?.Name ?? "Гость",
                Email = User.FindFirst(ClaimTypes.Email)?.Value ?? "Не указан",
                RegisteredDate = DateTime.Now
            });
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}