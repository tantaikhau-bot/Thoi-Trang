using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ThoiTrang.Models.Entities
{
    public class ProductVariant
    {
        [Key]
        public int VariantId { get; set; }
        public int ProductId { get; set; }
        public int? ColorId { get; set; }
        public int? SizeId { get; set; }

        [MaxLength(60)]
        public string? Sku { get; set; }

        public int Stock { get; set; }

        [Column(TypeName = "decimal(18,0)")]
        public decimal PriceExtra { get; set; }

        public Product? Product { get; set; }
        public Color? Color { get; set; }
        public Size? Size { get; set; }
    }
}
