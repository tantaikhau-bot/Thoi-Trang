namespace ThoiTrang.Models.Entities
{
    public class ScheduledEvent
    {
        public int ScheduledEventId { get; set; }
        public string Title { get; set; } = null!;
        public string? Meta { get; set; }
        public DateTime EventDate { get; set; }
        public string Tags { get; set; } = "sale";       // csv: sale,email,voucher,banner
        public string Status { get; set; } = "scheduled"; // scheduled / active / draft
        public DateTime CreatedAt { get; set; }
    }
}
