using Shop.Models;
using Supabase;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Shop.Services
{
    public class CartService
    {
        private readonly Supabase.Client _supabase;

        public CartService(Supabase.Client supabase)
        {
            _supabase = supabase;
        }

        public async Task AddToCart(string userId, long productId, int quantity = 1)
        {
            // Проверяем, не добавлен ли уже товар
            var existingResponse = await _supabase
                .From<CartItem>()
                .Where(x => x.UserId == userId && x.ProductId == productId)
                .Get();

            if (existingResponse.Models.Count > 0)
            {
                var existingItem = existingResponse.Models[0];
                await UpdateQuantity(existingItem.Id, existingItem.Quantity + quantity);
                return;
            }

            // Добавляем новый товар
            await _supabase
                .From<CartItem>()
                .Insert(new CartItem
                {
                    UserId = userId,
                    ProductId = productId,
                    Quantity = quantity
                });
        }

        public async Task RemoveFromCart(int cartItemId)
        {
            await _supabase
                .From<CartItem>()
                .Where(x => x.Id == cartItemId)
                .Delete();
        }

        public async Task<List<CartItem>> GetUserCart(string userId)
        {
            var response = await _supabase
                .From<CartItem>()
                .Where(x => x.UserId == userId)
                .Get();

            return response.Models;
        }

        public async Task UpdateQuantity(int cartItemId, int newQuantity)
        {
            await _supabase
                .From<CartItem>()
                .Where(x => x.Id == cartItemId)
                .Set(x => x.Quantity, newQuantity)
                .Update();
        }
    }
}