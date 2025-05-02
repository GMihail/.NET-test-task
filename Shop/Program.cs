using Microsoft.AspNetCore.Authentication.Cookies;
using Shop.Services;
using Shop.Components;
using Supabase.Gotrue;
using Supabase.Gotrue.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Конфигурация сервисов
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();

// Регистрация компонентов и сервисов
builder.Services.AddScoped<CartCount>();
builder.Services.AddScoped<CartService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddSingleton<SupabaseService>();

// Настройка Supabase
var supabaseUrl = builder.Configuration["Supabase:Url"]
    ?? throw new ArgumentNullException("Supabase:Url не настроен");
var supabaseKey = builder.Configuration["Supabase:Key"]
    ?? throw new ArgumentNullException("Supabase:Key не настроен");

builder.Services.AddSingleton(provider => new Supabase.Client(
    supabaseUrl,
    supabaseKey,
    new Supabase.SupabaseOptions
    {
        AutoConnectRealtime = false,
        AutoRefreshToken = true,
        SessionHandler = new SupabaseSessionHandler(
            provider.GetRequiredService<IHttpContextAccessor>())
    }));

// Настройка аутентификации
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "Shop.Auth";
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.SameSite = SameSiteMode.Lax;
    });

var app = builder.Build();

// Конвейер middleware
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

public class SupabaseSessionHandler : IGotrueSessionPersistence<Session>
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public SupabaseSessionHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public void SaveSession(Session session)
    {
        if (_httpContextAccessor.HttpContext == null || string.IsNullOrEmpty(session?.AccessToken))
            return;

        _httpContextAccessor.HttpContext.Response.Cookies.Append(
            "sb-access-token",
            session.AccessToken,
            new CookieOptions
            {
                Expires = DateTimeOffset.Now.AddSeconds(session.ExpiresIn),
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict
            });
    }

    public void DestroySession()
    {
        _httpContextAccessor.HttpContext?.Response.Cookies.Delete("sb-access-token");
    }

    public Session? LoadSession()
    {
        return _httpContextAccessor.HttpContext?.Request.Cookies["sb-access-token"] is string token
            ? new Session { AccessToken = token }
            : null;
    }
}