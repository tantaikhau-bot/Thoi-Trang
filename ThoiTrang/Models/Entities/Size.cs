using System.ComponentModel.DataAnnotations;

namespace ThoiTrang.Models.Entities
{
    public class Size
    {
        public int SizeId { get; set; }

        [Required, MaxLength(20)]
        public string Name { get; set; } = null!;

        public int DisplayOrder { get; set; }

        public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
    }
}
