using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Shop.Services;
using Supabase.Gotrue.Interfaces;
using Supabase.Gotrue;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Конфигурация сервисов
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();

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
        AutoConnectRealtime = true,
        AutoRefreshToken = true,
        SessionHandler = new SupabaseSessionHandler(
            provider.GetRequiredService<IHttpContextAccessor>())
    }));

// Настройка аутентификации
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(supabaseKey)),
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                context.Token = context.Request.Cookies["sb-access-token"];
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// Регистрация сервисов
builder.Services.AddScoped<AuthService>();
builder.Services.AddSingleton<SupabaseService>();

var app = builder.Build();

// Конвейер middleware
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "account",
    pattern: "account/{action=Login}",
    defaults: new { controller = "Account" });

app.UseStatusCodePagesWithRedirects("/Account/AccessDenied");

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