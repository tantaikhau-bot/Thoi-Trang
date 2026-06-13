using Microsoft.EntityFrameworkCore;
using ThoiTrang.Models.Entities;

namespace ThoiTrang.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<ProductImage> ProductImages => Set<ProductImage>();
        public DbSet<Color> Colors => Set<Color>();
        public DbSet<Size> Sizes => Set<Size>();
        public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
        public DbSet<User> Users => Set<User>();
        public DbSet<Address> Addresses => Set<Address>();
        public DbSet<CartItem> CartItems => Set<CartItem>();
        public DbSet<Wishlist> Wishlists => Set<Wishlist>();
        public DbSet<Voucher> Vouchers => Set<Voucher>();
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderDetail> OrderDetails => Set<OrderDetail>();
        public DbSet<Review> Reviews => Set<Review>();
        public DbSet<ProductQuestion> ProductQuestions => Set<ProductQuestion>();
        public DbSet<Notification> Notifications => Set<Notification>();
        public DbSet<UserVoucher> UserVouchers => Set<UserVoucher>();
        public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
        public DbSet<PaymentMethod> PaymentMethods => Set<PaymentMethod>();
        public DbSet<SiteSetting> SiteSettings => Set<SiteSetting>();
        public DbSet<ChatKnowledge> ChatKnowledges => Set<ChatKnowledge>();
        public DbSet<ScheduledEvent> ScheduledEvents => Set<ScheduledEvent>();
        public DbSet<Combo> Combos => Set<Combo>();
        public DbSet<ComboItem> ComboItems => Set<ComboItem>();
        public DbSet<Collection> Collections => Set<Collection>();

        protected override void OnModelCreating(ModelBuilder mb)
        {
            base.OnModelCreating(mb);

            // ===== Unique indexes / ràng buộc khớp với schema SQL =====
            mb.Entity<Category>().HasIndex(c => c.Slug).IsUnique();
            mb.Entity<Product>().HasIndex(p => p.Slug).IsUnique();
            mb.Entity<User>().HasIndex(u => u.Email).IsUnique();
            mb.Entity<Voucher>().HasIndex(v => v.Code).IsUnique();
            mb.Entity<Order>().HasIndex(o => o.OrderCode).IsUnique();
            mb.Entity<CartItem>().HasIndex(c => new { c.UserId, c.VariantId }).IsUnique();
            mb.Entity<Wishlist>().HasIndex(w => new { w.UserId, w.ProductId }).IsUnique();
            mb.Entity<UserVoucher>().HasIndex(uv => new { uv.UserId, uv.VoucherId }).IsUnique();
            mb.Entity<UserVoucher>().HasOne(uv => uv.User).WithMany().HasForeignKey(uv => uv.UserId).OnDelete(DeleteBehavior.Cascade);
            mb.Entity<UserVoucher>().HasOne(uv => uv.Voucher).WithMany().HasForeignKey(uv => uv.VoucherId).OnDelete(DeleteBehavior.Restrict);
            mb.Entity<ChatMessage>().HasOne(m => m.User).WithMany().HasForeignKey(m => m.UserId).OnDelete(DeleteBehavior.Cascade);
            mb.Entity<ChatMessage>().HasIndex(m => m.UserId);
            mb.Entity<PaymentMethod>().HasOne(p => p.User).WithMany().HasForeignKey(p => p.UserId).OnDelete(DeleteBehavior.Cascade);
            mb.Entity<SiteSetting>().HasIndex(s => s.SettingKey).IsUnique();

            // ===== Category tự tham chiếu =====
            mb.Entity<Category>()
              .HasOne(c => c.Parent)
              .WithMany(c => c.Children)
              .HasForeignKey(c => c.ParentId)
              .OnDelete(DeleteBehavior.Restrict);

            // ===== Tránh multiple cascade paths (khớp FK trong SQL) =====
            mb.Entity<Order>()
              .HasOne(o => o.User).WithMany(u => u.Orders)
              .HasForeignKey(o => o.UserId).OnDelete(DeleteBehavior.Restrict);

            mb.Entity<Review>()
              .HasOne(r => r.User).WithMany()
              .HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Restrict);

            mb.Entity<ProductQuestion>()
              .HasOne(q => q.User).WithMany()
              .HasForeignKey(q => q.UserId).OnDelete(DeleteBehavior.Restrict);

            mb.Entity<OrderDetail>()
              .HasOne(d => d.Variant).WithMany()
              .HasForeignKey(d => d.VariantId).OnDelete(DeleteBehavior.Restrict);

            mb.Entity<CartItem>()
              .HasOne(c => c.Variant).WithMany()
              .HasForeignKey(c => c.VariantId).OnDelete(DeleteBehavior.Restrict);

            mb.Entity<Wishlist>()
              .HasOne(w => w.Product).WithMany()
              .HasForeignKey(w => w.ProductId).OnDelete(DeleteBehavior.Restrict);

            // ===== Combo =====
            mb.Entity<ComboItem>()
              .HasOne(ci => ci.Combo).WithMany(c => c.Items)
              .HasForeignKey(ci => ci.ComboId).OnDelete(DeleteBehavior.Cascade);
            mb.Entity<ComboItem>()
              .HasOne(ci => ci.Product).WithMany()
              .HasForeignKey(ci => ci.ProductId).OnDelete(DeleteBehavior.Restrict);

            // ===== Cột tính toán LineTotal =====
            mb.Entity<OrderDetail>()
              .Property(d => d.LineTotal)
              .HasComputedColumnSql("[UnitPrice] * [Quantity]", stored: true);

            // ===== Mặc định giá trị thời gian =====
            foreach (var t in new[] { typeof(Category), typeof(Product), typeof(User),
                                      typeof(CartItem), typeof(Wishlist), typeof(Order),
                                      typeof(Review), typeof(ProductQuestion), typeof(Notification) })
            {
                mb.Entity(t).Property("CreatedAt").HasDefaultValueSql("SYSDATETIME()");
            }
        }
    }
}
