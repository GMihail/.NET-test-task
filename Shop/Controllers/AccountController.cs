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


namespace Shop.Controllers // Важно: контроллеры должны быть в пространстве имен Controllers
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
        public async Task<IActionResult> Login(LoginModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var session = await _authService.SignIn(model.Email, model.Password);
            if (session == null)
            {
                ModelState.AddModelError(string.Empty, "Неверный email или пароль");
                return View(model);
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, session.User.Id),
                new Claim(ClaimTypes.Name, session.User.Email),
                new Claim(ClaimTypes.Email, session.User.Email)
            };

            var identity = new ClaimsIdentity(claims,
                CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                new AuthenticationProperties
                {
                    IsPersistent = model.RememberMe,
                    ExpiresUtc = DateTime.UtcNow.AddDays(30)
                });

            return LocalRedirect(model.ReturnUrl ?? Url.Content("~/"));
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

            // Автоматический вход после регистрации
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
            // Используем только данные из куки
            return View(new ProfileViewModel
            {
                Username = User.Identity?.Name ?? "Гость",
                Email = User.Identity?.Name ?? "Не указан",
                RegisteredDate = DateTime.Now
            });
        }

        // Обработка случая, когда доступ запрещен
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}