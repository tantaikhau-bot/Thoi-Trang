namespace ThoiTrang.Models.Entities
{
    public class PaymentMethod
    {
        public int PaymentMethodId { get; set; }
        public int UserId { get; set; }
        public string Type { get; set; } = "momo";   // momo / bank / visa / cod
        public string Label { get; set; } = null!;     // VD: "Momo", "Visa Card"
        public string? Detail { get; set; }            // VD: "0901 *** 567", "**** 4242"
        public bool IsDefault { get; set; }
        public DateTime CreatedAt { get; set; }

        public User? User { get; set; }
    }
}
