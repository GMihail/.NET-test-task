using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using Supabase.Postgrest.Models;

namespace Shop.Models
{
    [Table("products")] // Указываем имя таблицы в Supabase
    public class Product : BaseModel
    {
        [PrimaryKey("id")] // Первичный ключ
        public int Id { get; set; }

        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Column("price")]
        public decimal Price { get; set; }

        [Column("description")]
        public string? Description { get; set; }
    }
}