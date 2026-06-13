using System.ComponentModel.DataAnnotations;

namespace ThoiTrang.Models.Entities
{
    public class ProductImage
    {
        [Key]
        public int ImageId { get; set; }
        public int ProductId { get; set; }

        [Required, MaxLength(500)]
        public string ImageUrl { get; set; } = null!;

        public bool IsMain { get; set; }
        public int DisplayOrder { get; set; }

        public Product? Product { get; set; }
    }
}
