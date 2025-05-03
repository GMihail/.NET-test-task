using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

[Table("products")]
public class Product : BaseModel
{
    [PrimaryKey("id", false)]
    public long Id { get; set; }

    [Column("name")]
    public string Name { get; set; } = "Неизвестный товар";

    [Column("price")]
    public decimal Price { get; set; } = 0;

    [Column("description")]
    public string Description { get; set; } = string.Empty;
}