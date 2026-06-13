using System.ComponentModel.DataAnnotations;

namespace ThoiTrang.Models.Entities
{
    public class Category
    {
        public int CategoryId { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = null!;

        [Required, MaxLength(120)]
        public string Slug { get; set; } = null!;

        public int? ParentId { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }

        // Navigation
        public Category? Parent { get; set; }
        public ICollection<Category> Children { get; set; } = new List<Category>();
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
