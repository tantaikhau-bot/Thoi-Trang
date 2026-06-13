using System.ComponentModel.DataAnnotations;

namespace ThoiTrang.Models.Entities
{
    public class Color
    {
        public int ColorId { get; set; }

        [Required, MaxLength(50)]
        public string Name { get; set; } = null!;

        [MaxLength(7)]
        public string? HexCode { get; set; }

        public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
    }
}
