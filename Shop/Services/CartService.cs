using Shop.Models;
using Supabase;
using Supabase.Postgrest;
using Supabase.Postgrest.Exceptions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Shop.Services
{
    public class CartService
    {
        private readonly Supabase.Client _supabase;
        private readonly ILogger<CartService> _logger;

        public CartService(Supabase.Client supabase, ILogger<CartService> logger)
        {
            _supabase = supabase;
            _logger = logger;
        }

        public async Task AddToCart(string userId, long productId, int quantity = 1)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                    throw new ArgumentException("User ID cannot be empty");

                if (quantity <= 0)
                    throw new ArgumentException("Quantity must be positive");

                // Проверяем существование товара
                var product = await _supabase
                    .From<Product>()
                    .Where(p => p.Id == productId)
                    .Single();

                if (product == null)
                    throw new ArgumentException($"Product with ID {productId} not found");

                // Проверяем наличие товара в корзине
                var existingItem = await _supabase
                    .From<CartItem>()
                    .Where(x => x.UserId == userId && x.ProductId == productId)
                    .Single();

                if (existingItem != null)
                {
                    await UpdateQuantity(existingItem.Id, existingItem.Quantity + quantity);
                    _logger.LogInformation($"Updated quantity for product {productId} in cart of user {userId}");
                }
                else
                {
                    // Создаем объект без указания ID
                    var newItem = new CartItem
                    {
                        UserId = userId,
                        ProductId = productId,
                        Quantity = quantity,
                        CreatedAt = DateTime.UtcNow
                    };

                    // Используем Insert без возврата модели
                    await _supabase
                        .From<CartItem>()
                        .Insert(newItem, new QueryOptions { Returning = QueryOptions.ReturnType.Minimal });

                    _logger.LogInformation($"Added product {productId} to cart of user {userId}");
                }
            }
            catch (PostgrestException ex)
            {
                _logger.LogError(ex, $"Supabase error while adding product {productId} to cart");
                throw new Exception("Database error occurred while updating cart");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error adding product {productId} to cart");
                throw;
            }
        }

        public async Task RemoveFromCart(int cartItemId)
        {
            try
            {
                await _supabase
                    .From<CartItem>()
                    .Where(x => x.Id == cartItemId)
                    .Delete();

                _logger.LogInformation($"Removed cart item {cartItemId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error removing cart item {cartItemId}");
                throw;
            }
        }

        public async Task<List<CartItem>> GetUserCart(string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                    throw new ArgumentException("User ID cannot be empty");

                var response = await _supabase
                    .From<CartItem>()
                    .Where(x => x.UserId == userId)
                    .Order(x => x.CreatedAt, Constants.Ordering.Descending)
                    .Get();

                return response.Models;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting cart for user {userId}");
                throw;
            }
        }

        public async Task<CartItem> UpdateQuantity(int cartItemId, int newQuantity)
        {
            try
            {
                if (newQuantity <= 0)
                    throw new ArgumentException("Quantity must be positive");

                // Обновляем запись
                await _supabase
                    .From<CartItem>()
                    .Where(x => x.Id == cartItemId)
                    .Set(x => x.Quantity, newQuantity)
                    .Update();

                // Получаем обновленную запись
                var updatedItem = await _supabase
                    .From<CartItem>()
                    .Where(x => x.Id == cartItemId)
                    .Single();

                if (updatedItem == null)
                    throw new Exception("Failed to get updated cart item");

                _logger.LogInformation($"Updated quantity for cart item {cartItemId} to {newQuantity}");

                return updatedItem;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating quantity for cart item {cartItemId}");
                throw;
            }
        }
    }
}