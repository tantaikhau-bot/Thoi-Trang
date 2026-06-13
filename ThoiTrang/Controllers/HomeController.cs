using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThoiTrang.Data;
using ThoiTrang.Models;
using ThoiTrang.Models.Entities;

namespace ThoiTrang.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IPasswordHasher<User> _hasher;
        private readonly ThoiTrang.Services.IEmailSender _email;

        public HomeController(AppDbContext db, IPasswordHasher<User> hasher, ThoiTrang.Services.IEmailSender email)
        {
            _db = db;
            _hasher = hasher;
            _email = email;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.FlashProducts = await ProductsQuery()
                .Where(p => p.IsSale)
                .OrderByDescending(p => p.SoldCount).Take(4).ToListAsync();
            ViewBag.BestSellers = await ProductsQuery()
                .OrderByDescending(p => p.SoldCount).Take(4).ToListAsync();

            // ===== Đếm sản phẩm theo "Mua theo nhu cầu" — khớp y hệt cách trang Search lọc =====
            const string ciai = "Latin1_General_CI_AI";
            async Task<int> CountKw(string kw)
            {
                var like = "%" + kw + "%";
                return await ProductsQuery().CountAsync(p =>
                    EF.Functions.Like(EF.Functions.Collate(p.Name, ciai), like)
                 || EF.Functions.Like(EF.Functions.Collate(p.Category!.Name, ciai), like)
                 || (p.Material != null && EF.Functions.Like(EF.Functions.Collate(p.Material, ciai), like)));
            }
            ViewBag.NeedCounts = new Dictionary<string, int>
            {
                ["ao"] = await CountKw("áo"),
                ["quan"] = await CountKw("quần"),
                ["khoac"] = await CountKw("khoác"),
                ["giay"] = await CountKw("giày"),
                ["tui"] = await CountKw("túi"),
                ["phukien"] = await CountKw("phụ kiện")
            };
            return View();
        }
        // Truy vấn cơ bản: kèm Category, Images, Variants + Color
        private IQueryable<Product> ProductsQuery() =>
            _db.Products
               .Include(p => p.Category)
               .Include(p => p.Images)
               .Include(p => p.Variants).ThenInclude(v => v.Color)
               .Where(p => p.IsActive);

        public async Task<IActionResult> Nam()
        {
            var list = await ProductsQuery()
                .Where(p => p.Category!.Slug == "nam")
                .OrderByDescending(p => p.CreatedAt).ToListAsync();
            return View(list);
        }
        public async Task<IActionResult> Nu()
        {
            var list = await ProductsQuery()
                .Where(p => p.Category!.Slug == "nu")
                .OrderByDescending(p => p.CreatedAt).ToListAsync();
            return View(list);
        }
        public async Task<IActionResult> MoiVe()
        {
            var list = await ProductsQuery()
                .Where(p => p.IsNew)
                .OrderByDescending(p => p.CreatedAt).ToListAsync();
            return View(list);
        }
        public async Task<IActionResult> BoSuuTap()
        {
            ViewBag.Collections = await _db.Collections
                .Where(c => c.IsActive)
                .OrderBy(c => c.DisplayOrder).ThenBy(c => c.CollectionId)
                .ToListAsync();
            return View();
        }
        public async Task<IActionResult> Sale()
        {
            var list = await ProductsQuery()
                .Where(p => p.IsSale)
                .OrderByDescending(p => p.CreatedAt).ToListAsync();

            ViewBag.ActiveVouchers = await _db.Vouchers
                .Where(v => v.IsActive && v.Quantity > 0)
                .OrderByDescending(v => v.DiscountValue).Take(8).ToListAsync();
            var uid = CurrentUserId();
            ViewBag.SavedVoucherIds = uid == null ? new List<int>() :
                await _db.UserVouchers.Where(uv => uv.UserId == uid).Select(uv => uv.VoucherId).ToListAsync();

            // Combo thật từ DB
            ViewBag.Combos = await _db.Combos
                .Where(c => c.IsActive)
                .Include(c => c.Items).ThenInclude(i => i.Product)
                .OrderBy(c => c.ComboId)
                .ToListAsync();

            // Mua theo tầm giá — sản phẩm thật phân theo 3 phân khúc
            ViewBag.PriceProducts = await ProductsQuery()
                .OrderBy(p => p.Price)
                .ToListAsync();

            // ===== Lịch sự kiện sale (liên kết countdown) =====
            var nowT = DateTime.Now;
            var saleEvents = await _db.ScheduledEvents
                .Where(e => e.Status != "draft" && (e.Tags == null || e.Tags.Contains("sale")))
                .OrderBy(e => e.EventDate)
                .ToListAsync();
            // Sự kiện sắp diễn ra gần nhất (countdown đếm tới đây)
            ViewBag.NextSaleEvent = saleEvents.FirstOrDefault(e => e.EventDate > nowT);
            // Sự kiện đang diễn ra (mới nhất đã tới giờ)
            ViewBag.CurrentSaleEvent = saleEvents.Where(e => e.EventDate <= nowT)
                .OrderByDescending(e => e.EventDate).FirstOrDefault();
            return View(list);
        }
        public IActionResult GioiThieu()
        {
            return View();
        }
        [Authorize]
        public async Task<IActionResult> TaiKhoan()
        {
            var uid = CurrentUserId();
            ViewBag.MyVouchers = await _db.UserVouchers
                .Where(uv => uv.UserId == uid)
                .Include(uv => uv.Voucher)
                .OrderBy(uv => uv.IsUsed).ThenByDescending(uv => uv.UserVoucherId)
                .ToListAsync();

            ViewBag.CurrentUser = await _db.Users.FindAsync(uid);
            ViewBag.MyOrders = await _db.Orders
                .Where(o => o.UserId == uid)
                .Include(o => o.OrderDetails)
                .OrderByDescending(o => o.OrderId).ToListAsync();
            ViewBag.MyAddresses = await _db.Addresses
                .Where(a => a.UserId == uid)
                .OrderByDescending(a => a.IsDefault).ToListAsync();
            ViewBag.WishlistCount = await _db.Wishlists.CountAsync(w => w.UserId == uid);
            ViewBag.CartCount = await _db.CartItems.Where(c => c.UserId == uid).SumAsync(c => (int?)c.Quantity) ?? 0;
            ViewBag.OrderCount = ((List<Order>)ViewBag.MyOrders).Count;
            ViewBag.MyWishlist = await _db.Products
                .Where(p => _db.Wishlists.Any(w => w.UserId == uid && w.ProductId == p.ProductId))
                .Include(p => p.Category)
                .OrderByDescending(p => p.ProductId).ToListAsync();
            ViewBag.MyPayments = await _db.PaymentMethods
                .Where(p => p.UserId == uid)
                .OrderByDescending(p => p.IsDefault).ToListAsync();

            // Thông báo (của user + chung)
            ViewBag.Notifications = await _db.Notifications
                .Where(n => n.UserId == uid || n.UserId == null)
                .OrderByDescending(n => n.NotificationId)
                .Take(30).ToListAsync();
            ViewBag.UnreadNotifCount = await _db.Notifications
                .CountAsync(n => n.UserId == uid && !n.IsRead);

            // Điểm tích lũy & hạng thành viên (1 điểm / 1.000đ chi tiêu, đơn không hủy)
            var spent = ((List<Order>)ViewBag.MyOrders)
                .Where(o => o.OrderStatus != "Cancelled").Sum(o => o.TotalAmount);
            int points = (int)(spent / 1000);
            var (tierKey, tierLabel, nextLabel, nextThreshold) =
                points >= 5000 ? ("platinum", "VIP PLATINUM", "", 5000) :
                points >= 3000 ? ("gold", "VIP GOLD", "Platinum", 5000) :
                points >= 1000 ? ("silver", "VIP SILVER", "Gold", 3000) :
                                 ("bronze", "THÀNH VIÊN BRONZE", "Silver", 1000);
            ViewBag.Points = points;
            ViewBag.TierKey = tierKey;
            ViewBag.TierLabel = tierLabel;
            ViewBag.NextTier = nextLabel;
            ViewBag.PointsToNext = nextLabel == "" ? 0 : Math.Max(0, nextThreshold - points);
            ViewBag.TierPercent = nextLabel == "" ? 100 : (int)Math.Min(100, points * 100.0 / nextThreshold);
            return View();
        }
        public IActionResult DangNhap()
        {
            return View();
        }
        [Authorize]
        public async Task<IActionResult> GioHang()
        {
            var uid = CurrentUserId();
            var items = await _db.CartItems
                .Where(c => c.UserId == uid)
                .Include(c => c.Variant!).ThenInclude(v => v.Product!).ThenInclude(p => p.Category)
                .Include(c => c.Variant!).ThenInclude(v => v.Color)
                .Include(c => c.Variant!).ThenInclude(v => v.Size)
                .OrderByDescending(c => c.CartItemId)
                .ToListAsync();

            // Voucher mà user ĐÃ DÙNG → loại khỏi danh sách
            var usedVoucherIds = await _db.UserVouchers
                .Where(uv => uv.UserId == uid && uv.IsUsed)
                .Select(uv => uv.VoucherId).ToListAsync();

            ViewBag.ActiveVouchers = await _db.Vouchers
                .Where(v => v.IsActive && v.Quantity > 0 && !usedVoucherIds.Contains(v.VoucherId))
                .OrderBy(v => v.MinOrder).Take(8).ToListAsync();
            ViewBag.SavedVoucherIds = await _db.UserVouchers
                .Where(uv => uv.UserId == uid && !uv.IsUsed).Select(uv => uv.VoucherId).ToListAsync();

            // Gợi ý mua kết hợp: sản phẩm chưa có trong giỏ
            var inCart = items.Select(c => c.Variant!.ProductId).Distinct().ToList();
            ViewBag.Suggest = await ProductsQuery()
                .Where(p => !inCart.Contains(p.ProductId))
                .OrderByDescending(p => p.SoldCount)
                .Take(8).ToListAsync();
            return View(items);
        }

        // Thêm vào giỏ (lưu DB theo tài khoản)
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AddToCart(int variantId, int quantity = 1)
        {
            var uid = CurrentUserId();
            if (quantity < 1) quantity = 1;
            var variant = await _db.ProductVariants.FindAsync(variantId);
            if (variant == null) return BadRequest(new { message = "Biến thể không tồn tại" });
            if (variant.Stock <= 0) return BadRequest(new { message = "Sản phẩm đã hết hàng" });

            var existing = await _db.CartItems.FirstOrDefaultAsync(c => c.UserId == uid && c.VariantId == variantId);
            int already = existing?.Quantity ?? 0;
            // Không cho vượt quá tồn kho
            if (already + quantity > variant.Stock)
            {
                int canAdd = variant.Stock - already;
                if (canAdd <= 0) return BadRequest(new { message = $"Trong giỏ đã có tối đa số lượng tồn kho ({variant.Stock})" });
                quantity = canAdd;
            }
            if (existing != null) existing.Quantity += quantity;
            else _db.CartItems.Add(new CartItem { UserId = uid!.Value, VariantId = variantId, Quantity = quantity });
            await _db.SaveChangesAsync();

            var count = await _db.CartItems.Where(c => c.UserId == uid).SumAsync(c => (int?)c.Quantity) ?? 0;
            return Json(new { success = true, count });
        }

        // Thêm combo thật vào giỏ — thêm từng sản phẩm thành phần (biến thể còn hàng)
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AddComboToCart(int comboId)
        {
            var uid = CurrentUserId();
            var combo = await _db.Combos
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.ComboId == comboId && c.IsActive);
            if (combo == null) return BadRequest(new { message = "Combo không tồn tại" });

            int added = 0;
            var outOfStock = new List<string>();
            foreach (var item in combo.Items)
            {
                // Chọn 1 biến thể còn hàng của sản phẩm
                var variant = await _db.ProductVariants
                    .Where(v => v.ProductId == item.ProductId && v.Stock > 0)
                    .OrderBy(v => v.VariantId)
                    .FirstOrDefaultAsync();
                if (variant == null)
                {
                    var p = await _db.Products.FindAsync(item.ProductId);
                    outOfStock.Add(p?.Name ?? "sản phẩm");
                    continue;
                }
                int qty = item.Quantity < 1 ? 1 : item.Quantity;
                var existing = await _db.CartItems.FirstOrDefaultAsync(c => c.UserId == uid && c.VariantId == variant.VariantId);
                int already = existing?.Quantity ?? 0;
                int canAdd = Math.Min(qty, Math.Max(0, variant.Stock - already));
                if (canAdd <= 0) { outOfStock.Add(variant.ProductId.ToString()); continue; }
                if (existing != null) existing.Quantity += canAdd;
                else _db.CartItems.Add(new CartItem { UserId = uid!.Value, VariantId = variant.VariantId, Quantity = canAdd });
                added++;
            }
            await _db.SaveChangesAsync();

            var cnt = await _db.CartItems.Where(c => c.UserId == uid).SumAsync(c => (int?)c.Quantity) ?? 0;
            if (added == 0) return BadRequest(new { message = "Các sản phẩm trong combo đã hết hàng" });
            return Json(new { success = true, count = cnt, added });
        }

        // Thêm nhanh vào giỏ từ thẻ sản phẩm (chọn biến thể còn hàng đầu tiên)
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AddProductToCart(int productId)
        {
            var uid = CurrentUserId();
            // Chỉ chọn biến thể còn hàng
            var variant = await _db.ProductVariants
                .Where(v => v.ProductId == productId && v.Stock > 0)
                .OrderBy(v => v.VariantId)
                .FirstOrDefaultAsync();
            if (variant == null)
            {
                bool hasAny = await _db.ProductVariants.AnyAsync(v => v.ProductId == productId);
                return BadRequest(new { message = hasAny ? "Sản phẩm đã hết hàng" : "Sản phẩm chưa có biến thể" });
            }

            var existing = await _db.CartItems.FirstOrDefaultAsync(c => c.UserId == uid && c.VariantId == variant.VariantId);
            int already = existing?.Quantity ?? 0;
            if (already + 1 > variant.Stock)
                return BadRequest(new { message = $"Đã đạt tối đa tồn kho ({variant.Stock})" });
            if (existing != null) existing.Quantity += 1;
            else _db.CartItems.Add(new CartItem { UserId = uid!.Value, VariantId = variant.VariantId, Quantity = 1 });
            await _db.SaveChangesAsync();

            var count = await _db.CartItems.Where(c => c.UserId == uid).SumAsync(c => (int?)c.Quantity) ?? 0;
            return Json(new { success = true, count });
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> UpdateCartQty(int cartItemId, int quantity)
        {
            var uid = CurrentUserId();
            var item = await _db.CartItems.Include(c => c.Variant)
                .FirstOrDefaultAsync(c => c.CartItemId == cartItemId && c.UserId == uid);
            if (item == null) return NotFound();
            int qty = quantity < 1 ? 1 : quantity;
            int stock = item.Variant?.Stock ?? qty;
            bool capped = false;
            if (qty > stock) { qty = Math.Max(1, stock); capped = true; }
            item.Quantity = qty;
            await _db.SaveChangesAsync();
            return Json(new { success = true, quantity = qty, capped, stock });
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> RemoveCartItem(int cartItemId)
        {
            var uid = CurrentUserId();
            var item = await _db.CartItems.FirstOrDefaultAsync(c => c.CartItemId == cartItemId && c.UserId == uid);
            if (item != null) { _db.CartItems.Remove(item); await _db.SaveChangesAsync(); }
            var count = await _db.CartItems.Where(c => c.UserId == uid).SumAsync(c => (int?)c.Quantity) ?? 0;
            return Json(new { success = true, count });
        }

        [Authorize]
        public async Task<IActionResult> YeuThich()
        {
            var uid = CurrentUserId();
            var items = await _db.Products
                .Where(p => _db.Wishlists.Any(w => w.UserId == uid && w.ProductId == p.ProductId))
                .Include(p => p.Category)
                .Include(p => p.Variants).ThenInclude(v => v.Color)
                .OrderByDescending(p => p.ProductId)
                .ToListAsync();

            // Gợi ý: sản phẩm bán chạy chưa nằm trong yêu thích
            var likedIds = items.Select(p => p.ProductId).ToList();
            ViewBag.Suggest = await ProductsQuery()
                .Where(p => !likedIds.Contains(p.ProductId))
                .OrderByDescending(p => p.SoldCount)
                .Take(4).ToListAsync();
            return View(items);
        }

        // Lấy id userId hiện tại
        private int? CurrentUserId()
        {
            var idClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(idClaim, out var uid) ? uid : (int?)null;
        }

        // Bật/tắt yêu thích (AJAX). Trả 401 nếu chưa đăng nhập.
        [HttpPost]
        public async Task<IActionResult> ToggleWishlist(int productId)
        {
            var uid = CurrentUserId();
            if (uid == null) return Unauthorized(new { message = "Cần đăng nhập" });

            var existing = await _db.Wishlists
                .FirstOrDefaultAsync(w => w.UserId == uid && w.ProductId == productId);

            bool added;
            if (existing != null)
            {
                _db.Wishlists.Remove(existing);
                added = false;
            }
            else
            {
                _db.Wishlists.Add(new Wishlist { UserId = uid.Value, ProductId = productId });
                added = true;
            }
            await _db.SaveChangesAsync();

            var count = await _db.Wishlists.CountAsync(w => w.UserId == uid);
            return Json(new { success = true, added, count });
        }

        // Xóa khỏi yêu thích
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> RemoveFromWishlist(int productId)
        {
            var uid = CurrentUserId();
            var existing = await _db.Wishlists
                .FirstOrDefaultAsync(w => w.UserId == uid && w.ProductId == productId);
            if (existing != null)
            {
                _db.Wishlists.Remove(existing);
                await _db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(YeuThich));
        }

        // Lưu voucher vào tài khoản (mỗi tài khoản 1 lần, phải đăng nhập)
        [HttpPost]
        public async Task<IActionResult> SaveUserVoucher(int voucherId)
        {
            var uid = CurrentUserId();
            if (uid == null) return Unauthorized(new { message = "Cần đăng nhập" });

            var voucher = await _db.Vouchers.FirstOrDefaultAsync(v => v.VoucherId == voucherId && v.IsActive);
            if (voucher == null) return BadRequest(new { message = "Voucher không khả dụng" });

            bool already = await _db.UserVouchers.AnyAsync(uv => uv.UserId == uid && uv.VoucherId == voucherId);
            if (already) return Json(new { success = false, alreadySaved = true, message = "Bạn đã lưu voucher này rồi" });

            _db.UserVouchers.Add(new UserVoucher { UserId = uid.Value, VoucherId = voucherId });
            if (voucher.Quantity > 0) voucher.Quantity -= 1;  // giảm số lượng khi lưu
            await _db.SaveChangesAsync();
            await AddNotificationAsync(uid, "Đã lưu voucher 🎫",
                $"Voucher {voucher.Code} đã được thêm vào tài khoản của bạn.", "promo");
            return Json(new { success = true, remaining = voucher.Quantity });
        }

        // Vòng quay may mắn — mỗi tài khoản 1 lần/ngày
        [HttpPost]
        public async Task<IActionResult> SpinWheel()
        {
            var uid = CurrentUserId();
            if (uid == null) return Unauthorized(new { message = "Cần đăng nhập" });
            var user = await _db.Users.FindAsync(uid);
            if (user == null) return Unauthorized();

            if (user.LastSpinAt.HasValue && user.LastSpinAt.Value.Date == DateTime.Now.Date)
                return Json(new { success = false, alreadySpun = true, message = "Hôm nay bạn đã quay rồi, quay lại vào ngày mai nhé!" });

            // Chọn ngẫu nhiên 1 voucher còn hiệu lực
            var vouchers = await _db.Vouchers.Where(v => v.IsActive && v.Quantity > 0).ToListAsync();
            if (vouchers.Count == 0) return Json(new { success = false, message = "Hết voucher để quay." });
            var won = vouchers[new Random().Next(vouchers.Count)];

            user.LastSpinAt = DateTime.Now;
            // Lưu vào tài khoản nếu chưa có
            bool already = await _db.UserVouchers.AnyAsync(x => x.UserId == uid && x.VoucherId == won.VoucherId);
            if (!already)
            {
                _db.UserVouchers.Add(new UserVoucher { UserId = uid.Value, VoucherId = won.VoucherId });
                if (won.Quantity > 0) won.Quantity -= 1;
            }
            await _db.SaveChangesAsync();
            await AddNotificationAsync(uid, "Vòng quay may mắn 🎡", $"Bạn đã trúng voucher {won.Code}!", "promo");

            var vval = won.DiscountType == "percent" ? $"{won.DiscountValue:#,##0}%" : $"{won.DiscountValue:#,##0}đ";
            return Json(new { success = true, code = won.Code, value = vval });
        }

        // Danh sách id sản phẩm đã yêu thích (để tô tim trên trang)
        [HttpGet]
        public async Task<IActionResult> WishlistIds()
        {
            var uid = CurrentUserId();
            if (uid == null) return Json(new int[0]);
            var ids = await _db.Wishlists.Where(w => w.UserId == uid).Select(w => w.ProductId).ToListAsync();
            return Json(ids);
        }

        [Authorize]
        public async Task<IActionResult> ThongBao()
        {
            var uid = CurrentUserId();
            // Thông báo của user + thông báo chung (UserId null)
            ViewBag.Notifications = await _db.Notifications
                .Where(n => n.UserId == uid || n.UserId == null)
                .OrderByDescending(n => n.NotificationId)
                .Take(50)
                .ToListAsync();

            // Đánh dấu tất cả là đã đọc (badge biến mất)
            var unread = _db.Notifications.Where(n => n.UserId == uid && !n.IsRead);
            foreach (var n in unread) n.IsRead = true;
            await _db.SaveChangesAsync();
            return View();
        }

        // Đánh dấu tất cả thông báo là đã đọc (gọi từ TaiKhoan)
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> MarkNotificationsRead()
        {
            var uid = CurrentUserId();
            var unread = _db.Notifications.Where(n => n.UserId == uid && !n.IsRead);
            foreach (var n in unread) n.IsRead = true;
            await _db.SaveChangesAsync();
            return Json(new { success = true });
        }

        // Xóa tất cả thông báo của user (chỉ thông báo riêng, không xóa broadcast chung)
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> DeleteAllNotifications()
        {
            var uid = CurrentUserId();
            var mine = _db.Notifications.Where(n => n.UserId == uid);
            _db.Notifications.RemoveRange(mine);
            await _db.SaveChangesAsync();
            return Json(new { success = true });
        }

        // Xóa toàn bộ danh sách yêu thích của user
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> ClearWishlist()
        {
            var uid = CurrentUserId();
            var all = _db.Wishlists.Where(w => w.UserId == uid);
            _db.Wishlists.RemoveRange(all);
            await _db.SaveChangesAsync();
            return Json(new { success = true });
        }

        // Đếm số cho badge header (thông báo chưa đọc, yêu thích, giỏ hàng)
        [HttpGet]
        public async Task<IActionResult> Counts()
        {
            var uid = CurrentUserId();
            if (uid == null) return Json(new { notif = 0, wishlist = 0, cart = 0 });
            var notif = await _db.Notifications.CountAsync(n => n.UserId == uid && !n.IsRead);
            var wishlist = await _db.Wishlists.CountAsync(w => w.UserId == uid);
            var cart = await _db.CartItems.Where(c => c.UserId == uid).SumAsync(c => (int?)c.Quantity) ?? 0;
            return Json(new { notif, wishlist, cart });
        }

        // Tạo 1 thông báo cho user (gọi nội bộ khi user thực hiện hành động)
        private async Task AddNotificationAsync(int? userId, string title, string content, string type)
        {
            // Tôn trọng cấu hình bật/tắt loại thông báo trong Admin
            var key = type == "order" ? "notify_order" : (type == "promo" ? "notify_promo" : "notify_news");
            if (!await GetSettingAsync(key, true)) return;

            _db.Notifications.Add(new Notification
            {
                UserId = userId,
                Title = title,
                Content = content,
                Type = type,
                IsRead = false
            });
            await _db.SaveChangesAsync();
        }
        // ===================== TÌM KIẾM =====================
        public async Task<IActionResult> Search(string? q)
        {
            q = (q ?? "").Trim();
            ViewBag.Query = q;
            List<Product> results;
            if (!string.IsNullOrWhiteSpace(q))
            {
                var like = "%" + q + "%";
                const string ciai = "Latin1_General_CI_AI"; // không phân biệt hoa thường & dấu
                results = await ProductsQuery()
                    .Where(p => EF.Functions.Like(EF.Functions.Collate(p.Name, ciai), like)
                             || EF.Functions.Like(EF.Functions.Collate(p.Category!.Name, ciai), like)
                             || (p.Material != null && EF.Functions.Like(EF.Functions.Collate(p.Material, ciai), like)))
                    .OrderByDescending(p => p.SoldCount)
                    .ToListAsync();
            }
            else
            {
                // Không có từ khóa → hiển thị tất cả sản phẩm (dùng cho "Xem tất cả")
                results = await ProductsQuery()
                    .OrderByDescending(p => p.SoldCount)
                    .ToListAsync();
            }
            ViewBag.Count = results.Count;
            return View(results);
        }

        // Gợi ý tìm kiếm (autocomplete) — trả JSON
        [HttpGet]
        public async Task<IActionResult> SearchSuggest(string? q)
        {
            q = (q ?? "").Trim();
            if (q.Length < 1) return Json(new { products = new object[0], categories = new object[0] });

            var like = "%" + q + "%";
            const string ciai = "Latin1_General_CI_AI";
            var products = await ProductsQuery()
                .Where(p => EF.Functions.Like(EF.Functions.Collate(p.Name, ciai), like)
                         || EF.Functions.Like(EF.Functions.Collate(p.Category!.Name, ciai), like))
                .OrderByDescending(p => p.SoldCount)
                .Take(6)
                .Select(p => new
                {
                    id = p.ProductId,
                    name = p.Name,
                    price = p.Price,
                    category = p.Category!.Name
                })
                .ToListAsync();

            var categories = await _db.Categories
                .Where(c => EF.Functions.Like(EF.Functions.Collate(c.Name, ciai), like))
                .Take(4)
                .Select(c => new { name = c.Name, slug = c.Slug })
                .ToListAsync();

            return Json(new { products, categories });
        }

        // Từ khóa phổ biến (theo sản phẩm bán chạy)
        [HttpGet]
        public async Task<IActionResult> PopularSearches()
        {
            var names = await _db.Products
                .Where(p => p.IsActive)
                .OrderByDescending(p => p.SoldCount)
                .Take(8)
                .Select(p => p.Name)
                .ToListAsync();
            return Json(names);
        }

        public async Task<IActionResult> CTSP(int? id)
        {
            // AsSplitQuery: tránh Cartesian explosion khi Include nhiều collection (Reviews + Variants + Images)
            var product = await ProductsQuery()
                .Include(p => p.Variants).ThenInclude(v => v.Size)
                .Include(p => p.Reviews).ThenInclude(r => r.User)
                .AsSplitQuery()
                .FirstOrDefaultAsync(p => id == null || p.ProductId == id);

            if (product != null)
            {
                ViewBag.Related = await ProductsQuery()
                    .Where(p => p.CategoryId == product.CategoryId && p.ProductId != product.ProductId)
                    .Take(4).ToListAsync();
                ViewBag.Questions = await _db.ProductQuestions
                    .Where(q => q.ProductId == product.ProductId)
                    .Include(q => q.User)
                    .OrderByDescending(q => q.QuestionId)
                    .Take(20).ToListAsync();
                // Đã mua sản phẩm này?
                var uid = CurrentUserId();
                ViewBag.HasBought = uid != null && await _db.OrderDetails
                    .AnyAsync(d => d.Variant!.ProductId == product.ProductId &&
                                   _db.Orders.Any(o => o.OrderId == d.OrderId && o.UserId == uid && o.OrderStatus == "Completed"));
                ViewBag.AlreadyReviewed = uid != null && await _db.Reviews
                    .AnyAsync(r => r.ProductId == product.ProductId && r.UserId == uid);
            }
            return View(product);
        }
        public IActionResult Privacy()
        {
            return View();
        }
        public IActionResult ThongTinCN()
        {
            return View();
        }
        public IActionResult CampaignAutumn()
        {
            return View();
        }
        public IActionResult LimitedEdition()
        {
            return View();
        }
        public IActionResult RawMaterials()
        {
            return View();
        }
        public IActionResult EditorialCapsule()
        {
            return View();
        }
        [Authorize]
        public async Task<IActionResult> Checkout()
        {
            var uid = CurrentUserId();
            ViewBag.MyAddresses = await _db.Addresses
                .Where(a => a.UserId == uid)
                .OrderByDescending(a => a.IsDefault).ToListAsync();

            // Phương thức thanh toán đang bật (từ cài đặt admin)
            var settings = await _db.SiteSettings.ToDictionaryAsync(s => s.SettingKey, s => s.Value == "true");
            bool On(string k) => !settings.ContainsKey(k) || settings[k];
            ViewBag.PayCod = On("pay_cod");
            ViewBag.PayBank = On("pay_bank");
            ViewBag.PayMomo = On("pay_momo");
            ViewBag.PayVnpay = On("pay_vnpay");
            ViewBag.PayPaypal = settings.ContainsKey("pay_paypal") && settings["pay_paypal"];
            ViewBag.ShipStandard = On("ship_standard");
            ViewBag.ShipExpress = On("ship_express");
            ViewBag.ShipSameday = settings.ContainsKey("ship_sameday") && settings["ship_sameday"];
            ViewBag.ShipStore = On("ship_store");
            return View();
        }
        public IActionResult BankTransfer()
        {
            return View();
        }
        public IActionResult OrderSuccess()
        {
            return View();
        }
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Admin()
        {
            ViewBag.TotalRevenue = await _db.Orders
                .Where(o => o.OrderStatus != "Cancelled")
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;
            ViewBag.TotalOrders = await _db.Orders.CountAsync();
            ViewBag.TotalCustomers = await _db.Users.CountAsync(u => u.Role == "Customer");
            ViewBag.TotalProducts = await _db.Products.CountAsync();
            ViewBag.TotalSold = await _db.OrderDetails.SumAsync(d => (int?)d.Quantity) ?? 0;

            // 8 đơn hàng gần nhất
            ViewBag.RecentOrders = await _db.Orders
                .OrderByDescending(o => o.OrderId)
                .Take(8).ToListAsync();

            // Bảng quản lý
            ViewBag.AllOrders = await _db.Orders
                .Include(o => o.OrderDetails)
                .Include(o => o.User)
                .OrderByDescending(o => o.OrderId).Take(50).ToListAsync();
            ViewBag.AllProducts = await _db.Products
                .Include(p => p.Category)
                .Include(p => p.Variants)
                .OrderByDescending(p => p.ProductId).Take(50).ToListAsync();
            ViewBag.AllCustomers = await _db.Users
                .Where(u => u.Role == "Customer")
                .OrderByDescending(u => u.UserId).Take(50).ToListAsync();
            ViewBag.AllVouchers = await _db.Vouchers
                .OrderByDescending(v => v.VoucherId).Take(50).ToListAsync();

            // ===== Số liệu thật cho các tab/header Admin =====
            // Sản phẩm
            var prodStocks = await _db.Products
                .Select(p => new { p.IsActive, Stock = p.Variants.Sum(v => (int?)v.Stock) ?? 0 })
                .ToListAsync();
            ViewBag.ProdTotal = prodStocks.Count;
            ViewBag.ProdActive = prodStocks.Count(p => p.IsActive && p.Stock > 10);
            ViewBag.ProdLow = prodStocks.Count(p => p.IsActive && p.Stock > 0 && p.Stock <= 10);
            ViewBag.ProdOut = prodStocks.Count(p => p.Stock == 0);
            ViewBag.ProdDraft = prodStocks.Count(p => !p.IsActive);
            ViewBag.CategoryCount = await _db.Categories.CountAsync();

            // Đơn hàng
            var statusCounts = await _db.Orders
                .GroupBy(o => o.OrderStatus)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();
            int SC(string s) => statusCounts.FirstOrDefault(x => x.Status == s)?.Count ?? 0;
            ViewBag.OrdTotal = statusCounts.Sum(x => x.Count);
            ViewBag.OrdPending = SC("Pending");
            ViewBag.OrdConfirmed = SC("Confirmed");
            ViewBag.OrdShipping = SC("Shipping");
            ViewBag.OrdCompleted = SC("Completed");
            ViewBag.OrdCancelled = SC("Cancelled");

            // Khách hàng (theo tổng chi tiêu → hạng)
            var custSpend = await _db.Users
                .Where(u => u.Role == "Customer")
                .Select(u => new { Spent = _db.Orders.Where(o => o.UserId == u.UserId && o.OrderStatus != "Cancelled").Sum(o => (decimal?)o.TotalAmount) ?? 0 })
                .ToListAsync();
            ViewBag.CustTotal = custSpend.Count;
            ViewBag.CustPlatinum = custSpend.Count(c => c.Spent >= 5000000);
            ViewBag.CustGold = custSpend.Count(c => c.Spent >= 3000000 && c.Spent < 5000000);
            ViewBag.CustSilver = custSpend.Count(c => c.Spent >= 1000000 && c.Spent < 3000000);
            ViewBag.CustBronze = custSpend.Count(c => c.Spent < 1000000);

            // Cấu hình bật/tắt (phương thức thanh toán)
            var setDict = await _db.SiteSettings.ToDictionaryAsync(s => s.SettingKey, s => s.Value == "true");
            bool SOn(string k, bool def = true) => setDict.ContainsKey(k) ? setDict[k] : def;
            ViewBag.SetPayCod = SOn("pay_cod");
            ViewBag.SetPayBank = SOn("pay_bank");
            ViewBag.SetPayMomo = SOn("pay_momo");
            ViewBag.SetPayVnpay = SOn("pay_vnpay");
            ViewBag.SetPayPaypal = SOn("pay_paypal", false);
            ViewBag.SetShipStandard = SOn("ship_standard");
            ViewBag.SetShipExpress = SOn("ship_express");
            ViewBag.SetShipSameday = SOn("ship_sameday", false);
            ViewBag.SetShipStore = SOn("ship_store");
            ViewBag.SetNotifyOrder = SOn("notify_order");
            ViewBag.SetNotifyPromo = SOn("notify_promo");
            ViewBag.SetNotifyNews = SOn("notify_news");
            ViewBag.SetChatAutoreply = SOn("chat_autoreply");

            // Voucher
            ViewBag.VoucherActive = await _db.Vouchers.CountAsync(v => v.IsActive);
            ViewBag.VoucherExpiring = await _db.Vouchers.CountAsync(v => v.IsActive && v.EndDate != null && v.EndDate < DateTime.Now.AddDays(7));

            // Đánh giá & Hỏi đáp
            ViewBag.UnansweredQuestions = await _db.ProductQuestions.CountAsync(q => q.Answer == null);
            ViewBag.TotalReviews = await _db.Reviews.CountAsync();

            // Bộ sưu tập (CMS)
            ViewBag.AllCollections = await _db.Collections
                .OrderBy(c => c.DisplayOrder).ThenBy(c => c.CollectionId)
                .ToListAsync();

            // Danh sách quản trị viên (phân quyền)
            ViewBag.Admins = await _db.Users
                .Where(u => u.Role == "Admin")
                .OrderBy(u => u.UserId)
                .ToListAsync();
            ViewBag.CurrentAdminEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

            // ===== Số liệu biểu đồ thật =====
            var validOrders = await _db.Orders
                .Where(o => o.OrderStatus != "Cancelled")
                .Select(o => new { o.TotalAmount, o.CreatedAt })
                .ToListAsync();

            // Doanh thu 7 ngày gần nhất (thứ 2 → CN của tuần hiện tại tính ngược 7 ngày)
            var today = DateTime.Today;
            var rev7 = new List<object>();
            string[] dayLabels = { "T2", "T3", "T4", "T5", "T6", "T7", "CN" };
            // 7 ngày gần nhất kết thúc hôm nay
            for (int i = 6; i >= 0; i--)
            {
                var day = today.AddDays(-i);
                var sum = validOrders.Where(o => o.CreatedAt.Date == day).Sum(o => o.TotalAmount);
                rev7.Add(new { label = day.ToString("dd/MM"), value = sum });
            }
            ViewBag.Revenue7Days = rev7;
            ViewBag.Revenue7Max = rev7.Count > 0 ? rev7.Max(x => (decimal)((dynamic)x).value) : 0;

            // Doanh thu 12 tháng gần nhất
            var rev12 = new List<object>();
            for (int i = 11; i >= 0; i--)
            {
                var m = new DateTime(today.Year, today.Month, 1).AddMonths(-i);
                var sum = validOrders.Where(o => o.CreatedAt.Year == m.Year && o.CreatedAt.Month == m.Month).Sum(o => o.TotalAmount);
                rev12.Add(new { label = $"T{m.Month}/{m.Year % 100}", value = sum });
            }
            ViewBag.Revenue12Months = rev12;
            ViewBag.Revenue12Max = rev12.Count > 0 ? rev12.Max(x => (decimal)((dynamic)x).value) : 0;

            // Thống kê tháng hiện tại
            var thisMonth = validOrders.Where(o => o.CreatedAt.Year == today.Year && o.CreatedAt.Month == today.Month).ToList();
            ViewBag.MonthRevenue = thisMonth.Sum(o => o.TotalAmount);
            ViewBag.MonthOrders = thisMonth.Count;
            ViewBag.AvgOrderValue = thisMonth.Count > 0 ? thisMonth.Sum(o => o.TotalAmount) / thisMonth.Count : 0;
            // So sánh tháng trước
            var prevMonthDate = new DateTime(today.Year, today.Month, 1).AddMonths(-1);
            var prevMonth = validOrders.Where(o => o.CreatedAt.Year == prevMonthDate.Year && o.CreatedAt.Month == prevMonthDate.Month).ToList();
            decimal prevRev = prevMonth.Sum(o => o.TotalAmount);
            ViewBag.MonthRevenueGrowth = prevRev > 0 ? Math.Round((((decimal)ViewBag.MonthRevenue - prevRev) / prevRev) * 100, 1) : 0;

            // Donut theo danh mục (đếm sản phẩm mỗi danh mục)
            var catCounts = await _db.Categories
                .Select(c => new { c.Name, Count = _db.Products.Count(p => p.CategoryId == c.CategoryId) })
                .Where(x => x.Count > 0)
                .OrderByDescending(x => x.Count)
                .ToListAsync();
            ViewBag.CategoryCounts = catCounts.Cast<object>().ToList();

            // Top sản phẩm bán chạy (theo SoldCount thật)
            ViewBag.TopProducts = await _db.Products
                .Where(p => p.IsActive)
                .OrderByDescending(p => p.SoldCount)
                .Take(5)
                .Select(p => new { p.ProductId, p.Name, p.Price, p.SoldCount })
                .ToListAsync();

            return View();
        }

        // ===================== ĐĂNG NHẬP =====================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password)
        {
            email = (email ?? "").Trim();

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                TempData["AuthError"] = "Vui lòng nhập email và mật khẩu.";
                return RedirectToAction(nameof(DangNhap));
            }

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email && u.IsActive);
            if (user == null)
            {
                TempData["AuthError"] = "Email không tồn tại hoặc đã bị khóa.";
                return RedirectToAction(nameof(DangNhap));
            }

            var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, password);
            if (result == PasswordVerificationResult.Failed)
            {
                TempData["AuthError"] = "Mật khẩu không đúng.";
                return RedirectToAction(nameof(DangNhap));
            }

            await SignInUserAsync(user);

            // Phân luồng theo vai trò
            return user.Role == "Admin"
                ? RedirectToAction(nameof(Admin))
                : RedirectToAction(nameof(TaiKhoan));
        }

        // ===================== ĐĂNG KÝ (chỉ tạo tài khoản khách) =====================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string fullName, string email, string phone, string password)
        {
            fullName = (fullName ?? "").Trim();
            email = (email ?? "").Trim();
            phone = (phone ?? "").Trim();

            if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                TempData["AuthError"] = "Vui lòng điền đầy đủ họ tên, email và mật khẩu.";
                return RedirectToAction(nameof(DangNhap));
            }

            if (password.Length < 8)
            {
                TempData["AuthError"] = "Mật khẩu phải có ít nhất 8 ký tự.";
                return RedirectToAction(nameof(DangNhap));
            }

            bool emailExists = await _db.Users.AnyAsync(u => u.Email == email);
            if (emailExists)
            {
                TempData["AuthError"] = "Email này đã được đăng ký.";
                return RedirectToAction(nameof(DangNhap));
            }

            var user = new User
            {
                FullName = fullName,
                Email = email,
                Phone = string.IsNullOrWhiteSpace(phone) ? null : phone,
                Role = "Customer",
                IsActive = true
            };
            user.PasswordHash = _hasher.HashPassword(user, password);

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            // Thông báo chào mừng
            await AddNotificationAsync(user.UserId, "Chào mừng đến MONO.WEAR 🎉",
                "Tài khoản của bạn đã được tạo thành công. Khám phá bộ sưu tập mới ngay!", "info");

            // Email chào mừng
            await _email.SendAsync(user.Email, "Chào mừng bạn đến với MONO.WEAR 🎉",
                ThoiTrang.Services.EmailTemplate.Wrap($"Xin chào {user.FullName}!",
                    "Cảm ơn bạn đã tạo tài khoản tại MONO.WEAR.\n\nGiờ đây bạn có thể mua sắm, lưu sản phẩm yêu thích, theo dõi đơn hàng và nhận nhiều ưu đãi đặc biệt.\n\nChúc bạn mua sắm vui vẻ!"));

            // Đăng nhập luôn sau khi đăng ký → vào trang Tài khoản
            await SignInUserAsync(user);
            TempData["AuthSuccess"] = "Tạo tài khoản thành công!";
            return RedirectToAction(nameof(TaiKhoan));
        }

        // ===================== ĐẶT HÀNG (lưu vào DB) =====================
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> PlaceOrder([FromBody] PlaceOrderRequest req)
        {
            if (req == null || req.Items == null || req.Items.Count == 0)
                return BadRequest(new { message = "Giỏ hàng trống." });

            int? userId = null;
            if (User.Identity?.IsAuthenticated == true)
            {
                var idClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(idClaim, out var uid)) userId = uid;
            }

            // Áp dụng voucher (nếu có & hợp lệ) — mỗi voucher chỉ dùng 1 lần / tài khoản
            int? voucherId = null;
            UserVoucher? usedUv = null;
            if (!string.IsNullOrWhiteSpace(req.Voucher))
            {
                var v = await _db.Vouchers.FirstOrDefaultAsync(x => x.Code == req.Voucher && x.IsActive);
                if (v != null)
                {
                    voucherId = v.VoucherId;
                    if (userId != null)
                    {
                        usedUv = await _db.UserVouchers
                            .FirstOrDefaultAsync(uv => uv.UserId == userId && uv.VoucherId == v.VoucherId);
                        if (usedUv != null && usedUv.IsUsed)
                            return BadRequest(new { message = "Voucher này bạn đã sử dụng rồi." });
                    }
                }
            }

            var order = new Order
            {
                OrderCode = "MNW" + DateTime.Now.ToString("yyMMddHHmmss"),
                UserId = userId,
                ReceiverName = string.IsNullOrWhiteSpace(req.ReceiverName) ? "Khách" : req.ReceiverName!.Trim(),
                ReceiverPhone = req.ReceiverPhone?.Trim() ?? "",
                ShippingAddress = req.ShippingAddress?.Trim() ?? "",
                Note = req.Note?.Trim(),
                Subtotal = req.Subtotal,
                ProductDiscount = req.ProductDiscount,
                VoucherId = voucherId,
                VoucherDiscount = req.VoucherDiscount,
                ShippingFee = req.ShippingFee,
                TotalAmount = req.Total,
                ShippingMethod = req.ShippingMethod ?? "standard",
                PaymentMethod = req.PaymentMethod ?? "cod",
                PaymentStatus = req.PaymentMethod == "cod" ? "Pending" : "Pending",
                OrderStatus = "Pending"
            };

            foreach (var it in req.Items)
            {
                // Xác định VariantId: ưu tiên client gửi, nếu thiếu thì suy ra từ tên + mô tả biến thể
                int? variantId = it.VariantId;
                if (variantId == null || variantId <= 0)
                {
                    // Thử tìm biến thể theo tên sản phẩm + chuỗi "Màu X · Size Y"
                    var prod = await _db.Products.FirstOrDefaultAsync(p => p.Name == it.Name);
                    if (prod != null)
                    {
                        var cand = await _db.ProductVariants
                            .Include(v => v.Color).Include(v => v.Size)
                            .Where(v => v.ProductId == prod.ProductId)
                            .ToListAsync();
                        var match = cand.FirstOrDefault(v =>
                            !string.IsNullOrEmpty(it.Variant) &&
                            (v.Color != null && it.Variant.Contains(v.Color.Name)) &&
                            (v.Size != null && it.Variant.Contains(v.Size.Name)));
                        variantId = match?.VariantId ?? cand.FirstOrDefault()?.VariantId;
                    }
                }
                int qty = it.Qty <= 0 ? 1 : it.Qty;
                // Chặn đặt vượt tồn kho
                if (variantId != null)
                {
                    var vstock = await _db.ProductVariants.FindAsync(variantId);
                    if (vstock != null)
                    {
                        if (vstock.Stock <= 0)
                            return BadRequest(new { message = $"Sản phẩm \"{it.Name}\" đã hết hàng." });
                        if (qty > vstock.Stock)
                            return BadRequest(new { message = $"Sản phẩm \"{it.Name}\" chỉ còn {vstock.Stock} sản phẩm trong kho." });
                    }
                }
                order.OrderDetails.Add(new OrderDetail
                {
                    VariantId = variantId,
                    ProductName = it.Name ?? "Sản phẩm",
                    VariantInfo = it.Variant,
                    UnitPrice = it.Unit,
                    Quantity = qty
                });
            }

            _db.Orders.Add(order);

            // Đánh dấu voucher đã dùng (số lượng đã trừ khi lưu)
            if (usedUv != null) { usedUv.IsUsed = true; usedUv.UsedAt = DateTime.Now; }
            await _db.SaveChangesAsync();

            // Xóa giỏ hàng của người dùng sau khi đặt thành công + thông báo
            if (userId != null)
            {
                var cartItems = _db.CartItems.Where(c => c.UserId == userId);
                _db.CartItems.RemoveRange(cartItems);
                await _db.SaveChangesAsync();

                await AddNotificationAsync(userId, "Đặt hàng thành công ✅",
                    $"Đơn hàng {order.OrderCode} ({order.TotalAmount:#,##0}₫) đã được tiếp nhận và đang chờ xử lý.", "order");

                // Email xác nhận đơn hàng
                var buyer = await _db.Users.FindAsync(userId);
                if (buyer != null)
                {
                    var itemLines = string.Join("\n", order.OrderDetails.Select(d =>
                        $"• {d.ProductName} ({d.VariantInfo}) × {d.Quantity} = {d.UnitPrice * d.Quantity:#,##0}₫"));
                    var emailBody = $"Cảm ơn bạn đã đặt hàng tại MONO.WEAR!\n\n" +
                        $"Mã đơn: {order.OrderCode}\n" +
                        $"Người nhận: {order.ReceiverName} - {order.ReceiverPhone}\n" +
                        $"Địa chỉ: {order.ShippingAddress}\n\n" +
                        $"Sản phẩm:\n{itemLines}\n\n" +
                        $"Tạm tính: {order.Subtotal:#,##0}₫\n" +
                        (order.VoucherDiscount > 0 ? $"Giảm voucher: -{order.VoucherDiscount:#,##0}₫\n" : "") +
                        $"Phí vận chuyển: {(order.ShippingFee > 0 ? $"{order.ShippingFee:#,##0}₫" : "Miễn phí")}\n" +
                        $"TỔNG CỘNG: {order.TotalAmount:#,##0}₫\n\n" +
                        $"Thanh toán: {order.PaymentMethod.ToUpper()}\n\n" +
                        "Chúng tôi sẽ liên hệ và giao hàng trong thời gian sớm nhất.";
                    await _email.SendAsync(buyer.Email, $"Xác nhận đơn hàng {order.OrderCode} - MONO.WEAR",
                        ThoiTrang.Services.EmailTemplate.Wrap("Đặt hàng thành công! ✅", emailBody));
                }
            }

            return Json(new { success = true, orderCode = order.OrderCode });
        }

        // ===================== ADMIN CRUD =====================
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddProduct([FromBody] ProductFormRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.Name) || req.Price <= 0)
                return BadRequest(new { message = "Tên và giá bán là bắt buộc." });

            // Xác định danh mục theo giới tính
            var slug = req.Gender == "female" ? "nu" : "nam";
            var cat = await _db.Categories.FirstOrDefaultAsync(c => c.Slug == slug)
                      ?? await _db.Categories.FirstOrDefaultAsync();
            if (cat == null) return BadRequest(new { message = "Chưa có danh mục." });

            // Tạo slug duy nhất từ tên
            var baseSlug = ToSlug(req.Name);
            var uniqueSlug = baseSlug;
            int n = 1;
            while (await _db.Products.AnyAsync(p => p.Slug == uniqueSlug))
                uniqueSlug = $"{baseSlug}-{n++}";

            var product = new Product
            {
                Name = req.Name.Trim(),
                Slug = uniqueSlug,
                Sku = string.IsNullOrWhiteSpace(req.Sku) ? null : req.Sku.Trim(),
                Price = req.Price,
                OldPrice = req.OldPrice > 0 ? req.OldPrice : (decimal?)null,
                Material = string.IsNullOrWhiteSpace(req.Material) ? null : req.Material.Trim(),
                Description = req.Description,
                CategoryId = cat.CategoryId,
                IsNew = req.Badge == "new",
                IsSale = req.Badge == "sale",
                IsFeatured = req.Badge == "bestseller",
                IsActive = true,
                RatingAvg = 5,
                RatingCount = 0,
                SoldCount = 0
            };
            _db.Products.Add(product);
            await _db.SaveChangesAsync();

            // Ảnh mặc định
            _db.ProductImages.Add(new ProductImage
            {
                ProductId = product.ProductId,
                ImageUrl = "/images/products/" + uniqueSlug + ".jpg",
                IsMain = true
            });
            await _db.SaveChangesAsync();

            return Json(new { success = true, productId = product.ProductId });
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditProduct([FromBody] ProductEditRequest req)
        {
            if (req == null || req.ProductId <= 0) return BadRequest();
            var p = await _db.Products.FindAsync(req.ProductId);
            if (p == null) return NotFound();

            if (!string.IsNullOrWhiteSpace(req.Name)) p.Name = req.Name.Trim();
            if (req.Price > 0) p.Price = req.Price;
            p.OldPrice = req.OldPrice > 0 ? req.OldPrice : (decimal?)null;
            if (!string.IsNullOrWhiteSpace(req.Material)) p.Material = req.Material.Trim();
            if (req.Description != null) p.Description = req.Description;
            p.IsNew = req.Badge == "new";
            p.IsSale = req.Badge == "sale";
            p.IsFeatured = req.Badge == "bestseller";
            if (!string.IsNullOrWhiteSpace(req.Gender))
            {
                var slug = req.Gender == "female" ? "nu" : "nam";
                var cat = await _db.Categories.FirstOrDefaultAsync(c => c.Slug == slug);
                if (cat != null) p.CategoryId = cat.CategoryId;
            }
            p.UpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync();
            return Json(new { success = true });
        }

        // Lấy danh sách biến thể (màu/size/tồn kho) của 1 sản phẩm — cho modal sửa
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetProductVariants(int productId)
        {
            var variants = await _db.ProductVariants
                .Where(v => v.ProductId == productId)
                .Include(v => v.Color)
                .Include(v => v.Size)
                .OrderBy(v => v.ColorId).ThenBy(v => v.SizeId)
                .Select(v => new
                {
                    variantId = v.VariantId,
                    colorName = v.Color != null ? v.Color.Name : "—",
                    sizeName = v.Size != null ? v.Size.Name : "—",
                    stock = v.Stock,
                    priceExtra = v.PriceExtra
                })
                .ToListAsync();
            return Json(variants);
        }

        // Cập nhật tồn kho từng biến thể (từ modal sửa sản phẩm)
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateVariantStock([FromBody] List<VariantStockItem> items)
        {
            if (items == null || items.Count == 0) return BadRequest(new { message = "Không có dữ liệu" });
            foreach (var it in items)
            {
                var v = await _db.ProductVariants.FindAsync(it.VariantId);
                if (v != null) v.Stock = it.Stock < 0 ? 0 : it.Stock;
            }
            await _db.SaveChangesAsync();
            return Json(new { success = true });
        }

        public class VariantStockItem
        {
            public int VariantId { get; set; }
            public int Stock { get; set; }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteProduct(int productId)
        {
            var p = await _db.Products.FindAsync(productId);
            if (p == null) return NotFound();

            // Nếu sản phẩm chưa từng được đặt → xóa hẳn, ngược lại ẩn đi (giữ lịch sử đơn)
            bool hasOrders = await _db.OrderDetails
                .AnyAsync(d => d.Variant != null && d.Variant.ProductId == productId);
            if (hasOrders)
            {
                p.IsActive = false;
            }
            else
            {
                _db.Products.Remove(p); // cascade: images + variants
            }
            await _db.SaveChangesAsync();
            return Json(new { success = true, softDeleted = hasOrders });
        }

        // Lưu cấu hình bật/tắt (vd phương thức thanh toán)
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SaveSetting(string key, bool enabled)
        {
            if (string.IsNullOrWhiteSpace(key)) return BadRequest();
            var s = await _db.SiteSettings.FirstOrDefaultAsync(x => x.SettingKey == key);
            if (s == null) _db.SiteSettings.Add(new SiteSetting { SettingKey = key, Value = enabled ? "true" : "false" });
            else s.Value = enabled ? "true" : "false";
            await _db.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SaveVoucher([FromBody] VoucherFormRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.Code) || req.DiscountValue <= 0)
                return BadRequest(new { message = "Mã và giá trị giảm là bắt buộc." });

            var code = req.Code.Trim().ToUpper();
            if (await _db.Vouchers.AnyAsync(v => v.Code == code))
                return BadRequest(new { message = "Mã voucher đã tồn tại." });

            _db.Vouchers.Add(new Voucher
            {
                Code = code,
                Description = string.IsNullOrWhiteSpace(req.Description) ? null : req.Description.Trim(),
                DiscountType = req.DiscountType == "percent" ? "percent" : "amount",
                DiscountValue = req.DiscountValue,
                MinOrder = req.MinOrder,
                Quantity = req.Quantity,
                StartDate = req.StartDate,
                EndDate = req.EndDate,
                IsActive = true
            });
            await _db.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditVoucher([FromBody] VoucherEditRequest req)
        {
            if (req == null || req.VoucherId <= 0) return BadRequest();
            var v = await _db.Vouchers.FindAsync(req.VoucherId);
            if (v == null) return NotFound();
            if (!string.IsNullOrWhiteSpace(req.Code)) v.Code = req.Code.Trim().ToUpper();
            v.Description = string.IsNullOrWhiteSpace(req.Description) ? null : req.Description.Trim();
            v.DiscountType = req.DiscountType == "percent" ? "percent" : "amount";
            if (req.DiscountValue > 0) v.DiscountValue = req.DiscountValue;
            v.MinOrder = req.MinOrder;
            v.Quantity = req.Quantity;
            v.StartDate = req.StartDate;
            v.EndDate = req.EndDate;
            await _db.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetVoucher(int id)
        {
            var v = await _db.Vouchers.FindAsync(id);
            if (v == null) return NotFound();
            return Json(new
            {
                v.VoucherId, v.Code, v.Description, v.DiscountType,
                v.DiscountValue, v.MinOrder, v.Quantity,
                startDate = v.StartDate?.ToString("yyyy-MM-dd"),
                endDate = v.EndDate?.ToString("yyyy-MM-dd")
            });
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteVoucher(int voucherId)
        {
            var v = await _db.Vouchers.FindAsync(voucherId);
            if (v != null) { _db.Vouchers.Remove(v); await _db.SaveChangesAsync(); }
            return Json(new { success = true });
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateOrderStatus(int orderId, string status)
        {
            var o = await _db.Orders
                .Include(x => x.OrderDetails)
                .FirstOrDefaultAsync(x => x.OrderId == orderId);
            if (o == null) return NotFound();
            var allowed = new[] { "Pending", "Confirmed", "Shipping", "Completed", "Cancelled" };
            if (!allowed.Contains(status)) return BadRequest(new { message = "Trạng thái không hợp lệ." });

            // ===== Quản lý tồn kho theo trạng thái =====
            // Completed → trừ tồn kho mỗi biến thể + tăng SoldCount (1 lần duy nhất)
            if (status == "Completed" && !o.StockDeducted)
            {
                foreach (var d in o.OrderDetails.Where(d => d.VariantId != null))
                {
                    var variant = await _db.ProductVariants.FindAsync(d.VariantId);
                    if (variant != null)
                    {
                        variant.Stock = Math.Max(0, variant.Stock - d.Quantity);
                        var prod = await _db.Products.FindAsync(variant.ProductId);
                        if (prod != null) prod.SoldCount += d.Quantity;
                    }
                }
                o.StockDeducted = true;
            }
            // Hủy đơn ĐÃ trừ kho → hoàn trả tồn kho + giảm SoldCount
            else if (status == "Cancelled" && o.StockDeducted)
            {
                foreach (var d in o.OrderDetails.Where(d => d.VariantId != null))
                {
                    var variant = await _db.ProductVariants.FindAsync(d.VariantId);
                    if (variant != null)
                    {
                        variant.Stock += d.Quantity;
                        var prod = await _db.Products.FindAsync(variant.ProductId);
                        if (prod != null) prod.SoldCount = Math.Max(0, prod.SoldCount - d.Quantity);
                    }
                }
                o.StockDeducted = false;
            }

            o.OrderStatus = status;
            o.UpdatedAt = DateTime.Now;
            if (status == "Completed") o.PaymentStatus = "Paid";
            await _db.SaveChangesAsync();

            // Thông báo cho khách hàng về trạng thái đơn
            if (o.UserId != null)
            {
                var label = status switch
                {
                    "Confirmed" => "đã được xác nhận",
                    "Shipping" => "đang được giao đến bạn",
                    "Completed" => "đã giao thành công",
                    "Cancelled" => "đã bị hủy",
                    _ => "đang chờ xử lý"
                };
                await AddNotificationAsync(o.UserId, "Cập nhật đơn hàng 📦",
                    $"Đơn hàng {o.OrderCode} {label}.", "order");

                // Email cập nhật trạng thái
                var buyer = await _db.Users.FindAsync(o.UserId);
                if (buyer != null)
                    await _email.SendAsync(buyer.Email, $"Đơn hàng {o.OrderCode} {label} - MONO.WEAR",
                        ThoiTrang.Services.EmailTemplate.Wrap("Cập nhật đơn hàng 📦",
                            $"Xin chào {buyer.FullName},\n\nĐơn hàng {o.OrderCode} của bạn {label}.\n\nTổng tiền: {o.TotalAmount:#,##0}₫\n\nCảm ơn bạn đã mua sắm tại MONO.WEAR!"));
            }
            return Json(new { success = true });
        }

        // Chuyển tên có dấu thành slug không dấu
        private static string ToSlug(string input)
        {
            var s = input.Trim().ToLowerInvariant();
            var norm = s.Normalize(System.Text.NormalizationForm.FormD);
            var sb = new System.Text.StringBuilder();
            foreach (var ch in norm)
            {
                var uc = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch);
                if (uc != System.Globalization.UnicodeCategory.NonSpacingMark) sb.Append(ch);
            }
            s = sb.ToString().Normalize(System.Text.NormalizationForm.FormC);
            s = s.Replace('đ', 'd');
            var slug = System.Text.RegularExpressions.Regex.Replace(s, "[^a-z0-9]+", "-").Trim('-');
            return string.IsNullOrEmpty(slug) ? "sp-" + Guid.NewGuid().ToString("N").Substring(0, 6) : slug;
        }

        // ===================== HỒ SƠ CÁ NHÂN =====================
        // ===================== CẬP NHẬT ẢNH ĐẠI DIỆN =====================
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> UpdateAvatar(IFormFile avatar)
        {
            if (avatar == null || avatar.Length == 0)
                return BadRequest(new { message = "Vui lòng chọn ảnh" });
            if (avatar.Length > 5 * 1024 * 1024)
                return BadRequest(new { message = "Ảnh không được vượt quá 5MB" });

            var ext = Path.GetExtension(avatar.FileName).ToLowerInvariant();
            string[] allowed = [".jpg", ".jpeg", ".png", ".webp", ".gif"];
            if (!allowed.Contains(ext))
                return BadRequest(new { message = "Chỉ hỗ trợ ảnh JPG, PNG, WebP, GIF" });

            var uid = CurrentUserId();
            var user = await _db.Users.FindAsync(uid);
            if (user == null) return NotFound();

            // Xóa ảnh cũ nếu có
            if (!string.IsNullOrEmpty(user.AvatarUrl))
            {
                var oldPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", user.AvatarUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
            }

            // Lưu ảnh mới
            var dir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "avatars");
            Directory.CreateDirectory(dir);
            var fileName = $"av_{uid}_{DateTime.Now:yyyyMMddHHmmss}{ext}";
            var savePath = Path.Combine(dir, fileName);
            using (var stream = System.IO.File.Create(savePath))
                await avatar.CopyToAsync(stream);

            user.AvatarUrl = $"/uploads/avatars/{fileName}";
            await _db.SaveChangesAsync();

            return Json(new { success = true, avatarUrl = user.AvatarUrl });
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> UpdateProfile(string fullName, string phone, string gender, DateTime? birthDate)
        {
            var uid = CurrentUserId();
            var u = await _db.Users.FindAsync(uid);
            if (u == null) return NotFound();
            if (!string.IsNullOrWhiteSpace(fullName)) u.FullName = fullName.Trim();
            u.Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
            u.Gender = string.IsNullOrWhiteSpace(gender) ? null : gender.Trim();
            u.BirthDate = birthDate.HasValue ? DateOnly.FromDateTime(birthDate.Value) : null;
            await _db.SaveChangesAsync();
            return Json(new { success = true });
        }

        // ===================== ĐỊA CHỈ =====================
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AddAddress(string receiverName, string phone, string province, string district, string ward, string addressLine, bool isDefault = false)
        {
            var uid = CurrentUserId();
            if (string.IsNullOrWhiteSpace(receiverName) || string.IsNullOrWhiteSpace(phone) || string.IsNullOrWhiteSpace(addressLine))
                return BadRequest(new { message = "Thiếu thông tin địa chỉ." });

            bool isFirst = !await _db.Addresses.AnyAsync(a => a.UserId == uid);
            if (isDefault || isFirst)
            {
                foreach (var a in _db.Addresses.Where(a => a.UserId == uid && a.IsDefault)) a.IsDefault = false;
            }
            _db.Addresses.Add(new Address
            {
                UserId = uid!.Value,
                ReceiverName = receiverName.Trim(),
                Phone = phone.Trim(),
                Province = (province ?? "").Trim(),
                District = (district ?? "").Trim(),
                Ward = string.IsNullOrWhiteSpace(ward) ? null : ward.Trim(),
                AddressLine = addressLine.Trim(),
                IsDefault = isDefault || isFirst
            });
            await _db.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> DeleteAddress(int addressId)
        {
            var uid = CurrentUserId();
            var a = await _db.Addresses.FirstOrDefaultAsync(x => x.AddressId == addressId && x.UserId == uid);
            if (a != null) { _db.Addresses.Remove(a); await _db.SaveChangesAsync(); }
            return Json(new { success = true });
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> SetDefaultAddress(int addressId)
        {
            var uid = CurrentUserId();
            foreach (var a in _db.Addresses.Where(a => a.UserId == uid)) a.IsDefault = (a.AddressId == addressId);
            await _db.SaveChangesAsync();
            return Json(new { success = true });
        }

        // ===================== PHƯƠNG THỨC THANH TOÁN =====================
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AddPaymentMethod(string type, string label, string detail, bool isDefault = false)
        {
            var uid = CurrentUserId();
            if (string.IsNullOrWhiteSpace(label)) return BadRequest(new { message = "Thiếu thông tin." });
            bool isFirst = !await _db.PaymentMethods.AnyAsync(p => p.UserId == uid);
            if (isDefault || isFirst)
                foreach (var p in _db.PaymentMethods.Where(p => p.UserId == uid && p.IsDefault)) p.IsDefault = false;
            _db.PaymentMethods.Add(new PaymentMethod
            {
                UserId = uid!.Value,
                Type = string.IsNullOrWhiteSpace(type) ? "momo" : type,
                Label = label.Trim(),
                Detail = string.IsNullOrWhiteSpace(detail) ? null : detail.Trim(),
                IsDefault = isDefault || isFirst
            });
            await _db.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> DeletePaymentMethod(int paymentMethodId)
        {
            var uid = CurrentUserId();
            var p = await _db.PaymentMethods.FirstOrDefaultAsync(x => x.PaymentMethodId == paymentMethodId && x.UserId == uid);
            if (p != null) { _db.PaymentMethods.Remove(p); await _db.SaveChangesAsync(); }
            return Json(new { success = true });
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> SetDefaultPaymentMethod(int paymentMethodId)
        {
            var uid = CurrentUserId();
            foreach (var p in _db.PaymentMethods.Where(p => p.UserId == uid)) p.IsDefault = (p.PaymentMethodId == paymentMethodId);
            await _db.SaveChangesAsync();
            return Json(new { success = true });
        }

        // Đọc 1 cấu hình bật/tắt
        private async Task<bool> GetSettingAsync(string key, bool def = true)
        {
            var s = await _db.SiteSettings.FirstOrDefaultAsync(x => x.SettingKey == key);
            return s == null ? def : s.Value == "true";
        }

        // Cấu hình cho chat widget (autoreply)
        [HttpGet]
        public async Task<IActionResult> ChatConfig()
        {
            return Json(new
            {
                autoreply = await GetSettingAsync("chat_autoreply", true),
                loggedIn = CurrentUserId() != null
            });
        }

        // ===================== CHATBOT TỰ ĐỘNG (không cần đăng nhập) =====================
        [HttpPost]
        public async Task<IActionResult> ChatBotReply(string message)
        {
            message = (message ?? "").Trim();
            var m = message.ToLowerInvariant();
            bool loggedIn = CurrentUserId() != null;

            var mNoDia = NoDia(m);
            bool Has(params string[] kws) => kws.Any(w => mNoDia.Contains(NoDia(w)));

            // Tách từ khóa sản phẩm
            var stop = new[] { "cho", "minh", "hoi", "muon", "co", "shop", "ban", "the", "nao", "gia", "bao", "nhieu", "xem", "tim", "mua", "nay", "kia", "oi", "em", "khong", "duoi", "tam", "khoang", "tren", "gioi", "thieu", "tu", "van", "toi", "la", "gi", "nhat" };
            var words = mNoDia.Split(new[] { ' ', ',', '.', '?', '!' }, StringSplitOptions.RemoveEmptyEntries)
                         .Where(w => w.Length >= 2 && !stop.Contains(w) && !w.All(char.IsDigit)).Distinct().ToList();
            var allActive = await ProductsQuery().ToListAsync();
            List<Product> prodMatch = words.Count > 0
                ? allActive.Where(p => words.Any(w => NoDia(p.Name ?? "").Contains(w) || NoDia(p.Category?.Name ?? "").Contains(w))).OrderByDescending(p => p.SoldCount).Take(3).ToList()
                : new List<Product>();

            // Phân tích khoảng giá ("dưới 500k", "tầm 300k", "300-600k")
            var nums = System.Text.RegularExpressions.Regex.Matches(mNoDia, @"(\d+)\s*(k|nghin|nghìn|ngan|tr|trieu|triệu|m)?")
                .Cast<System.Text.RegularExpressions.Match>()
                .Where(x => x.Groups[1].Success)
                .Select(x => {
                    long v = long.Parse(x.Groups[1].Value); var unit = x.Groups[2].Value;
                    if (unit == "tr" || unit == "trieu" || unit == "triệu" || unit == "m") v *= 1000000;
                    else if (unit.StartsWith("k") || unit.StartsWith("ng")) v *= 1000;
                    else if (v < 1000) v *= 1000;     // "300" hiểu là 300k
                    return (decimal)v;
                }).Where(v => v >= 10000).ToList();

            string reply;
            bool needLogin = false, checkout = false;
            int? addProductId = null;
            string[] chips;
            string[] defaultChips = { "Sản phẩm bán chạy 🔥", "Đang khuyến mãi 🎁", "Theo dõi đơn 📦", "Tư vấn chọn size 📏", "Địa chỉ cửa hàng 📍" };
            List<object> products = new();

            // ===== (1) Theo dõi đơn hàng (động) =====
            if (Has("theo doi don", "don hang cua toi", "don cua toi", "kiem tra don", "don toi dau", "tinh trang don", "don da giao", "trang thai don"))
            {
                chips = new[] { "Sản phẩm bán chạy 🔥", "Đang khuyến mãi 🎁", "Đổi trả thế nào? 🔄" };
                if (!loggedIn) { needLogin = true; reply = "Dạ anh/chị vui lòng đăng nhập để em tra cứu đơn hàng giúp mình nha 🥰"; }
                else
                {
                    var od = await _db.Orders.Where(o => o.UserId == CurrentUserId()).OrderByDescending(o => o.OrderId).FirstOrDefaultAsync();
                    if (od == null) reply = "Dạ anh/chị chưa có đơn hàng nào ạ. Mua sắm ngay để em phục vụ nhé 🛍️";
                    else
                    {
                        var st = od.OrderStatus switch { "Completed" => "đã giao thành công ✅", "Shipping" => "đang trên đường giao 🚚", "Confirmed" => "đã được xác nhận, đang chuẩn bị 📦", "Cancelled" => "đã hủy ❌", _ => "đang chờ xử lý ⏳" };
                        reply = $"Dạ đơn gần nhất của anh/chị là #{od.OrderCode} ({od.TotalAmount:#,##0}₫) hiện {st} ạ. Anh/chị xem chi tiết trong mục Đơn hàng của tài khoản nhé!";
                    }
                }
            }
            // ===== (2) Khuyến mãi / Voucher (động - liệt kê mã thật) =====
            else if (Has("khuyen mai", "voucher", "ma giam", "uu dai", "giam gia", "sale", "ma khuyen mai", "coupon"))
            {
                var vchs = await _db.Vouchers.Where(v => v.IsActive && v.Quantity > 0).OrderByDescending(v => v.DiscountValue).Take(3).ToListAsync();
                if (vchs.Count == 0) reply = "Dạ hiện chưa có mã nào ạ, anh/chị theo dõi trang Sale để cập nhật ưu đãi mới nhé 🎁";
                else
                {
                    var lines = string.Join(" · ", vchs.Select(v => v.DiscountType == "percent" ? $"{v.Code} (-{v.DiscountValue:#,##0}%)" : $"{v.Code} (-{v.DiscountValue:#,##0}₫)"));
                    reply = $"Dạ MONO.WEAR đang có các mã: {lines} 🎁 Anh/chị vào trang Sale bấm \"Săn mã\" để lưu nha!";
                }
                chips = new[] { "Săn voucher", "Sản phẩm đang sale 🔥", "Theo dõi đơn 📦" };
            }
            // ===== (3) Tìm theo tầm giá (động) =====
            else if (nums.Count > 0 && (Has("duoi", "tam", "khoang", "tren", "gia", "re") || prodMatch.Count > 0))
            {
                IEnumerable<Product> pool = allActive;
                if (prodMatch.Count > 0) pool = allActive.Where(p => words.Any(w => NoDia(p.Name ?? "").Contains(w) || NoDia(p.Category?.Name ?? "").Contains(w)));
                List<Product> res;
                if (Has("duoi", "re hon", "khong qua") && nums.Count >= 1) res = pool.Where(p => p.Price <= nums.Max()).ToList();
                else if (Has("tren") && nums.Count >= 1) res = pool.Where(p => p.Price >= nums.Min()).ToList();
                else if (nums.Count >= 2) { var lo = nums.Min(); var hi = nums.Max(); res = pool.Where(p => p.Price >= lo && p.Price <= hi).ToList(); }
                else { var c = nums.First(); res = pool.Where(p => p.Price >= c * 0.7m && p.Price <= c * 1.3m).ToList(); }
                res = res.OrderBy(p => p.Price).Take(4).ToList();
                if (res.Count > 0) { reply = $"Dạ em tìm thấy {res.Count} mẫu trong tầm giá anh/chị mong muốn nè 👇 Mẫu nào ưng em chốt đơn liền ạ!"; products = res.Select(ToCard).ToList(); }
                else reply = "Dạ trong tầm giá đó hiện chưa có mẫu phù hợp ạ 😅 Anh/chị thử khoảng giá khác hoặc xem mẫu bán chạy nhé!";
                chips = new[] { "Dưới 500k", "Tầm 1 triệu", "Sản phẩm bán chạy 🔥" };
            }
            // ===== (4) Mua / chốt đơn (động - thêm giỏ) =====
            else if (Has("chot don", "dat hang", "dat don", "mua ngay", "mua luon", "lay cai nay", "dat mua", "chot luon", "thanh toan luon"))
            {
                chips = new[] { "Sản phẩm bán chạy 🔥", "Tư vấn chọn size 📏", "Theo dõi đơn 📦" };
                if (!loggedIn) { needLogin = true; reply = "Dạ để em chốt đơn, anh/chị vui lòng đăng nhập tài khoản trước nha 🥰 Sau đó em thêm sản phẩm vào giỏ và đưa mình tới trang thanh toán liền ạ!"; }
                else
                {
                    var pick = prodMatch.FirstOrDefault() ?? allActive.OrderByDescending(p => p.SoldCount).FirstOrDefault();
                    if (pick != null)
                    {
                        addProductId = pick.ProductId; checkout = true;
                        reply = $"Dạ tuyệt vời ạ! Em đã thêm \"{pick.Name}\" vào giỏ rồi nha 🛍️ Em dẫn mình qua trang thanh toán để chốt đơn nhé. Cảm ơn anh/chị đã tin tưởng MONO.WEAR ❤️";
                        products = new List<object> { ToCard(pick) };
                    }
                    else reply = "Dạ kho đang cập nhật ạ, anh/chị quay lại sau giúp em nha 🙏";
                }
            }
            else
            {
                // ===== (5) DÒ KHO TRI THỨC: chọn câu trả lời khớp nhất =====
                var kb = await _db.ChatKnowledges.Where(k => k.IsActive).ToListAsync();
                ChatKnowledge? best = null; int bestScore = 0;
                foreach (var k in kb)
                {
                    int score = 0;
                    foreach (var raw in k.Keywords.Split(',', StringSplitOptions.RemoveEmptyEntries))
                    {
                        var kw = NoDia(raw.Trim());
                        if (kw.Length >= 2 && mNoDia.Contains(kw)) score += kw.Length; // từ khóa dài/cụ thể = điểm cao
                    }
                    if (score > bestScore || (score == bestScore && score > 0 && k.Priority > (best?.Priority ?? -1)))
                    { best = k; bestScore = score; }
                }

                if (best != null && bestScore > 0)
                {
                    reply = best.Answer;
                    if (best.Topic == "greeting")
                    {
                        products = allActive.OrderByDescending(p => p.SoldCount).Take(3).Select(ToCard).ToList();
                        chips = defaultChips;
                    }
                    else chips = defaultChips;
                }
                // ===== (6) Không có trong kho nhưng khớp sản phẩm =====
                else if (prodMatch.Count > 0)
                {
                    reply = "Dạ MONO.WEAR có những mẫu này hợp với anh/chị nè 👇 Anh/chị thích mẫu nào em tư vấn thêm hoặc chốt đơn luôn ạ!";
                    products = prodMatch.Select(ToCard).ToList();
                    chips = new[] { "Mẫu rẻ hơn", "Tư vấn chọn size 📏", "Chốt đơn ngay 🛒" };
                }
                // ===== (7) Mặc định =====
                else
                {
                    reply = "Dạ em chưa hiểu rõ ý anh/chị 😅 Anh/chị có thể hỏi em về sản phẩm, tầm giá, khuyến mãi, vận chuyển, đơn hàng... hoặc chọn nhanh bên dưới nha!";
                    products = allActive.OrderByDescending(p => p.SoldCount).Take(3).Select(ToCard).ToList();
                    chips = defaultChips;
                }
            }

            return Json(new { reply, products, needLogin, checkout, addProductId, chips });
        }

        // Bỏ dấu + viết thường (so khớp tiếng Việt không phân biệt dấu)
        private static string NoDia(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            var norm = s.ToLowerInvariant().Normalize(System.Text.NormalizationForm.FormD);
            var sb = new System.Text.StringBuilder();
            foreach (var ch in norm)
                if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch) != System.Globalization.UnicodeCategory.NonSpacingMark)
                    sb.Append(ch);
            return sb.ToString().Normalize(System.Text.NormalizationForm.FormC).Replace('đ', 'd');
        }

        private static object ToCard(Product p) => new
        {
            id = p.ProductId,
            name = p.Name,
            price = p.Price,
            category = p.Category?.Name ?? ""
        };

        // ===================== CHAT (khách) =====================
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> SendMessage(string content)
        {
            var uid = CurrentUserId();
            if (string.IsNullOrWhiteSpace(content)) return BadRequest();
            _db.ChatMessages.Add(new ChatMessage { UserId = uid!.Value, FromAdmin = false, Content = content.Trim() });
            await _db.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetMessages()
        {
            var uid = CurrentUserId();
            // đánh dấu tin admin là đã đọc
            var unread = _db.ChatMessages.Where(m => m.UserId == uid && m.FromAdmin && !m.IsRead);
            foreach (var m in unread) m.IsRead = true;
            await _db.SaveChangesAsync();

            var msgs = await _db.ChatMessages
                .Where(m => m.UserId == uid)
                .OrderBy(m => m.ChatMessageId)
                .Select(m => new { m.Content, m.FromAdmin, time = m.CreatedAt.ToString("HH:mm") })
                .ToListAsync();
            return Json(msgs);
        }

        // ===================== CHAT (admin) =====================
        // ===================== SỰ KIỆN (LÊN LỊCH) =====================
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetEvents()
        {
            var evs = await _db.ScheduledEvents
                .OrderBy(e => e.EventDate)
                .Select(e => new
                {
                    id = e.ScheduledEventId,
                    day = e.EventDate.Day.ToString("D2"),
                    month = "T" + e.EventDate.Month + "/" + (e.EventDate.Year % 100).ToString("D2"),
                    title = e.Title,
                    meta = e.Meta,
                    tags = e.Tags,
                    status = e.Status
                }).ToListAsync();
            return Json(evs);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddEvent(string title, string meta, DateTime? eventDate, string tags, string status)
        {
            if (string.IsNullOrWhiteSpace(title)) return BadRequest(new { message = "Thiếu tên sự kiện" });
            _db.ScheduledEvents.Add(new ScheduledEvent
            {
                Title = title.Trim(),
                Meta = string.IsNullOrWhiteSpace(meta) ? null : meta.Trim(),
                EventDate = eventDate ?? DateTime.Now,
                Tags = string.IsNullOrWhiteSpace(tags) ? "sale" : tags,
                Status = string.IsNullOrWhiteSpace(status) ? "scheduled" : status
            });
            await _db.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateEvent(int id, string title, string meta, DateTime? eventDate, string tags, string status)
        {
            var e = await _db.ScheduledEvents.FindAsync(id);
            if (e == null) return NotFound();
            if (!string.IsNullOrWhiteSpace(title)) e.Title = title.Trim();
            e.Meta = string.IsNullOrWhiteSpace(meta) ? null : meta.Trim();
            if (eventDate.HasValue) e.EventDate = eventDate.Value;
            if (!string.IsNullOrWhiteSpace(tags)) e.Tags = tags;
            if (!string.IsNullOrWhiteSpace(status)) e.Status = status;
            await _db.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            var e = await _db.ScheduledEvents.FindAsync(id);
            if (e != null) { _db.ScheduledEvents.Remove(e); await _db.SaveChangesAsync(); }
            return Json(new { success = true });
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ToggleEventStatus(int id)
        {
            var e = await _db.ScheduledEvents.FindAsync(id);
            if (e == null) return NotFound();
            e.Status = e.Status == "active" ? "scheduled" : "active";
            await _db.SaveChangesAsync();
            return Json(new { success = true, status = e.Status });
        }

        // ===================== ĐÁNH GIÁ SẢN PHẨM =====================
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AddReview(int productId, byte rating, string? content, List<IFormFile>? reviewImages)
        {
            if (rating < 1 || rating > 5) return BadRequest(new { message = "Điểm đánh giá không hợp lệ" });
            var uid = CurrentUserId();
            // Kiểm tra đã mua sản phẩm chưa
            bool hasBought = await _db.OrderDetails
                .AnyAsync(d => d.Variant!.ProductId == productId &&
                               _db.Orders.Any(o => o.OrderId == d.OrderId && o.UserId == uid && o.OrderStatus == "Completed"));

            // Lưu ảnh đính kèm (tối đa 3 ảnh, ≤ 5MB mỗi ảnh)
            var imagePaths = new List<string>();
            if (reviewImages != null)
            {
                var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "reviews");
                Directory.CreateDirectory(uploadDir);
                string[] allowed = [".jpg", ".jpeg", ".png", ".webp"];
                foreach (var file in reviewImages.Take(3))
                {
                    if (file.Length > 5 * 1024 * 1024) continue; // bỏ qua file > 5MB
                    var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                    if (!allowed.Contains(ext)) continue;
                    var fileName = $"rv_{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid():N}{ext}";
                    var savePath = Path.Combine(uploadDir, fileName);
                    using var stream = System.IO.File.Create(savePath);
                    await file.CopyToAsync(stream);
                    imagePaths.Add($"/uploads/reviews/{fileName}");
                }
            }

            _db.Reviews.Add(new Review
            {
                ProductId = productId,
                UserId = uid,
                Rating = rating,
                Content = string.IsNullOrWhiteSpace(content) ? null : content.Trim(),
                IsVerified = hasBought,
                Images = imagePaths.Count > 0 ? string.Join(",", imagePaths) : null,
                CreatedAt = DateTime.Now
            });
            await _db.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> HelpfulReview(int reviewId)
        {
            var r = await _db.Reviews.FindAsync(reviewId);
            if (r == null) return NotFound();
            r.HelpfulCount++;
            await _db.SaveChangesAsync();
            return Json(new { success = true, count = r.HelpfulCount });
        }

        // ===================== HỎI ĐÁP SẢN PHẨM =====================
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AddQuestion(int productId, string question)
        {
            if (string.IsNullOrWhiteSpace(question)) return BadRequest(new { message = "Câu hỏi không được để trống" });
            var uid = CurrentUserId();
            _db.ProductQuestions.Add(new ProductQuestion
            {
                ProductId = productId,
                UserId = uid,
                Question = question.Trim(),
                CreatedAt = DateTime.Now
            });
            await _db.SaveChangesAsync();
            return Json(new { success = true });
        }

        // ===================== KHÁCH HỦY ĐƠN =====================
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CancelOrder(int orderId)
        {
            var uid = CurrentUserId();
            var order = await _db.Orders.FirstOrDefaultAsync(o => o.OrderId == orderId && o.UserId == uid);
            if (order == null) return NotFound();
            // Chỉ cho hủy khi đơn chưa giao
            if (order.OrderStatus is not ("Pending" or "Confirmed"))
                return BadRequest(new { message = "Đơn đang giao hoặc đã hoàn tất, không thể hủy." });

            order.OrderStatus = "Cancelled";
            order.UpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync();
            await AddNotificationAsync(uid, "Đơn hàng đã hủy",
                $"Đơn #{order.OrderCode} đã được hủy theo yêu cầu của bạn.", "order");
            return Json(new { success = true });
        }

        // ===================== CHI TIẾT ĐƠN HÀNG =====================
        [Authorize]
        public async Task<IActionResult> GetOrderDetail(int orderId)
        {
            var uid = CurrentUserId();
            var order = await _db.Orders
                .Include(o => o.OrderDetails).ThenInclude(d => d.Variant).ThenInclude(v => v!.Color)
                .Include(o => o.OrderDetails).ThenInclude(d => d.Variant).ThenInclude(v => v!.Size)
                .Include(o => o.Voucher)
                .FirstOrDefaultAsync(o => o.OrderId == orderId && (o.UserId == uid || User.IsInRole("Admin")));
            if (order == null) return NotFound();

            return Json(new
            {
                orderId = order.OrderId,
                orderCode = order.OrderCode,
                status = order.OrderStatus,
                paymentMethod = order.PaymentMethod,
                paymentStatus = order.PaymentStatus,
                shippingMethod = order.ShippingMethod,
                receiverName = order.ReceiverName,
                receiverPhone = order.ReceiverPhone,
                shippingAddress = order.ShippingAddress,
                note = order.Note,
                subtotal = order.Subtotal,
                voucherDiscount = order.VoucherDiscount,
                shippingFee = order.ShippingFee,
                totalAmount = order.TotalAmount,
                createdAt = order.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
                voucherCode = order.Voucher?.Code,
                items = order.OrderDetails.Select(d => new
                {
                    productName = d.ProductName,
                    variantInfo = d.VariantInfo,
                    unitPrice = d.UnitPrice,
                    quantity = d.Quantity,
                    lineTotal = d.LineTotal
                }).ToList()
            });
        }

        // ===================== ADMIN: THỐNG KÊ THEO KỲ =====================
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetStats(string period = "thismonth")
        {
            var orders = await _db.Orders
                .Where(o => o.OrderStatus != "Cancelled")
                .Select(o => new { o.TotalAmount, o.CreatedAt })
                .ToListAsync();

            var today = DateTime.Today;
            DateTime from, to;
            var chart = new List<object>();
            string label;

            switch (period)
            {
                case "lastmonth":
                    {
                        var m = new DateTime(today.Year, today.Month, 1).AddMonths(-1);
                        from = m; to = m.AddMonths(1);
                        label = $"Tháng {m.Month}/{m.Year}";
                        // Biểu đồ: theo ngày trong tháng
                        int days = DateTime.DaysInMonth(m.Year, m.Month);
                        for (int d = 1; d <= days; d++)
                        {
                            var day = new DateTime(m.Year, m.Month, d);
                            var sum = orders.Where(o => o.CreatedAt.Date == day).Sum(o => o.TotalAmount);
                            chart.Add(new { label = d.ToString(), value = sum });
                        }
                        break;
                    }
                case "3months":
                    {
                        from = new DateTime(today.Year, today.Month, 1).AddMonths(-2); to = today.AddDays(1);
                        label = "3 tháng gần nhất";
                        for (int i = 2; i >= 0; i--)
                        {
                            var mm = new DateTime(today.Year, today.Month, 1).AddMonths(-i);
                            var sum = orders.Where(o => o.CreatedAt.Year == mm.Year && o.CreatedAt.Month == mm.Month).Sum(o => o.TotalAmount);
                            chart.Add(new { label = $"T{mm.Month}/{mm.Year % 100}", value = sum });
                        }
                        break;
                    }
                case "year":
                    {
                        from = new DateTime(today.Year, 1, 1); to = new DateTime(today.Year + 1, 1, 1);
                        label = $"Năm {today.Year}";
                        for (int mo = 1; mo <= 12; mo++)
                        {
                            var sum = orders.Where(o => o.CreatedAt.Year == today.Year && o.CreatedAt.Month == mo).Sum(o => o.TotalAmount);
                            chart.Add(new { label = $"T{mo}", value = sum });
                        }
                        break;
                    }
                default: // thismonth
                    {
                        var m = new DateTime(today.Year, today.Month, 1);
                        from = m; to = m.AddMonths(1);
                        label = $"Tháng {m.Month}/{m.Year}";
                        int days = DateTime.DaysInMonth(m.Year, m.Month);
                        for (int d = 1; d <= days; d++)
                        {
                            var day = new DateTime(m.Year, m.Month, d);
                            var sum = orders.Where(o => o.CreatedAt.Date == day).Sum(o => o.TotalAmount);
                            chart.Add(new { label = d.ToString(), value = sum });
                        }
                        break;
                    }
            }

            var inRange = orders.Where(o => o.CreatedAt >= from && o.CreatedAt < to).ToList();
            decimal revenue = inRange.Sum(o => o.TotalAmount);
            int orderCount = inRange.Count;
            decimal aov = orderCount > 0 ? revenue / orderCount : 0;
            int newCustomers = await _db.Users.CountAsync(u => u.Role == "Customer" && u.CreatedAt >= from && u.CreatedAt < to);

            return Json(new { period, label, revenue, orderCount, aov, newCustomers, chart });
        }

        // ===================== ADMIN: QUẢN LÝ HỎI ĐÁP =====================
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAdminQuestions(string? filter)
        {
            var query = _db.ProductQuestions
                .Include(q => q.User)
                .Include(q => q.Product)
                .AsQueryable();
            if (filter == "unanswered") query = query.Where(q => q.Answer == null);
            else if (filter == "answered") query = query.Where(q => q.Answer != null);

            var list = await query
                .OrderByDescending(q => q.QuestionId)
                .Take(100)
                .Select(q => new
                {
                    questionId = q.QuestionId,
                    productId = q.ProductId,
                    productName = q.Product!.Name,
                    customerName = q.User != null ? q.User.FullName : "Khách",
                    question = q.Question,
                    answer = q.Answer,
                    answeredAt = q.AnsweredAt,
                    createdAt = q.CreatedAt
                }).ToListAsync();
            return Json(list);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AnswerQuestion(int questionId, string answer)
        {
            if (string.IsNullOrWhiteSpace(answer))
                return BadRequest(new { message = "Câu trả lời không được để trống" });
            var q = await _db.ProductQuestions.FindAsync(questionId);
            if (q == null) return NotFound();
            q.Answer = answer.Trim();
            q.AnsweredAt = DateTime.Now;
            await _db.SaveChangesAsync();
            // Báo cho khách (nếu có tài khoản)
            if (q.UserId != null)
                await AddNotificationAsync(q.UserId, "Câu hỏi của bạn đã được trả lời 💬",
                    "MONO.WEAR vừa trả lời câu hỏi của bạn về sản phẩm. Xem ngay nhé!", "info");
            return Json(new { success = true });
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteQuestion(int questionId)
        {
            var q = await _db.ProductQuestions.FindAsync(questionId);
            if (q != null) { _db.ProductQuestions.Remove(q); await _db.SaveChangesAsync(); }
            return Json(new { success = true });
        }

        // ===================== ADMIN: QUẢN LÝ ĐÁNH GIÁ =====================
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAdminReviews(int? star)
        {
            var query = _db.Reviews
                .Include(r => r.User)
                .Include(r => r.Product)
                .AsQueryable();
            if (star.HasValue && star.Value >= 1 && star.Value <= 5)
                query = query.Where(r => r.Rating == star.Value);

            var list = await query
                .OrderByDescending(r => r.ReviewId)
                .Take(100)
                .Select(r => new
                {
                    reviewId = r.ReviewId,
                    productId = r.ProductId,
                    productName = r.Product!.Name,
                    customerName = r.User != null ? r.User.FullName : "Khách ẩn danh",
                    rating = r.Rating,
                    content = r.Content,
                    images = r.Images,
                    isVerified = r.IsVerified,
                    helpfulCount = r.HelpfulCount,
                    createdAt = r.CreatedAt
                }).ToListAsync();
            return Json(list);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteReview(int reviewId)
        {
            var r = await _db.Reviews.FindAsync(reviewId);
            if (r != null) { _db.Reviews.Remove(r); await _db.SaveChangesAsync(); }
            return Json(new { success = true });
        }

        // ===================== ADMIN: GỬI EMAIL + THÔNG BÁO TỚI KHÁCH =====================
        // Gửi email thật qua SMTP (nếu bật) + tạo Thông báo trong hệ thống.
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SendCustomerEmail(int? userId, string subject, string body, bool toAll = false)
        {
            if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(body))
                return BadRequest(new { message = "Thiếu tiêu đề hoặc nội dung" });
            subject = subject.Trim();
            body = body.Trim();
            var html = ThoiTrang.Services.EmailTemplate.Wrap(subject, body);

            int count = 0, emailSent = 0;
            if (toAll)
            {
                var customers = await _db.Users.Where(u => u.Role == "Customer" && u.IsActive)
                    .Select(u => new { u.UserId, u.Email }).ToListAsync();
                foreach (var c in customers)
                {
                    await AddNotificationAsync(c.UserId, subject, body, "promo");
                    count++;
                    if (await _email.SendAsync(c.Email, subject, html)) emailSent++;
                }
            }
            else if (userId != null)
            {
                var c = await _db.Users.FindAsync(userId);
                if (c == null) return BadRequest(new { message = "Khách hàng không tồn tại" });
                await AddNotificationAsync(userId, subject, body, "promo");
                count = 1;
                if (await _email.SendAsync(c.Email, subject, html)) emailSent++;
            }
            else return BadRequest(new { message = "Chưa chọn người nhận" });

            return Json(new { success = true, count, emailSent, emailEnabled = _email.IsEnabled });
        }

        // ===================== ADMIN: QUẢN LÝ BỘ SƯU TẬP (CMS) =====================
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetCollections()
        {
            var list = await _db.Collections
                .OrderBy(c => c.DisplayOrder).ThenBy(c => c.CollectionId)
                .Select(c => new
                {
                    collectionId = c.CollectionId,
                    label = c.Label, title = c.Title, description = c.Description,
                    icon = c.Icon, coverClass = c.CoverClass,
                    linkUrl = c.LinkUrl, linkText = c.LinkText,
                    displayOrder = c.DisplayOrder, isActive = c.IsActive
                }).ToListAsync();
            return Json(list);
        }

        public class CollectionFormRequest
        {
            public int CollectionId { get; set; }
            public string? Label { get; set; }
            public string? Title { get; set; }
            public string? Description { get; set; }
            public string? Icon { get; set; }
            public string? CoverClass { get; set; }
            public string? LinkUrl { get; set; }
            public string? LinkText { get; set; }
            public int DisplayOrder { get; set; }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SaveCollection([FromBody] CollectionFormRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.Title))
                return BadRequest(new { message = "Vui lòng nhập tiêu đề bộ sưu tập." });

            Collection col;
            if (req.CollectionId > 0)
            {
                col = await _db.Collections.FindAsync(req.CollectionId);
                if (col == null) return NotFound();
            }
            else
            {
                col = new Collection { CreatedAt = DateTime.Now };
                _db.Collections.Add(col);
            }
            col.Label = req.Label?.Trim();
            col.Title = req.Title!.Trim();
            col.Description = req.Description?.Trim();
            col.Icon = string.IsNullOrWhiteSpace(req.Icon) ? "ti-hanger" : req.Icon.Trim();
            col.CoverClass = string.IsNullOrWhiteSpace(req.CoverClass) ? "bg-concept-1" : req.CoverClass.Trim();
            col.LinkUrl = req.LinkUrl?.Trim();
            col.LinkText = string.IsNullOrWhiteSpace(req.LinkText) ? "Khám phá chi tiết" : req.LinkText.Trim();
            col.DisplayOrder = req.DisplayOrder;
            await _db.SaveChangesAsync();
            return Json(new { success = true, collectionId = col.CollectionId });
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteCollection(int collectionId)
        {
            var c = await _db.Collections.FindAsync(collectionId);
            if (c != null) { _db.Collections.Remove(c); await _db.SaveChangesAsync(); }
            return Json(new { success = true });
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ToggleCollection(int collectionId)
        {
            var c = await _db.Collections.FindAsync(collectionId);
            if (c == null) return NotFound();
            c.IsActive = !c.IsActive;
            await _db.SaveChangesAsync();
            return Json(new { success = true, isActive = c.IsActive });
        }

        // ===================== ADMIN: QUẢN LÝ COMBO =====================
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetCombos()
        {
            var list = await _db.Combos
                .Include(c => c.Items).ThenInclude(i => i.Product)
                .OrderByDescending(c => c.ComboId)
                .Select(c => new
                {
                    comboId = c.ComboId,
                    name = c.Name,
                    description = c.Description,
                    comboPrice = c.ComboPrice,
                    oldPrice = c.OldPrice,
                    badge = c.Badge,
                    isActive = c.IsActive,
                    items = c.Items.Select(i => new { i.ProductId, productName = i.Product!.Name, i.Quantity }).ToList()
                }).ToListAsync();
            return Json(list);
        }

        public class ComboFormRequest
        {
            public int ComboId { get; set; }
            public string? Name { get; set; }
            public string? Description { get; set; }
            public decimal ComboPrice { get; set; }
            public decimal OldPrice { get; set; }
            public string? Badge { get; set; }
            public List<ComboItemDto> Items { get; set; } = new();
        }
        public class ComboItemDto { public int ProductId { get; set; } public int Quantity { get; set; } = 1; }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SaveCombo([FromBody] ComboFormRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.Name) || req.ComboPrice <= 0)
                return BadRequest(new { message = "Vui lòng nhập tên và giá combo hợp lệ." });
            if (req.Items == null || req.Items.Count == 0)
                return BadRequest(new { message = "Combo cần ít nhất 1 sản phẩm." });

            Combo combo;
            if (req.ComboId > 0)
            {
                combo = await _db.Combos.Include(c => c.Items).FirstOrDefaultAsync(c => c.ComboId == req.ComboId);
                if (combo == null) return NotFound();
                _db.ComboItems.RemoveRange(combo.Items);
            }
            else
            {
                combo = new Combo { CreatedAt = DateTime.Now };
                _db.Combos.Add(combo);
            }
            combo.Name = req.Name.Trim();
            combo.Description = string.IsNullOrWhiteSpace(req.Description) ? null : req.Description.Trim();
            combo.ComboPrice = req.ComboPrice;
            combo.OldPrice = req.OldPrice;
            combo.Badge = string.IsNullOrWhiteSpace(req.Badge) ? null : req.Badge;
            combo.Items = req.Items
                .Where(i => i.ProductId > 0)
                .Select(i => new ComboItem { ProductId = i.ProductId, Quantity = i.Quantity < 1 ? 1 : i.Quantity })
                .ToList();
            await _db.SaveChangesAsync();
            return Json(new { success = true, comboId = combo.ComboId });
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteCombo(int comboId)
        {
            var c = await _db.Combos.FindAsync(comboId);
            if (c != null) { _db.Combos.Remove(c); await _db.SaveChangesAsync(); }
            return Json(new { success = true });
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ToggleCombo(int comboId)
        {
            var c = await _db.Combos.FindAsync(comboId);
            if (c == null) return NotFound();
            c.IsActive = !c.IsActive;
            await _db.SaveChangesAsync();
            return Json(new { success = true, isActive = c.IsActive });
        }

        // ===================== QUẢN LÝ QUẢN TRỊ VIÊN (PHÂN QUYỀN) =====================
        // Chỉ Super Admin mới được thêm/sửa/xóa admin
        private async Task<bool> IsSuperAdminAsync()
        {
            var uid = CurrentUserId();
            var u = await _db.Users.FindAsync(uid);
            return u != null && u.Role == "Admin" && u.AdminTitle == "SuperAdmin";
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddAdmin(string fullName, string email, string password, string title)
        {
            if (!await IsSuperAdminAsync()) return StatusCode(403, new { message = "Chỉ Super Admin được thêm quản trị viên." });
            fullName = (fullName ?? "").Trim();
            email = (email ?? "").Trim();
            if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                return BadRequest(new { message = "Vui lòng nhập đầy đủ họ tên, email và mật khẩu." });
            if (password.Length < 8) return BadRequest(new { message = "Mật khẩu phải có ít nhất 8 ký tự." });
            if (await _db.Users.AnyAsync(u => u.Email == email)) return BadRequest(new { message = "Email này đã được sử dụng." });

            var allowedTitles = new[] { "SuperAdmin", "Manager", "Staff" };
            var t = allowedTitles.Contains(title) ? title : "Staff";

            var admin = new User
            {
                FullName = fullName,
                Email = email,
                Role = "Admin",
                AdminTitle = t,
                IsActive = true,
                CreatedAt = DateTime.Now
            };
            admin.PasswordHash = _hasher.HashPassword(admin, password);
            _db.Users.Add(admin);
            await _db.SaveChangesAsync();
            return Json(new { success = true, userId = admin.UserId });
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateAdminTitle(int userId, string title)
        {
            if (!await IsSuperAdminAsync()) return StatusCode(403, new { message = "Chỉ Super Admin được sửa quyền." });
            var u = await _db.Users.FindAsync(userId);
            if (u == null || u.Role != "Admin") return NotFound();
            if (u.Email == "admin@monowear.vn") return BadRequest(new { message = "Không thể đổi quyền của Super Admin gốc." });
            var allowedTitles = new[] { "SuperAdmin", "Manager", "Staff" };
            u.AdminTitle = allowedTitles.Contains(title) ? title : "Staff";
            await _db.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteAdmin(int userId)
        {
            if (!await IsSuperAdminAsync()) return StatusCode(403, new { message = "Chỉ Super Admin được xóa quản trị viên." });
            var u = await _db.Users.FindAsync(userId);
            if (u == null || u.Role != "Admin") return NotFound();
            if (u.Email == "admin@monowear.vn") return BadRequest(new { message = "Không thể xóa Super Admin gốc." });
            if (u.UserId == CurrentUserId()) return BadRequest(new { message = "Không thể tự xóa chính mình." });
            // Gỡ liên kết để giữ lịch sử
            foreach (var o in _db.Orders.Where(o => o.UserId == userId)) o.UserId = null;
            _db.Users.Remove(u);
            await _db.SaveChangesAsync();
            return Json(new { success = true });
        }

        // Xóa khách hàng (gỡ liên kết đơn/đánh giá để giữ lịch sử, rồi xóa)
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteCustomer(int userId)
        {
            var u = await _db.Users.FindAsync(userId);
            if (u == null || u.Role == "Admin") return BadRequest(new { message = "Không thể xóa tài khoản này" });

            foreach (var o in _db.Orders.Where(o => o.UserId == userId)) o.UserId = null;
            foreach (var r in _db.Reviews.Where(r => r.UserId == userId)) r.UserId = null;
            foreach (var qn in _db.ProductQuestions.Where(q => q.UserId == userId)) qn.UserId = null;
            await _db.SaveChangesAsync();

            _db.Users.Remove(u); // cascade: cart, wishlist, address, notification, chat, voucher, payment
            await _db.SaveChangesAsync();
            return Json(new { success = true });
        }

        // Khóa / mở khóa tài khoản khách
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ToggleUserActive(int userId)
        {
            var u = await _db.Users.FindAsync(userId);
            if (u == null || u.Role == "Admin") return BadRequest(new { message = "Không hợp lệ" });
            u.IsActive = !u.IsActive;
            await _db.SaveChangesAsync();
            return Json(new { success = true, locked = !u.IsActive });
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetCustomerOrders(int userId)
        {
            var orders = await _db.Orders
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderId).Take(5)
                .Select(o => new { o.OrderCode, date = o.CreatedAt.ToString("dd/MM/yyyy"), o.TotalAmount, o.OrderStatus })
                .ToListAsync();
            return Json(orders);
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetChatUsers()
        {
            var convos = await _db.ChatMessages
                .GroupBy(m => m.UserId)
                .Select(g => new
                {
                    userId = g.Key,
                    name = g.First().User!.FullName,
                    lastMessage = g.OrderByDescending(x => x.ChatMessageId).First().Content,
                    unread = g.Count(x => !x.FromAdmin && !x.IsRead)
                })
                .ToListAsync();
            return Json(convos);
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAdminMessages(int userId)
        {
            var unread = _db.ChatMessages.Where(m => m.UserId == userId && !m.FromAdmin && !m.IsRead);
            foreach (var m in unread) m.IsRead = true;
            await _db.SaveChangesAsync();

            var msgs = await _db.ChatMessages
                .Where(m => m.UserId == userId)
                .OrderBy(m => m.ChatMessageId)
                .Select(m => new { m.Content, m.FromAdmin, time = m.CreatedAt.ToString("HH:mm") })
                .ToListAsync();
            return Json(msgs);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SendAdminMessage(int userId, string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return BadRequest();
            _db.ChatMessages.Add(new ChatMessage { UserId = userId, FromAdmin = true, Content = content.Trim() });
            await _db.SaveChangesAsync();
            await AddNotificationAsync(userId, "Tin nhắn từ MONO.WEAR 💬", content.Trim(), "system");
            return Json(new { success = true });
        }

        // ===================== ĐĂNG KÝ / ĐĂNG NHẬP QUA MẠNG XÃ HỘI =====================
        [HttpPost]
        public async Task<IActionResult> SocialAuth(string provider, string email, string name)
        {
            email = (email ?? "").Trim().ToLower();
            provider = string.IsNullOrWhiteSpace(provider) ? "Google" : provider.Trim();
            if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
                return BadRequest(new { message = "Email không hợp lệ" });

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
            bool isNew = false;
            if (user == null)
            {
                user = new User
                {
                    FullName = string.IsNullOrWhiteSpace(name) ? email.Split('@')[0] : name.Trim(),
                    Email = email,
                    Role = "Customer",
                    IsActive = true
                };
                // Tài khoản mạng xã hội: đặt mật khẩu ngẫu nhiên (đăng nhập qua nút social)
                user.PasswordHash = _hasher.HashPassword(user, Guid.NewGuid().ToString("N"));
                _db.Users.Add(user);
                await _db.SaveChangesAsync();
                isNew = true;
                await AddNotificationAsync(user.UserId, $"Chào mừng đến MONO.WEAR 🎉",
                    $"Tài khoản của bạn đã được tạo qua {provider}. Khám phá ngay nhé!", "info");
            }
            else if (!user.IsActive)
            {
                return BadRequest(new { message = "Tài khoản đã bị khóa" });
            }

            await SignInUserAsync(user);
            return Json(new { success = true, isNew });
        }

        // ===================== ĐĂNG XUẤT =====================
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Index));
        }

        // ===================== ĐỔI MẬT KHẨU (đã đăng nhập) =====================
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(currentPassword) || string.IsNullOrWhiteSpace(newPassword))
                return BadRequest(new { message = "Vui lòng nhập đầy đủ mật khẩu." });
            if (newPassword.Length < 8)
                return BadRequest(new { message = "Mật khẩu mới phải có ít nhất 8 ký tự." });

            var uid = CurrentUserId();
            var user = await _db.Users.FindAsync(uid);
            if (user == null) return NotFound();

            var verify = _hasher.VerifyHashedPassword(user, user.PasswordHash, currentPassword);
            if (verify == PasswordVerificationResult.Failed)
                return BadRequest(new { message = "Mật khẩu hiện tại không đúng." });

            user.PasswordHash = _hasher.HashPassword(user, newPassword);
            await _db.SaveChangesAsync();
            return Json(new { success = true });
        }

        // ===================== QUÊN MẬT KHẨU (đặt lại tại chỗ) =====================
        // Lưu ý: chưa tích hợp email thật. Cho phép đặt lại trực tiếp khi xác nhận đúng email + SĐT đã đăng ký.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string email, string phone, string newPassword)
        {
            email = (email ?? "").Trim();
            phone = (phone ?? "").Trim();
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(newPassword))
                return BadRequest(new { message = "Vui lòng nhập email và mật khẩu mới." });
            if (newPassword.Length < 8)
                return BadRequest(new { message = "Mật khẩu mới phải có ít nhất 8 ký tự." });

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email && u.IsActive);
            if (user == null)
                return BadRequest(new { message = "Email không tồn tại trong hệ thống." });

            // Xác thực thêm bằng SĐT đã đăng ký (nếu tài khoản có SĐT)
            if (!string.IsNullOrEmpty(user.Phone) && user.Phone != phone)
                return BadRequest(new { message = "Số điện thoại không khớp với tài khoản." });

            user.PasswordHash = _hasher.HashPassword(user, newPassword);
            await _db.SaveChangesAsync();
            await AddNotificationAsync(user.UserId, "Mật khẩu đã được đặt lại 🔐",
                "Mật khẩu tài khoản của bạn vừa được thay đổi. Nếu không phải bạn thực hiện, vui lòng liên hệ ngay.", "system");
            await _email.SendAsync(user.Email, "Mật khẩu đã được đặt lại - MONO.WEAR",
                ThoiTrang.Services.EmailTemplate.Wrap("Mật khẩu đã được đặt lại 🔐",
                    $"Xin chào {user.FullName},\n\nMật khẩu tài khoản MONO.WEAR của bạn vừa được đặt lại thành công.\n\nNếu không phải bạn thực hiện, vui lòng liên hệ ngay với chúng tôi qua hotline 1900 6868."));
            return Json(new { success = true });
        }

        private async Task SignInUserAsync(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity));
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
