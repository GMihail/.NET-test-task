using Supabase; // Основной клиент
using Supabase.Gotrue.Interfaces;
using Microsoft.Extensions.Logging;
using Supabase.Gotrue;

namespace Shop.Services
{
    public class SupabaseService
    {
        // Явно указываем пространство имен Supabase.Client
        public readonly Supabase.Client Client;
        private readonly ILogger<SupabaseService> _logger;

        public SupabaseService(IConfiguration config, ILogger<SupabaseService> logger)
        {
            _logger = logger;

            try
            {
                // Явное указание типа Supabase.Client
                Client = new Supabase.Client(
                    config["Supabase:Url"] ?? throw new ArgumentNullException("Supabase:Url is missing"),
                    config["Supabase:Key"] ?? throw new ArgumentNullException("Supabase:Key is missing"),
                    new Supabase.SupabaseOptions
                    {
                        AutoConnectRealtime = false,
                        AutoRefreshToken = true,
                        SessionHandler = new SupabaseSessionHandler()
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
        private Session? _currentSession;

        public void SaveSession(Session session)
        {
            _currentSession = session;
        }

        public void DestroySession()
        {
            _currentSession = null;
        }

        public Session? LoadSession()
        {
            return _currentSession;
        }
    }
}