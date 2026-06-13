using System.ComponentModel.DataAnnotations;

namespace ThoiTrang.Models.Entities
{
    public class ProductQuestion
    {
        [Key]
        public int QuestionId { get; set; }
        public int ProductId { get; set; }
        public int? UserId { get; set; }

        public string Question { get; set; } = null!;
        public string? Answer { get; set; }
        public DateTime? AnsweredAt { get; set; }
        public DateTime CreatedAt { get; set; }

        public Product? Product { get; set; }
        public User? User { get; set; }
    }
}
