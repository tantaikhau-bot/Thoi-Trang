# MONO.WEAR — Tổng hợp dự án (bàn giao cho phiên chat mới)

> File này tóm tắt TOÀN BỘ dự án website thời trang MONO.WEAR để có thể tiếp tục làm việc ở đoạn chat mới mà không mất ngữ cảnh.

---

## 1. Tổng quan
- **Tên:** MONO.WEAR — website thương mại điện tử thời trang.
- **Công nghệ:** ASP.NET Core MVC (.NET 10) + Entity Framework Core + SQL Server.
- **Đường dẫn dự án:** `D:\ThoiTrang\ThoiTrang\ThoiTrang\`
- **Kiến trúc:** MVC. Hầu hết các trang là Razor View `Layout = null` (mỗi trang tự chứa HTML/CSS/JS đầy đủ). 1 controller chính: `Controllers/HomeController.cs`.

## 2. Database
- **Tên DB:** `MonoWear` trên SQL Server instance mặc định `localhost` (Windows Auth).
- **Chuỗi kết nối:** `appsettings.json` → `Server=localhost;Database=MonoWear;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True;`
- **⚠️ QUAN TRỌNG:** Khi chạy file .sql phải dùng cờ **`-f 65001`** để không lỗi font tiếng Việt:
  `sqlcmd -S localhost -E -C -I -f 65001 -i "file.sql"`

### Các bảng (Models/Entities/*.cs + AppDbContext)
- `Categories`, `Products`, `ProductImages`, `Colors`, `Sizes`, `ProductVariants`
- `Users` (có cột `LastSpinAt` cho vòng quay), `Addresses`, `PaymentMethods`
- `CartItems`, `Wishlists`, `Orders`, `OrderDetails`
- `Vouchers`, `UserVouchers` (voucher đã lưu, IsUsed), `Reviews`, `ProductQuestions`
- `Notifications`, `ChatMessages` (chat khách↔admin), `ChatKnowledges` (kho tri thức chatbot)
- `SiteSettings` (key/value: pay_*, ship_*, notify_*, chat_autoreply), `ScheduledEvents`
- Script DB nằm ở thư mục `Database/*.sql` (MonoWear.sql, SeedProducts.sql, UserVouchers.sql, ChatMessages.sql, PaymentMethods.sql, ChatKnowledge.sql...). Vài cột/bảng thêm sau bằng ALTER trực tiếp: `Users.LastSpinAt`, `SiteSettings` (ship_sameday), `ScheduledEvents`.

### Lưu ý EF
- ProductImage(ImageId), ProductVariant(VariantId), ProductQuestion(QuestionId) cần `[Key]` (tên khóa không theo quy ước).
- `OrderDetail.LineTotal` là computed column (cần QUOTED_IDENTIFIER ON).

## 3. Tài khoản mẫu
- **Admin:** `admin@monowear.vn` / `Admin@123` → trang `/Home/Admin`
- **Khách:** `khoi@example.com` / `Khach@123` → `/Home/TaiKhoan`
- Mật khẩu hash PBKDF2 (`IPasswordHasher<User>`). Program.cs seed lại hash nếu còn placeholder `HASH_ADMIN`/`HASH_USER`.
- Auth: Cookie Authentication. Phân quyền: `[Authorize]`, `[Authorize(Roles="Admin")]`.

## 4. Chức năng đã hoàn thành (KHÁCH)
- **Sản phẩm data-driven** từ DB: Index (Flash Sale + Bán chạy), Nam, Nu, MoiVe, Sale, Search — đều dùng partial `Views/Home/_ProductCard.cshtml`.
  - Thẻ SP: nút 👁 mắt → CTSP/{id}; nút ➕ → AddProductToCart; nút tim → ToggleWishlist. Badge ưu tiên hiện `-X%` khi giảm giá.
- **Lọc danh mục** (`wwwroot/js/filter.js`) trên Nam/Nu/MoiVe/Sale.
- **Tìm kiếm** (`wwwroot/js/search.js`): gợi ý realtime, lịch sử (localStorage), từ khóa phổ biến, không phân biệt dấu (Collate CI_AI). Endpoint Search/SearchSuggest/PopularSearches.
- **CTSP** chi tiết: màu/size từ biến thể thật + tồn kho; thêm giỏ theo variantId.
- **Yêu thích** (Wishlist DB), **Giỏ hàng** (CartItems DB theo user) — thêm/sửa SL/xóa/"Lưu để mua sau"→wishlist, "Xóa đã chọn" đều lưu DB.
- **Thanh toán** (Checkout): địa chỉ từ DB (đồng bộ với TaiKhoan), phương thức thanh toán + vận chuyển ẩn/hiện theo SiteSettings, voucher (chọn→áp dụng→đổi được), PlaceOrder lưu Order + xóa giỏ + tạo thông báo.
- **Tài khoản** (TaiKhoan): thông tin cá nhân (UpdateProfile lưu DB), đơn hàng thật, yêu thích thật, địa chỉ (Add/Delete/SetDefault), phương thức thanh toán (Add/Delete/SetDefault), voucher đã lưu, hạng thành viên + điểm (1 điểm/1.000đ; Bronze<1000, Silver≥1000, Gold≥3000, Platinum≥5000), badge số thật.
- **Thông báo** (ThongBao, cần login): hiện hoạt động thật (đặt hàng, lưu voucher, cập nhật đơn...); xem xong badge về 0.
- **Header badges** (chuông/giỏ/tim): cập nhật realtime qua endpoint `Counts` (trong `_ChatWidget`).
- **Sale**: kho voucher "Săn mã"→SaveUserVoucher (giảm Quantity khi lưu), vòng quay may mắn 1 lần/ngày (SpinWheel + Users.LastSpinAt), combo/deal "Mua combo"→AddComboToCart.
- **Voucher**: mỗi tài khoản lưu 1 lần/voucher; mỗi voucher dùng 1 lần (PlaceOrder đánh dấu IsUsed).
- **Đăng ký/đăng nhập mạng xã hội** (mô phỏng): nút Google/Facebook → SocialAuth (nhập email tạo/đăng nhập).
- **Header sticky**: đã đổi `overflow-x: hidden`→`overflow-x: clip` toàn bộ trang.

## 5. Chatbot (widget nổi góc phải dưới — `Views/Home/_ChatWidget.cshtml`)
- Hiển thị cho MỌI khách (kể cả chưa đăng nhập, trừ Admin). Khách chưa login vẫn chat được; **chốt đơn/mua cần đăng nhập**.
- **Dò kho tri thức** `ChatKnowledges`: chấm điểm theo độ dài keyword khớp (NoDia bỏ dấu), chọn câu trả lời điểm cao nhất.
- Xử lý động: theo dõi đơn, liệt kê voucher, lọc theo tầm giá ("áo dưới 500k"), chốt đơn (thêm giỏ).
- Có **nút gợi ý nhanh (chips)** kiểu Beta ĐMX. Trả về cả thẻ sản phẩm (ảnh/giá/link).
- Endpoints: `ChatBotReply`, `ChatConfig`, `SendMessage`/`GetMessages` (khách), `GetChatUsers`/`GetAdminMessages`/`SendAdminMessage` (admin). Bật/tắt qua SiteSettings `chat_autoreply`.

## 6. Chức năng ADMIN (`Views/Home/Admin.cshtml`)
- **Số liệu thật**: doanh thu, đơn, khách, sản phẩm; header + tab + sidebar badge + donut tính từ DB.
- **Tab lọc** (filterAdminTab): Sản phẩm (theo tồn kho: đang bán/sắp hết/hết/nháp), Đơn hàng (theo trạng thái), Khách hàng (theo hạng).
- **Tìm kiếm trong bảng** (searchAdminTable) cho cả 3 bảng.
- **Sản phẩm**: Thêm/Sửa/Xóa (AddProduct/EditProduct/DeleteProduct).
- **Đơn hàng**: dropdown đổi trạng thái (UpdateOrderStatus) → gửi thông báo cho khách.
- **Khách hàng**: xem chi tiết (modal đọc dữ liệu thật + đơn hàng thật qua GetCustomerOrders), Hạn chế/Mở khóa (ToggleUserActive), Xóa (DeleteCustomer — gỡ FK rồi xóa), Gửi email, Xuất CSV.
- **Voucher**: Tạo/Sửa/Xóa (SaveVoucher/EditVoucher/DeleteVoucher) + render từ DB.
- **Cài đặt** (SiteSettings, SaveSetting): bật/tắt phương thức **thanh toán** (cod/bank/momo/vnpay/paypal), **vận chuyển** (standard/express/sameday/store), **thông báo** (notify_order/promo/news → gate AddNotificationAsync), **chatbot** (chat_autoreply) → tất cả ảnh hưởng trang khách thật. Đã bỏ toast spam "Đang tải cài đặt...".
- **Chat**: panel chat admin nối DB thật (loadChatConvos poll 5s, trả lời khách).
- **Sự kiện (Lên lịch)**: CRUD lưu DB (GetEvents/AddEvent/UpdateEvent/DeleteEvent/ToggleEventStatus).
- Thông báo AI tự động (proactive bubble) vẫn giữ — KHÔNG xóa.

## 7. Cách chạy & test
- Build: `dotnet build "D:\ThoiTrang\ThoiTrang\ThoiTrang\ThoiTrang.csproj" -v q --nologo`
- Chạy: `dotnet run --project "...ThoiTrang.csproj" --urls "http://localhost:5199"` (hoặc F5 trong Visual Studio → https://localhost:7235).
- Khi test bằng PowerShell + Invoke-WebRequest: lấy `__RequestVerificationToken` từ trang DangNhap rồi POST Login (có antiforgery). JSON tiếng Việt qua PowerShell dễ lỗi encoding → test bằng ASCII hoặc trình duyệt thật.
- Trước khi build phải dừng tiến trình đang chạy: `Get-Process ThoiTrang | Stop-Process -Force`.

## 8. Còn hardcode / chưa làm (gợi ý làm tiếp)
- Trang BoSuuTap + các trang campaign (CampaignAutumn, LimitedEdition, RawMaterials, EditorialCapsule): nội dung tĩnh, chưa data-driven.
- Combo & "đồng giá" trang Sale: "Mua combo" thêm biến thể đại diện vào giỏ (chưa phải combo thật trong DB).
- OAuth Google/Facebook thật (hiện mô phỏng nhập email) — cần Client ID/Secret.
- Email thật (SMTP): hiện gửi dưới dạng Notification trong hệ thống (khách thấy ở mục Thông báo) thay vì email thật.
- Panel "Bộ sưu tập" Admin: editorial collections vẫn là 4 mục tĩnh (không có bảng Collections trong DB); nút Tạo/Sửa chỉ hiện toast. Đã bỏ số views/lượt mua giả.

## 9. Đã hoàn thiện gần đây (2026-06-09)
- **CTSP đánh giá data-driven**: Tab Reviews render từ bảng `Reviews` thật; thống kê sao động (trung bình, phân bố); lọc theo sao/đã mua; nút "Hữu ích" → `HelpfulReview`; form viết đánh giá (cho user đã mua + chưa đánh giá) → `AddReview` lưu DB + **upload ảnh** (cột `Reviews.Images`, lưu `wwwroot/uploads/reviews/`). Badge tab "Đánh giá/Hỏi đáp" đếm thật.
- **CTSP Q&A data-driven**: Tab Hỏi đáp render từ `ProductQuestions`; `AddQuestion` lưu DB (cần login).
- **CTSP sản phẩm liên quan data-driven** + chặn thêm giỏ khi chưa login (nút gọi `requireLogin`).
- **Chi tiết đơn hàng**: `GetOrderDetail(orderId)` trả JSON; modal trong TaiKhoan (click dòng đơn). Admin xem được mọi đơn.
- **Ảnh đại diện**: cột `Users.AvatarUrl`; endpoint `UpdateAvatar` (lưu `wwwroot/uploads/avatars/`, xóa ảnh cũ); 2 nút đổi (camera hero + card panel), preview tức thì.
- **5 khách hàng mẫu** (lan/hung/mai/tu/hoa@example.com, MK `Khach@123`) + 13 đơn + 17 đánh giá thật — script `Database/SeedCustomers.sql`.
- **Auth fix gốc (Program.cs)**: Cookie Auth trả **401** cho POST/AJAX khi chưa login (thay vì 302→200), nhờ đó nút thêm giỏ/wishlist... hiện đúng toast + redirect.
- **Đổi mật khẩu**: `ChangePassword` (verify MK cũ, hash MK mới) — wire form TaiKhoan→Bảo mật.
- **Quên mật khẩu**: `ForgotPassword` (xác thực email + SĐT đã đăng ký → đặt lại tại chỗ) — modal trong DangNhap. Chưa có email thật.
- **Admin: Đánh giá & Hỏi đáp** (section mới `reviews`, badge câu chưa trả lời): `GetAdminReviews`/`DeleteReview`, `GetAdminQuestions`/`AnswerQuestion`/`DeleteQuestion`. Trả lời Q&A → tạo Notification cho khách, hiện ngay trên CTSP.
- **Admin: Gửi email hàng loạt** `SendCustomerEmail(toAll)` → tạo Notification "promo" cho mọi khách (chưa SMTP).
- **Admin số liệu thật**: biểu đồ doanh thu 7 ngày + 12 tháng (`Revenue7Days`/`Revenue12Months` từ Orders), donut danh mục (`CategoryCounts`), panel Thống kê (doanh thu/đơn tháng này, AOV, % tăng trưởng so tháng trước), **phân trang client-side thật** (`setupPagination`, 10 dòng/trang) thay nút số trang cứng.
- **CTSP query**: thêm `.AsSplitQuery()` tránh Cartesian explosion (Reviews×Variants×Images).

### Rà soát đợt 2 (2026-06-09)
- **TaiKhoan thông báo data-driven**: thay 4 thông báo cứng bằng `ViewBag.Notifications` thật; nút "Đánh dấu đã đọc" → `MarkNotificationsRead` (endpoint mới). `ViewBag.UnreadNotifCount`.
- **TaiKhoan "Lịch sử điểm thưởng" data-driven**: thay 4 dòng cứng bằng điểm từ đơn thật (TotalAmount/1000 mỗi đơn không hủy).
- **Khách hủy đơn**: endpoint `CancelOrder(orderId)` — chỉ hủy đơn Pending/Confirmed của chính mình, tạo Notification. Nút "Hủy đơn hàng" trong modal chi tiết đơn (TaiKhoan) chỉ hiện khi status cho phép.
- **Admin gửi email cho 1 khách**: `sendSingleCustomerEmail` wire thật vào `SendCustomerEmail(userId)`; `emailCustomer(name,email,uid)` + `currentViewUid` lấy từ data-uid dòng bảng.
- *(Giữ tĩnh có chủ đích)*: catalog "đổi điểm lấy quà", toggle 2FA bảo mật (demo UI), help-contact cards (thông tin liên hệ), trang campaign/BoSuuTap (nội dung marketing).

### Email SMTP thật (2026-06-09)
- **Cấu hình** `appsettings.json` → section `Smtp` (Host=smtp.gmail.com, Port=587, User/Password=App Password Gmail, FromName, Enabled). ⚠️ App Password lưu ở đây — KHÔNG commit lên git public.
- **Service** `Services/EmailSender.cs`: `IEmailSender.SendAsync(to, subject, htmlBody)` dùng `System.Net.Mail.SmtpClient` (STARTTLS). `EmailTemplate.Wrap(title, text)` bọc HTML có thương hiệu. Đăng ký Scoped trong Program.cs, inject vào HomeController (`_email`).
- **Gửi email tự động tại**: Register (chào mừng), PlaceOrder (xác nhận đơn + chi tiết), UpdateOrderStatus (cập nhật trạng thái), ForgotPassword (xác nhận đổi MK), SendCustomerEmail (admin gửi 1 khách / toàn bộ). Tất cả vẫn tạo Notification song song.
- Nếu `Smtp:Enabled=false` hoặc thiếu Password → tự bỏ qua gửi (chỉ tạo Notification), không lỗi.

## 9. Quy ước khi sửa code
- Mỗi View là HTML đầy đủ (Layout=null). CSS dùng `@@` cho ký tự `@` trong Razor.
- Thẻ sản phẩm dùng chung `_ProductCard.cshtml`; trang nào render thì cần CSS `.products .product...` (đã thêm override vào Index/Sale/MoiVe; Nam/Nu có sẵn).
- Widget chat + badge: partial `_ChatWidget.cshtml`, nhúng `@await Html.PartialAsync("_ChatWidget")` trước `</body>` các trang khách.
- Helper trong HomeController: `CurrentUserId()`, `GetSettingAsync(key)`, `AddNotificationAsync(...)`, `NoDia(s)` (bỏ dấu), `ToSlug(s)`, `ToCard(product)`.
