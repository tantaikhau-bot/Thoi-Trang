using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ThoiTrang.Models.Entities
{
    public class Product
    {
        public int ProductId { get; set; }
        public int CategoryId { get; set; }

        [Required, MaxLength(200)]
        public string Name { get; set; } = null!;

        [Required, MaxLength(220)]
        public string Slug { get; set; } = null!;

        [MaxLength(50)]
        public string? Sku { get; set; }

        [MaxLength(500)]
        public string? ShortDesc { get; set; }

        public string? Description { get; set; }

        [Column(TypeName = "decimal(18,0)")]
        public decimal Price { get; set; }

        [Column(TypeName = "decimal(18,0)")]
        public decimal? OldPrice { get; set; }

        [MaxLength(200)]
        public string? Material { get; set; }

        [MaxLength(100)]
        public string? Origin { get; set; }

        public bool IsNew { get; set; }
        public bool IsSale { get; set; }
        public bool IsFeatured { get; set; }
        public bool IsActive { get; set; } = true;

        [Column(TypeName = "decimal(3,2)")]
        public decimal RatingAvg { get; set; }
        public int RatingCount { get; set; }
        public int SoldCount { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Navigation
        public Category? Category { get; set; }
        public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
        public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
        public ICollection<ProductQuestion> Questions { get; set; } = new List<ProductQuestion>();
    }
}
