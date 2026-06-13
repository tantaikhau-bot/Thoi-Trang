namespace ThoiTrang.Models.Entities
{
    public class SiteSetting
    {
        public int SiteSettingId { get; set; }
        public string SettingKey { get; set; } = null!;   // vd: pay_cod, pay_bank, pay_momo, pay_vnpay, pay_paypal
        public string Value { get; set; } = "true";        // "true" / "false"
    }
}
