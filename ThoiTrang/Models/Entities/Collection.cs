using System.ComponentModel.DataAnnotations;

namespace ThoiTrang.Models.Entities
{
    public class Collection
    {
        public int CollectionId { get; set; }

        // Nhãn nhỏ phía trên (vd "CAMPAIGN AUTUMN '26")
        [MaxLength(100)]
        public string? Label { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; } = null!;

        [MaxLength(800)]
        public string? Description { get; set; }

        [MaxLength(50)]
        public string? Icon { get; set; }       // ti-hanger, ti-feather...

        [MaxLength(50)]
        public string? CoverClass { get; set; } // bg-concept-1..4

        [MaxLength(200)]
        public string? LinkUrl { get; set; }    // /Home/CampaignAutumn...

        [MaxLength(80)]
        public string? LinkText { get; set; }   // "Khám phá chi tiết"

        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
    }
}
