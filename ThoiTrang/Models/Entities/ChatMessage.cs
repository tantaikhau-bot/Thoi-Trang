namespace ThoiTrang.Models.Entities
{
    public class ChatMessage
    {
        public int ChatMessageId { get; set; }
        public int UserId { get; set; }          // cuộc trò chuyện thuộc về user này
        public bool FromAdmin { get; set; }       // true = admin gửi, false = khách gửi
        public string Content { get; set; } = null!;
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }

        public User? User { get; set; }
    }
}
