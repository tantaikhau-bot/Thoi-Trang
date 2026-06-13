namespace ThoiTrang.Models.Entities
{
    public class Wishlist
    {
        public int WishlistId { get; set; }
        public int UserId { get; set; }
        public int ProductId { get; set; }
        public DateTime CreatedAt { get; set; }

        public User? User { get; set; }
        public Product? Product { get; set; }
    }
}
