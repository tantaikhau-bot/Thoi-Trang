namespace ThoiTrang.Models
{
    public class ProductFormRequest
    {
        public string? Name { get; set; }
        public string? Sku { get; set; }
        public decimal Price { get; set; }
        public decimal OldPrice { get; set; }
        public string? Material { get; set; }
        public string? Description { get; set; }
        public string Gender { get; set; } = "male";  // male / female / unisex
        public string Badge { get; set; } = "none";    // none / new / sale / bestseller
    }

    public class ProductEditRequest : ProductFormRequest
    {
        public int ProductId { get; set; }
    }
}
