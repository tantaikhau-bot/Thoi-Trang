using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ThoiTrang.Models.Entities
{
    public class Order
    {
        public int OrderId { get; set; }

        [Required, MaxLength(30)]
        public string OrderCode { get; set; } = null!;

        public int? UserId { get; set; }

        [Required, MaxLength(150)]
        public string ReceiverName { get; set; } = null!;

        [Required, MaxLength(20)]
        public string ReceiverPhone { get; set; } = null!;

        [Required, MaxLength(400)]
        public string ShippingAddress { get; set; } = null!;

        [MaxLength(500)]
        public string? Note { get; set; }

        [Column(TypeName = "decimal(18,0)")]
        public decimal Subtotal { get; set; }

        [Column(TypeName = "decimal(18,0)")]
        public decimal ProductDiscount { get; set; }

        public int? VoucherId { get; set; }

        [Column(TypeName = "decimal(18,0)")]
        public decimal VoucherDiscount { get; set; }

        [Column(TypeName = "decimal(18,0)")]
        public decimal ShippingFee { get; set; }

        [Column(TypeName = "decimal(18,0)")]
        public decimal TotalAmount { get; set; }

        [MaxLength(20)]
        public string ShippingMethod { get; set; } = "standard"; // standard/express/store

        [MaxLength(20)]
        public string PaymentMethod { get; set; } = "cod"; // bank/momo/vnpay/cod

        [MaxLength(20)]
        public string PaymentStatus { get; set; } = "Pending"; // Pending/Paid/Failed

        [MaxLength(20)]
        public string OrderStatus { get; set; } = "Pending"; // Pending/Confirmed/Shipping/Completed/Cancelled

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Đã trừ tồn kho cho đơn này chưa (tránh trừ/cộng 2 lần)
        public bool StockDeducted { get; set; }

        public User? User { get; set; }
        public Voucher? Voucher { get; set; }
        public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    }
}
