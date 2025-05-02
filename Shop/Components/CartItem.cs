using Microsoft.AspNetCore.Mvc;
using Shop.Services;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Shop.Components 
{
    public class CartCount : ViewComponent
    {
        private readonly CartService _cartService;

        public CartCount(CartService cartService)
        {
            _cartService = cartService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var userId = UserClaimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Content("0");

            try
            {
                var cartItems = await _cartService.GetUserCart(userId);
                return Content(cartItems.Count.ToString());
            }
            catch
            {
                return Content("0");
            }
        }
    }
}