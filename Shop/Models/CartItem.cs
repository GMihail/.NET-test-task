using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

[Table("cart_items")]
public class CartItem : BaseModel
{
    [PrimaryKey("id", true)]
    [Column("id", ignoreOnInsert: true)] 
    public int Id { get; set; }

    [Column("user_id")]
    public string UserId { get; set; }

    [Column("product_id")]
    public long ProductId { get; set; }

    [Column("quantity")]
    public int Quantity { get; set; } = 1;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}