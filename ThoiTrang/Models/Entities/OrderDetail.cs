using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ThoiTrang.Models.Entities
{
    public class OrderDetail
    {
        public int OrderDetailId { get; set; }
        public int OrderId { get; set; }
        public int? VariantId { get; set; }

        [Required, MaxLength(200)]
        public string ProductName { get; set; } = null!;

        [MaxLength(120)]
        public string? VariantInfo { get; set; }

        [Column(TypeName = "decimal(18,0)")]
        public decimal UnitPrice { get; set; }

        public int Quantity { get; set; }

        // Cột tính toán trong DB (computed, persisted) — chỉ đọc
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        [Column(TypeName = "decimal(18,0)")]
        public decimal LineTotal { get; private set; }

        public Order? Order { get; set; }
        public ProductVariant? Variant { get; set; }
    }
}
