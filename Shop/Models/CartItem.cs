using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

[Table("cart_items")]
public class CartItem : BaseModel
{
    [PrimaryKey("id", false)]
    public long Id { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("product_id")]
    public long ProductId { get; set; }

    [Column("quantity")]
    public int Quantity { get; set; } = 1;

    [Reference(typeof(Product))]
    public Product Product { get; set; }
}