using Shop.Models;
using Supabase;
using Supabase.Gotrue;
using Supabase.Gotrue.Interfaces;
using Supabase.Postgrest;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;
using Supabase.Postgrest.Exceptions;
using Microsoft.AspNetCore.Authentication.OAuth;

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

        public async Task<User?> SignUp(string email, string password, string username)
        {
            try
            {
                // 1. Регистрация пользователя
                var response = await _supabase.Auth.SignUp(email, password);

                if (response?.User?.Id == null)
                {
                    Console.WriteLine("ОШИБКА: Не получен ID пользователя");
                    return null;
                }

                // 2. Создание профиля
                await _supabase.From<Profile>()
                    .Insert(new Profile
                    {
                        UserId = response.User.Id, 
                        Username = username
                    }, new QueryOptions
                    {
                        Returning = QueryOptions.ReturnType.Minimal
                    });

                return response.User;
            }
            catch (PostgrestException ex)
            {
                Console.WriteLine($"POSTGREST ERROR: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GENERAL ERROR: {ex.Message}");
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
                // Используем правильный синтаксис для Supabase.Postgrest
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
        public async Task<DateTime> GetUserRegistrationDate(string userId)
        {
            var user = await _supabase.Auth.GetUser(userId);
            return user?.CreatedAt ?? DateTime.UtcNow;
        }
        public async Task<User?> GetUserById(string userId)
        {
            return await _supabase.Auth.GetUser(userId);
        }
    }
}