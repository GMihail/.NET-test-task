﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shop.Models;
using Shop.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Shop.Controllers
{
    [Authorize]
    public class CartController : Controller
    {
        private readonly CartService _cartService;
        private readonly Supabase.Client _supabase;
        private readonly ILogger<CartController> _logger;

        public CartController(
            CartService cartService,
            Supabase.Client supabase,
            ILogger<CartController> logger)
        {
            _cartService = cartService;
            _supabase = supabase;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null) return Challenge();

                var cartItems = await _cartService.GetUserCart(userId);
                if (!cartItems.Any())
                {
                    return View(new List<CartItemViewModel>());
                }

                var model = await BuildCartViewModel(userId, cartItems);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка загрузки корзины");
                TempData["Error"] = "Не удалось загрузить корзину";
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(long productId, int quantity = 1)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (string.IsNullOrEmpty(userId))
                    return Challenge();

                if (!ValidateQuantity(quantity))
                {
                    TempData["Error"] = "Недопустимое количество";
                    return RedirectToAction("Index", "Products");
                }

                await _cartService.AddToCart(userId, productId, quantity);
                TempData["Success"] = "Товар успешно добавлен в корзину";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка добавления товара {productId} в корзину");
                TempData["Error"] = ex is ArgumentException ? ex.Message : "Ошибка добавления в корзину";
                return RedirectToAction("Index", "Products");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int cartItemId)
        {
            try
            {
                await _cartService.RemoveFromCart(cartItemId);
                TempData["Success"] = "Товар удален из корзины";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка удаления из корзины: {cartItemId}");
                TempData["Error"] = "Ошибка при удалении из корзины";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(int cartItemId, int quantity)
        {
            try
            {
                if (!ValidateQuantity(quantity))
                {
                    TempData["Error"] = "Количество должно быть от 1 до 100";
                    return RedirectToAction("Index");
                }

                await _cartService.UpdateQuantity(cartItemId, quantity);
                TempData["Success"] = "Количество обновлено";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка обновления количества: {cartItemId}");
                TempData["Error"] = "Ошибка при обновлении количества";
                return RedirectToAction("Index");
            }
        }

        private string? GetCurrentUserId()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        private bool ValidateQuantity(int quantity)
        {
            return quantity > 0 && quantity <= 100;
        }

        private async Task<List<CartItemViewModel>> BuildCartViewModel(string userId, List<CartItem> cartItems)
        {
            var productIds = cartItems.Select(i => i.ProductId).Distinct().ToList();
            var products = new List<Product>();
            foreach (var id in productIds)
            {
                var response = await _supabase
                    .From<Product>()
                    .Where(x => x.Id == id)
                    .Get();

                if (response.Model != null)
                    products.Add(response.Model);
            }

            return cartItems.Select(cartItem =>
            {
                var product = products.FirstOrDefault(x => x.Id == cartItem.ProductId);

                return new CartItemViewModel
                {
                    Id = cartItem.Id,
                    Product = product ?? new Product
                    {
                        Id = cartItem.ProductId,
                        Name = "[Товар недоступен]",
                        Price = 0,
                        Description = "Этот товар был удален или временно недоступен"
                    },
                    Quantity = cartItem.Quantity
                };
            }).ToList();
        }
    }
}