using System.ComponentModel.DataAnnotations;

namespace ThoiTrang.Models.Entities
{
    public class User
    {
        public int UserId { get; set; }

        [Required, MaxLength(150)]
        public string FullName { get; set; } = null!;

        [Required, MaxLength(150)]
        public string Email { get; set; } = null!;

        [MaxLength(20)]
        public string? Phone { get; set; }

        [Required, MaxLength(255)]
        public string PasswordHash { get; set; } = null!;

        [MaxLength(500)]
        public string? AvatarUrl { get; set; }

        [MaxLength(10)]
        public string? Gender { get; set; }

        public DateOnly? BirthDate { get; set; }

        [MaxLength(20)]
        public string Role { get; set; } = "Customer";

        // Cấp bậc quản trị (chỉ áp dụng cho Role=Admin): SuperAdmin/Manager/Staff
        [MaxLength(50)]
        public string? AdminTitle { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime? LastSpinAt { get; set; }   // lần quay vòng may mắn gần nhất

        // Navigation
        public ICollection<Address> Addresses { get; set; } = new List<Address>();
        public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
        public ICollection<Wishlist> Wishlists { get; set; } = new List<Wishlist>();
        public ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}
