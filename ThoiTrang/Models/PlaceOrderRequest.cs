namespace ThoiTrang.Models
{
    public class PlaceOrderRequest
    {
        public string? ReceiverName { get; set; }
        public string? ReceiverPhone { get; set; }
        public string? ShippingAddress { get; set; }
        public string? Note { get; set; }
        public decimal Subtotal { get; set; }
        public decimal ProductDiscount { get; set; }
        public string? Voucher { get; set; }
        public decimal VoucherDiscount { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal Total { get; set; }
        public string ShippingMethod { get; set; } = "standard";
        public string PaymentMethod { get; set; } = "cod";
        public List<OrderItemDto> Items { get; set; } = new();
    }

    public class OrderItemDto
    {
        public int? VariantId { get; set; }
        public string? Name { get; set; }
        public string? Variant { get; set; }
        public decimal Unit { get; set; }
        public int Qty { get; set; }
    }
}
