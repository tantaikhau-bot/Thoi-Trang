using System.ComponentModel.DataAnnotations;

namespace ThoiTrang.Models.Entities
{
    public class Address
    {
        public int AddressId { get; set; }
        public int UserId { get; set; }

        [Required, MaxLength(150)]
        public string ReceiverName { get; set; } = null!;

        [Required, MaxLength(20)]
        public string Phone { get; set; } = null!;

        [Required, MaxLength(100)]
        public string Province { get; set; } = null!;

        [Required, MaxLength(100)]
        public string District { get; set; } = null!;

        [MaxLength(100)]
        public string? Ward { get; set; }

        [Required, MaxLength(300)]
        public string AddressLine { get; set; } = null!;

        public bool IsDefault { get; set; }

        public User? User { get; set; }
    }
}
