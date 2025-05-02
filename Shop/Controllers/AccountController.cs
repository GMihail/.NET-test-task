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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Пытаемся выполнить вход
                var session = await _authService.SignIn(model.Email, model.Password);

                // Если пользователь не найден или пароль неверный
                if (session?.User == null)
                {
                    ModelState.AddModelError(string.Empty, "Неверный email или пароль");
                    return View(model);
                }

                // Успешная авторизация - создаем claims
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
                    ExpiresUtc = DateTime.UtcNow.AddDays(30),
                    AllowRefresh = true
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)),
                    authProperties);

                // Безопасный редирект
                return RedirectToLocal(model.ReturnUrl);
            }
            catch (PostgrestException ex) when (ex.Message.Contains("Invalid login credentials"))
            {
                // Специфичная обработка ошибки неверных учетных данных
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
            return RedirectToAction("Index", "Home");
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