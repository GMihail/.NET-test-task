using Microsoft.AspNetCore.Mvc;
using Shop.Services;
using Shop.Models;
using System.Threading.Tasks;
using Supabase.Gotrue; // Добавляем для работы с Session

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
        [ValidateAntiForgeryToken] // Защита от CSRF
        public async Task<IActionResult> Login(LoginModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
                return View(model);

            var session = await _authService.SignIn(model.Email, model.Password);
            if (session == null)
            {
                ModelState.AddModelError(string.Empty, "Неверный email или пароль");
                return View(model);
            }

            return LocalRedirect(returnUrl ?? Url.Content("~/"));
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
            await _authService.SignOut();
            return RedirectToAction("Index", "Home");
        }

        // Обработка случая, когда доступ запрещен
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}