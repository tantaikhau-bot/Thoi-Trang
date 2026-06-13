namespace ThoiTrang.Models.Entities
{
    public class CartItem
    {
        public int CartItemId { get; set; }
        public int UserId { get; set; }
        public int VariantId { get; set; }
        public int Quantity { get; set; } = 1;
        public DateTime CreatedAt { get; set; }

        public User? User { get; set; }
        public ProductVariant? Variant { get; set; }
    }
}
