using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ThoiTrang.Models.Entities
{
    public class Voucher
    {
        public int VoucherId { get; set; }

        [Required, MaxLength(50)]
        public string Code { get; set; } = null!;

        [MaxLength(300)]
        public string? Description { get; set; }

        [MaxLength(10)]
        public string DiscountType { get; set; } = "amount"; // amount / percent

        [Column(TypeName = "decimal(18,0)")]
        public decimal DiscountValue { get; set; }

        [Column(TypeName = "decimal(18,0)")]
        public decimal MinOrder { get; set; }

        [Column(TypeName = "decimal(18,0)")]
        public decimal? MaxDiscount { get; set; }

        public int Quantity { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsActive { get; set; } = true;

        public ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}
