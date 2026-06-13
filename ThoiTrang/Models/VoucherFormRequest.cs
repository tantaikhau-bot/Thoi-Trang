namespace ThoiTrang.Models
{
    public class VoucherFormRequest
    {
        public string? Code { get; set; }
        public string? Description { get; set; }
        public string DiscountType { get; set; } = "amount";
        public decimal DiscountValue { get; set; }
        public decimal MinOrder { get; set; }
        public int Quantity { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class VoucherEditRequest : VoucherFormRequest
    {
        public int VoucherId { get; set; }
    }
}
