using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ThoiTrang.Models.Entities
{
    public class Combo
    {
        public int ComboId { get; set; }

        [Required, MaxLength(200)]
        public string Name { get; set; } = null!;

        [MaxLength(500)]
        public string? Description { get; set; }

        // Giá ưu đãi của combo
        [Column(TypeName = "decimal(18,0)")]
        public decimal ComboPrice { get; set; }

        // Tổng giá gốc (để hiện % tiết kiệm). Nếu 0 sẽ tự tính từ thành phần.
        [Column(TypeName = "decimal(18,0)")]
        public decimal OldPrice { get; set; }

        [MaxLength(20)]
        public string? Badge { get; set; } // fire / new / null

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }

        public ICollection<ComboItem> Items { get; set; } = new List<ComboItem>();
    }

    public class ComboItem
    {
        public int ComboItemId { get; set; }
        public int ComboId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; } = 1;

        public Combo? Combo { get; set; }
        public Product? Product { get; set; }
    }
}
