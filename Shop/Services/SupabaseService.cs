using System;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Supabase;
using Supabase.Gotrue;
using Supabase.Gotrue.Interfaces;

namespace Shop.Services
{
    public class SupabaseService
    {
        public readonly Supabase.Client Client;
        private readonly ILogger<SupabaseService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public SupabaseService(
            IConfiguration config,
            ILogger<SupabaseService> logger,
            IHttpContextAccessor httpContextAccessor)
        {
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;

            try
            {
                Client = new Supabase.Client(
                    config["Supabase:Url"] ?? throw new ArgumentNullException("Supabase:Url is missing"),
                    config["Supabase:Key"] ?? throw new ArgumentNullException("Supabase:Key is missing"),
                    new Supabase.SupabaseOptions
                    {
                        AutoConnectRealtime = false,
                        AutoRefreshToken = true,
                        SessionHandler = new SupabaseSessionHandler(_httpContextAccessor)
                    });

                Client.InitializeAsync().Wait();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Supabase initialization failed");
                throw;
            }
        }
    }

    public class SupabaseSessionHandler : IGotrueSessionPersistence<Session>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public SupabaseSessionHandler(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public void SaveSession(Session session)
        {
            _httpContextAccessor.HttpContext?.Response.Cookies.Append(
                "sb-session",
                JsonSerializer.Serialize(session),
                new CookieOptions
                {
                    Expires = DateTimeOffset.Now.AddDays(30),
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict
                });
        }

        public void DestroySession()
        {
            _httpContextAccessor.HttpContext?.Response.Cookies.Delete("sb-session");
        }

        public Session? LoadSession()
        {
            return _httpContextAccessor.HttpContext?.Request.Cookies["sb-session"] is string sessionJson
                ? JsonSerializer.Deserialize<Session>(sessionJson)
                : null;
        }
    }
}