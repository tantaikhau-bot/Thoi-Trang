using System.ComponentModel.DataAnnotations;

namespace ThoiTrang.Models.Entities
{
    public class Review
    {
        public int ReviewId { get; set; }
        public int ProductId { get; set; }
        public int? UserId { get; set; }
        public int? OrderId { get; set; }

        public byte Rating { get; set; } // 1..5

        public string? Content { get; set; }

        public bool IsVerified { get; set; }
        public int HelpfulCount { get; set; }
        public DateTime CreatedAt { get; set; }

        // Đường dẫn ảnh, phân cách bởi dấu phẩy (ví dụ: /uploads/reviews/abc.jpg,/uploads/reviews/xyz.jpg)
        public string? Images { get; set; }

        public Product? Product { get; set; }
        public User? User { get; set; }
    }
}
