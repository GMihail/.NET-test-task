using Supabase;

namespace Shop.Services
{
    public class SupabaseService
    {
        public readonly Client Client;

        public SupabaseService(IConfiguration config)
        {
            Client = new Client(
                config["Supabase:Url"],
                config["Supabase:Key"],
                new SupabaseOptions { AutoConnectRealtime = false }
            );

            Client.InitializeAsync().Wait();
        }
    }
}