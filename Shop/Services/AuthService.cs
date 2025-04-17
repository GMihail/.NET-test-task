using Supabase;
using Supabase.Gotrue;
using Supabase.Postgrest;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace Shop.Services
{
    public class AuthService
    {
        private readonly Supabase.Client _supabase;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuthService(Supabase.Client supabase, IHttpContextAccessor httpContextAccessor)
        {
            _supabase = supabase;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Регистрация нового пользователя
        /// </summary>
        public async Task<User?> SignUp(string email, string password, string username)
        {
            try
            {
                var response = await _supabase.Auth.SignUp(email, password);

                if (response?.User?.Id != null)
                {
                    await _supabase.From<Profile>()
                        .Insert(new Profile
                        {
                            UserId = response.User.Id,
                            Username = username
                        });
                }

                return response?.User;
            }
            catch (Exception ex)
            {
                // Логирование ошибки
                Console.WriteLine($"Ошибка регистрации: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Вход пользователя
        /// </summary>
        public async Task<Session?> SignIn(string email, string password)
        {
            try
            {
                var response = await _supabase.Auth.SignIn(email, password);
                return response;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка входа: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Выход пользователя
        /// </summary>
        public async Task SignOut()
        {
            try
            {
                await _supabase.Auth.SignOut();
                _httpContextAccessor.HttpContext?.Response.Cookies.Delete("sb-access-token");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка выхода: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Получение текущего пользователя
        /// </summary>
        public User? GetCurrentUser()
        {
            return _supabase.Auth.CurrentUser;
        }

        /// <summary>
        /// Получение профиля пользователя
        /// </summary>
        public async Task<Profile?> GetUserProfile(string userId)
        {
            try
            {
                return await _supabase.From<Profile>()
                    .Where(x => x.UserId == userId)
                    .Single();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка получения профиля: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Обновление профиля пользователя
        /// </summary>
        public async Task<Profile?> UpdateProfile(string userId, string newUsername)
        {
            try
            {
                var profile = await GetUserProfile(userId);
                if (profile == null) return null;

                profile.Username = newUsername;
                var response = await _supabase.From<Profile>()
                    .Update(profile);

                return response.Model;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка обновления профиля: {ex.Message}");
                throw;
            }
        }
    }

    /// <summary>
    /// Модель профиля пользователя
    /// </summary>
    [Table("profiles")]
    public class Profile : BaseModel
    {
        [PrimaryKey("user_id", false)]
        public string UserId { get; set; }

        [Column("username")]
        public string Username { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}