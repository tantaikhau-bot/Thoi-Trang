namespace ThoiTrang.Models.Entities
{
    public class ChatKnowledge
    {
        public int ChatKnowledgeId { get; set; }
        public string Topic { get; set; } = null!;        // chủ đề (ship, payment...)
        public string Keywords { get; set; } = null!;     // từ khóa, cách nhau dấu phẩy
        public string Answer { get; set; } = null!;       // câu trả lời
        public int Priority { get; set; }                 // ưu tiên khi điểm bằng nhau
        public bool IsActive { get; set; } = true;
    }
}
