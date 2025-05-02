using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Shop.Models
{
    [Table("profiles")]
    public class Profile : BaseModel
    {
        [PrimaryKey("user_id", false)]
        [Column("user_id")]
        public string UserId { get; set; }

        [Column("username")]
        public string Username { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

