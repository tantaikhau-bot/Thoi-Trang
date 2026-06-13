namespace ThoiTrang.Models.Entities
{
    public class UserVoucher
    {
        public int UserVoucherId { get; set; }
        public int UserId { get; set; }
        public int VoucherId { get; set; }
        public bool IsUsed { get; set; }
        public DateTime SavedAt { get; set; }
        public DateTime? UsedAt { get; set; }

        public User? User { get; set; }
        public Voucher? Voucher { get; set; }
    }
}
