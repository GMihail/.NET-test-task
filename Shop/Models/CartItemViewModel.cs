namespace Shop.Models
{
    public class CartItemViewModel
    {
        public int Id { get; set; }
        public Product Product { get; set; }
        public int Quantity { get; set; }
        public decimal TotalPrice => Product.Price * Quantity;
    }
}