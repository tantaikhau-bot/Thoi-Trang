using System.ComponentModel.DataAnnotations;

namespace ThoiTrang.Models.Entities
{
    public class Notification
    {
        public int NotificationId { get; set; }
        public int? UserId { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; } = null!;

        [MaxLength(500)]
        public string? Content { get; set; }

        [MaxLength(20)]
        public string Type { get; set; } = "info"; // info/order/promo

        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }

        public User? User { get; set; }
    }
}
