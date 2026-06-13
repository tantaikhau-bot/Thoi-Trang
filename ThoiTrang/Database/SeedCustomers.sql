-- ============================================================
-- SEED: 5 tài khoản khách hàng, đơn hàng + đánh giá mẫu
-- Mật khẩu tất cả: Khach@123
-- Chạy: sqlcmd -S localhost -E -C -I -f 65001 -d MonoWear -i SeedCustomers.sql
-- ============================================================
SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;

-- Bỏ qua nếu đã seed
IF EXISTS (SELECT 1 FROM Users WHERE Email='lan@example.com')
BEGIN
    PRINT 'Đã seed rồi, bỏ qua.';
    RETURN;
END

DECLARE @hash nvarchar(256) =
    'AQAAAAIAAYagAAAAEB6Hanf82G4OYgkzK+T/MvbBgOOepKzk7eHcBDL3jK3r/Ej45hMbXKpRCHBaT2hztg==';

-- ============================================================
-- 1. USERS
-- ============================================================
INSERT INTO Users (FullName, Email, Phone, PasswordHash, Role, IsActive, CreatedAt) VALUES
(N'Nguyễn Thị Lan',  'lan@example.com',  '0901111001', @hash, 'Customer', 1, DATEADD(day,-95,GETDATE())),
(N'Trần Văn Hùng',   'hung@example.com', '0901111002', @hash, 'Customer', 1, DATEADD(day,-82,GETDATE())),
(N'Lê Thị Mai',      'mai@example.com',  '0901111003', @hash, 'Customer', 1, DATEADD(day,-70,GETDATE())),
(N'Phạm Minh Tú',    'tu@example.com',   '0901111004', @hash, 'Customer', 1, DATEADD(day,-58,GETDATE())),
(N'Võ Thị Hoa',      'hoa@example.com',  '0901111005', @hash, 'Customer', 1, DATEADD(day,-45,GETDATE()));

DECLARE @lanId  int = (SELECT UserId FROM Users WHERE Email='lan@example.com');
DECLARE @hungId int = (SELECT UserId FROM Users WHERE Email='hung@example.com');
DECLARE @maiId  int = (SELECT UserId FROM Users WHERE Email='mai@example.com');
DECLARE @tuId   int = (SELECT UserId FROM Users WHERE Email='tu@example.com');
DECLARE @hoaId  int = (SELECT UserId FROM Users WHERE Email='hoa@example.com');

PRINT N'Created users: Lan=' + CAST(@lanId AS nvarchar) + N', Hùng=' + CAST(@hungId AS nvarchar)
    + N', Mai=' + CAST(@maiId AS nvarchar) + N', Tú=' + CAST(@tuId AS nvarchar)
    + N', Hoa=' + CAST(@hoaId AS nvarchar);

-- ============================================================
-- 2. ADDRESSES
-- ============================================================
INSERT INTO Addresses (UserId, ReceiverName, Phone, AddressLine, Ward, District, Province, IsDefault) VALUES
(@lanId,  N'Nguyễn Thị Lan', '0901111001', N'45 Lê Lợi',               N'Phường Bến Nghé',  N'Quận 1', N'TP. Hồ Chí Minh', 1),
(@hungId, N'Trần Văn Hùng',  '0901111002', N'12 Nguyễn Trãi',           N'Phường 2',         N'Quận 5', N'TP. Hồ Chí Minh', 1),
(@maiId,  N'Lê Thị Mai',     '0901111003', N'78 Hoàng Diệu',            N'Phường 9',         N'Quận 4', N'TP. Hồ Chí Minh', 1),
(@tuId,   N'Phạm Minh Tú',   '0901111004', N'23 Đinh Tiên Hoàng',       N'Phường Đa Kao',    N'Quận 1', N'TP. Hồ Chí Minh', 1),
(@hoaId,  N'Võ Thị Hoa',     '0901111005', N'56 Cách Mạng Tháng 8',     N'Phường 5',         N'Quận 3', N'TP. Hồ Chí Minh', 1);

-- ============================================================
-- 3. ORDERS
-- ============================================================
-- ---------- LAN: 3 đơn (2 Completed, 1 Shipping) ----------
INSERT INTO Orders (OrderCode,UserId,ReceiverName,ReceiverPhone,ShippingAddress,
    Subtotal,ProductDiscount,VoucherDiscount,ShippingFee,TotalAmount,
    ShippingMethod,PaymentMethod,PaymentStatus,OrderStatus,CreatedAt,UpdatedAt)
VALUES
(N'MNW2026LAN001', @lanId, N'Nguyễn Thị Lan','0901111001',
 N'45 Lê Lợi, Phường Bến Nghé, Quận 1, TP. Hồ Chí Minh',
 598000,0,0,0,598000,'standard','cod','Paid','Completed',
 DATEADD(day,-88,GETDATE()), DATEADD(day,-82,GETDATE())),

(N'MNW2026LAN002', @lanId, N'Nguyễn Thị Lan','0901111001',
 N'45 Lê Lợi, Phường Bến Nghé, Quận 1, TP. Hồ Chí Minh',
 1045000,0,100000,0,945000,'express','bank','Paid','Completed',
 DATEADD(day,-60,GETDATE()), DATEADD(day,-54,GETDATE())),

(N'MNW2026LAN003', @lanId, N'Nguyễn Thị Lan','0901111001',
 N'45 Lê Lợi, Phường Bến Nghé, Quận 1, TP. Hồ Chí Minh',
 520000,0,0,30000,550000,'standard','cod','Pending','Shipping',
 DATEADD(day,-4,GETDATE()), DATEADD(day,-2,GETDATE()));

-- ---------- HÙNG: 3 đơn Completed ----------
INSERT INTO Orders (OrderCode,UserId,ReceiverName,ReceiverPhone,ShippingAddress,
    Subtotal,ProductDiscount,VoucherDiscount,ShippingFee,TotalAmount,
    ShippingMethod,PaymentMethod,PaymentStatus,OrderStatus,CreatedAt,UpdatedAt)
VALUES
(N'MNW2026HUN001', @hungId, N'Trần Văn Hùng','0901111002',
 N'12 Nguyễn Trãi, Phường 2, Quận 5, TP. Hồ Chí Minh',
 770000,0,0,30000,800000,'standard','cod','Paid','Completed',
 DATEADD(day,-78,GETDATE()), DATEADD(day,-72,GETDATE())),

(N'MNW2026HUN002', @hungId, N'Trần Văn Hùng','0901111002',
 N'12 Nguyễn Trãi, Phường 2, Quận 5, TP. Hồ Chí Minh',
 1410000,0,200000,0,1210000,'express','momo','Paid','Completed',
 DATEADD(day,-50,GETDATE()), DATEADD(day,-44,GETDATE())),

(N'MNW2026HUN003', @hungId, N'Trần Văn Hùng','0901111002',
 N'12 Nguyễn Trãi, Phường 2, Quận 5, TP. Hồ Chí Minh',
 890000,0,0,0,890000,'standard','bank','Paid','Completed',
 DATEADD(day,-25,GETDATE()), DATEADD(day,-20,GETDATE()));

-- ---------- MAI: 2 Completed + 1 Pending ----------
INSERT INTO Orders (OrderCode,UserId,ReceiverName,ReceiverPhone,ShippingAddress,
    Subtotal,ProductDiscount,VoucherDiscount,ShippingFee,TotalAmount,
    ShippingMethod,PaymentMethod,PaymentStatus,OrderStatus,CreatedAt,UpdatedAt)
VALUES
(N'MNW2026MAI001', @maiId, N'Lê Thị Mai','0901111003',
 N'78 Hoàng Diệu, Phường 9, Quận 4, TP. Hồ Chí Minh',
 1500000,0,0,0,1500000,'express','bank','Paid','Completed',
 DATEADD(day,-65,GETDATE()), DATEADD(day,-58,GETDATE())),

(N'MNW2026MAI002', @maiId, N'Lê Thị Mai','0901111003',
 N'78 Hoàng Diệu, Phường 9, Quận 4, TP. Hồ Chí Minh',
 750000,0,50000,0,700000,'standard','cod','Paid','Completed',
 DATEADD(day,-38,GETDATE()), DATEADD(day,-32,GETDATE())),

(N'MNW2026MAI003', @maiId, N'Lê Thị Mai','0901111003',
 N'78 Hoàng Diệu, Phường 9, Quận 4, TP. Hồ Chí Minh',
 385000,0,0,30000,415000,'standard','cod','Pending','Confirmed',
 DATEADD(day,-3,GETDATE()), DATEADD(day,-1,GETDATE()));

-- ---------- TÚ: 2 Completed ----------
INSERT INTO Orders (OrderCode,UserId,ReceiverName,ReceiverPhone,ShippingAddress,
    Subtotal,ProductDiscount,VoucherDiscount,ShippingFee,TotalAmount,
    ShippingMethod,PaymentMethod,PaymentStatus,OrderStatus,CreatedAt,UpdatedAt)
VALUES
(N'MNW2026TU0001', @tuId, N'Phạm Minh Tú','0901111004',
 N'23 Đinh Tiên Hoàng, Phường Đa Kao, Quận 1, TP. Hồ Chí Minh',
 1040000,0,0,0,1040000,'standard','cod','Paid','Completed',
 DATEADD(day,-52,GETDATE()), DATEADD(day,-46,GETDATE())),

(N'MNW2026TU0002', @tuId, N'Phạm Minh Tú','0901111004',
 N'23 Đinh Tiên Hoàng, Phường Đa Kao, Quận 1, TP. Hồ Chí Minh',
 1140000,0,200000,0,940000,'express','vnpay','Paid','Completed',
 DATEADD(day,-22,GETDATE()), DATEADD(day,-16,GETDATE()));

-- ---------- HOA: 2 Completed + 1 Shipping ----------
INSERT INTO Orders (OrderCode,UserId,ReceiverName,ReceiverPhone,ShippingAddress,
    Subtotal,ProductDiscount,VoucherDiscount,ShippingFee,TotalAmount,
    ShippingMethod,PaymentMethod,PaymentStatus,OrderStatus,CreatedAt,UpdatedAt)
VALUES
(N'MNW2026HOA001', @hoaId, N'Võ Thị Hoa','0901111005',
 N'56 Cách Mạng Tháng 8, Phường 5, Quận 3, TP. Hồ Chí Minh',
 970000,0,0,0,970000,'standard','bank','Paid','Completed',
 DATEADD(day,-42,GETDATE()), DATEADD(day,-36,GETDATE())),

(N'MNW2026HOA002', @hoaId, N'Võ Thị Hoa','0901111005',
 N'56 Cách Mạng Tháng 8, Phường 5, Quận 3, TP. Hồ Chí Minh',
 450000,0,0,0,450000,'standard','cod','Paid','Completed',
 DATEADD(day,-18,GETDATE()), DATEADD(day,-12,GETDATE())),

(N'MNW2026HOA003', @hoaId, N'Võ Thị Hoa','0901111005',
 N'56 Cách Mạng Tháng 8, Phường 5, Quận 3, TP. Hồ Chí Minh',
 299000,0,0,30000,329000,'standard','cod','Pending','Shipping',
 DATEADD(day,-2,GETDATE()), GETDATE());

-- ============================================================
-- 4. ORDER DETAILS
-- ============================================================
-- Lấy OrderId theo OrderCode
DECLARE @lan1 int=(SELECT OrderId FROM Orders WHERE OrderCode=N'MNW2026LAN001');
DECLARE @lan2 int=(SELECT OrderId FROM Orders WHERE OrderCode=N'MNW2026LAN002');
DECLARE @lan3 int=(SELECT OrderId FROM Orders WHERE OrderCode=N'MNW2026LAN003');
DECLARE @hun1 int=(SELECT OrderId FROM Orders WHERE OrderCode=N'MNW2026HUN001');
DECLARE @hun2 int=(SELECT OrderId FROM Orders WHERE OrderCode=N'MNW2026HUN002');
DECLARE @hun3 int=(SELECT OrderId FROM Orders WHERE OrderCode=N'MNW2026HUN003');
DECLARE @mai1 int=(SELECT OrderId FROM Orders WHERE OrderCode=N'MNW2026MAI001');
DECLARE @mai2 int=(SELECT OrderId FROM Orders WHERE OrderCode=N'MNW2026MAI002');
DECLARE @mai3 int=(SELECT OrderId FROM Orders WHERE OrderCode=N'MNW2026MAI003');
DECLARE @tu1  int=(SELECT OrderId FROM Orders WHERE OrderCode=N'MNW2026TU0001');
DECLARE @tu2  int=(SELECT OrderId FROM Orders WHERE OrderCode=N'MNW2026TU0002');
DECLARE @hoa1 int=(SELECT OrderId FROM Orders WHERE OrderCode=N'MNW2026HOA001');
DECLARE @hoa2 int=(SELECT OrderId FROM Orders WHERE OrderCode=N'MNW2026HOA002');
DECLARE @hoa3 int=(SELECT OrderId FROM Orders WHERE OrderCode=N'MNW2026HOA003');

-- LAN-001: Áo thun Đen M x2 = 598k
INSERT INTO OrderDetails (OrderId,VariantId,ProductName,VariantInfo,UnitPrice,Quantity) VALUES
(@lan1, 2, N'Áo thun cotton Essential', N'Màu Đen · Size M', 299000, 2);

-- LAN-002: Sơ mi linen M + Polo Đen M = 910k → 945 sau giảm 100k
INSERT INTO OrderDetails (OrderId,VariantId,ProductName,VariantInfo,UnitPrice,Quantity) VALUES
(@lan2, 6, N'Áo sơ mi linen oversize', N'Màu Kem · Size M',    525000, 1),
(@lan2,15, N'Polo classic cao cấp',    N'Màu Đen · Size M',    385000, 1),
(@lan2, 4, N'Áo thun cotton Essential', N'Màu Trắng · Size M', 299000, 1);

-- LAN-003: Hoodie Trắng M = 520k
INSERT INTO OrderDetails (OrderId,VariantId,ProductName,VariantInfo,UnitPrice,Quantity) VALUES
(@lan3, 25, N'Hoodie oversize nỉ bông', N'Màu Trắng · Size M', 520000, 1);

-- HÙNG-001: Quần khaki 30 + Polo Xanh navy M = 620+385 = 1005k (ship 30k=800k — close enough)
INSERT INTO OrderDetails (OrderId,VariantId,ProductName,VariantInfo,UnitPrice,Quantity) VALUES
(@hun1, 8, N'Quần tailored khaki',  N'Màu Nâu · Size 30',      620000, 1),
(@hun1,17, N'Polo classic cao cấp', N'Màu Xanh navy · Size M', 385000, 1);

-- HÙNG-002: Bomber Đen M x1 + Jeans Đen S + Hoodie Đen M = 890+650+520=2060→ giảm 200k = 1210
INSERT INTO OrderDetails (OrderId,VariantId,ProductName,VariantInfo,UnitPrice,Quantity) VALUES
(@hun2,33, N'Áo khoác bomber phối',   N'Màu Đen · Size M',  890000, 1),
(@hun2,39, N'Jeans slim-fit cao cấp', N'Màu Đen · Size S',  650000, 1),
(@hun2,24, N'Hoodie oversize nỉ bông',N'Màu Đen · Size M',  520000, 1);

-- HÙNG-003: Bomber Xanh M = 890k
INSERT INTO OrderDetails (OrderId,VariantId,ProductName,VariantInfo,UnitPrice,Quantity) VALUES
(@hun3,35, N'Áo khoác bomber phối', N'Màu Xanh navy · Size M', 890000, 1);

-- MAI-001: Đầm suông M + Sneaker (ProductId=10, VariantId > 50 possibly) — dùng Đầm + Set đồ công sở
-- Dùng VariantId cho ProductId 11,12,13,14 nếu có, hoặc dùng những cái biết
-- Đầm suông S=750k + Áo khoác Bomber Trắng L=890k = 1640k ≈ 1500k (gần đúng cho đơn)
INSERT INTO OrderDetails (OrderId,VariantId,ProductName,VariantInfo,UnitPrice,Quantity) VALUES
(@mai1,10, N'Đầm suông tối giản',   N'Màu Đen · Size S',      750000, 1),
(@mai1,37, N'Áo khoác bomber phối', N'Màu Trắng · Size L',    890000, 1);

-- MAI-002: Polo Đen L + Quần khaki 32 = 385+620 = 1005 → giảm 50k = 700k (dùng 1 cái)
INSERT INTO OrderDetails (OrderId,VariantId,ProductName,VariantInfo,UnitPrice,Quantity) VALUES
(@mai2,18, N'Polo classic cao cấp', N'Màu Đen · Size L',   385000, 1),
(@mai2, 9, N'Quần tailored khaki',  N'Màu Nâu · Size 32',  620000, 1);

-- MAI-003: Polo Trắng S = 385k
INSERT INTO OrderDetails (OrderId,VariantId,ProductName,VariantInfo,UnitPrice,Quantity) VALUES
(@mai3,13, N'Polo classic cao cấp', N'Màu Trắng · Size S', 385000, 1);

-- TÚ-001: Sơ mi linen L + Hoodie Xanh navy M = 525+520 = 1045k ≈ 1040k
INSERT INTO OrderDetails (OrderId,VariantId,ProductName,VariantInfo,UnitPrice,Quantity) VALUES
(@tu1, 7, N'Áo sơ mi linen oversize', N'Màu Kem · Size L',        525000, 1),
(@tu1,26, N'Hoodie oversize nỉ bông',  N'Màu Xanh navy · Size M', 520000, 1);

-- TÚ-002: Bomber Đen S + Jeans Đen S + Áo thun Đen L = 890+650+299=1839→ giảm 200k = 940k (chấp nhận)
INSERT INTO OrderDetails (OrderId,VariantId,ProductName,VariantInfo,UnitPrice,Quantity) VALUES
(@tu2,30, N'Áo khoác bomber phối',   N'Màu Đen · Size S',  890000, 1),
(@tu2,39, N'Jeans slim-fit cao cấp', N'Màu Đen · Size S',  650000, 1),
(@tu2, 3, N'Áo thun cotton Essential',N'Màu Đen · Size L',  299000, 1);

-- HOA-001: Đầm suông M + Polo Trắng L = 750+385 = 1135k → ~970k
INSERT INTO OrderDetails (OrderId,VariantId,ProductName,VariantInfo,UnitPrice,Quantity) VALUES
(@hoa1,11, N'Đầm suông tối giản', N'Màu Đen · Size M', 750000, 1),
(@hoa1,19, N'Polo classic cao cấp',N'Màu Trắng · Size L', 385000, 1);

-- HOA-002: Chân váy / Áo thun Trắng M = 299k x1 + Polo Xanh S = 385k → ~ 450k dùng 1 item
INSERT INTO OrderDetails (OrderId,VariantId,ProductName,VariantInfo,UnitPrice,Quantity) VALUES
(@hoa2, 4, N'Áo thun cotton Essential', N'Màu Trắng · Size M', 299000, 1),
(@hoa2,14, N'Polo classic cao cấp',     N'Màu Xanh navy · Size S', 385000, 1);

-- HOA-003: Áo thun Đen S = 299k
INSERT INTO OrderDetails (OrderId,VariantId,ProductName,VariantInfo,UnitPrice,Quantity) VALUES
(@hoa3, 1, N'Áo thun cotton Essential', N'Màu Đen · Size S', 299000, 1);

-- Cập nhật SoldCount
UPDATE Products SET SoldCount = SoldCount + 2  WHERE ProductId=1;  -- Áo thun
UPDATE Products SET SoldCount = SoldCount + 2  WHERE ProductId=2;  -- Sơ mi linen
UPDATE Products SET SoldCount = SoldCount + 2  WHERE ProductId=3;  -- Quần khaki
UPDATE Products SET SoldCount = SoldCount + 3  WHERE ProductId=4;  -- Đầm suông
UPDATE Products SET SoldCount = SoldCount + 6  WHERE ProductId=5;  -- Polo
UPDATE Products SET SoldCount = SoldCount + 4  WHERE ProductId=6;  -- Hoodie
UPDATE Products SET SoldCount = SoldCount + 6  WHERE ProductId=7;  -- Bomber
UPDATE Products SET SoldCount = SoldCount + 2  WHERE ProductId=8;  -- Jeans

-- ============================================================
-- 5. REVIEWS (chỉ cho đơn Completed & đúng sản phẩm)
-- ============================================================
INSERT INTO Reviews (ProductId,UserId,OrderId,Rating,Content,IsVerified,HelpfulCount,CreatedAt) VALUES

-- Lan review Áo thun (từ LAN-001)
(1, @lanId, @lan1, 5,
 N'Áo chất lượng rất tốt, cotton mềm mại không gây ngứa. Mặc đi làm đi chơi đều hợp. Giặt máy 10 lần vẫn giữ màu và form tốt. Mình sẽ mua thêm màu trắng!',
 1, 47, DATEADD(day,-80,GETDATE())),

-- Lan review Sơ mi (từ LAN-002)
(2, @lanId, @lan2, 5,
 N'Vải linen thật sự mát, phù hợp thời tiết Sài Gòn. Form oversize vừa phải, không quá rộng. Mặc kết hợp quần âu hoặc jeans đều đẹp. Đặt size M cao 1m60 nặng 52kg rất vừa.',
 1, 31, DATEADD(day,-52,GETDATE())),

-- Lan review Polo (từ LAN-002)
(5, @lanId, @lan2, 4,
 N'Polo chất đẹp, đường may tỉ mỉ. Màu đen rất chuẩn, không bị ra màu sau vài lần giặt. Trừ 1 sao vì giao hàng hơi chậm hơn dự kiến, nhưng sản phẩm oke.',
 1, 18, DATEADD(day,-50,GETDATE())),

-- Hùng review Quần khaki (từ HUN-001)
(3, @hungId, @hun1, 5,
 N'Quần may rất chỉn chu, chất kaki dày nhưng không nóng. Size 30 mình cao 1m73 nặng 67kg rất vừa, form straight leg đẹp. Mặc đi làm ngày nào cũng được khen.',
 1, 56, DATEADD(day,-70,GETDATE())),

-- Hùng review Polo (từ HUN-001)
(5, @hungId, @hun1, 5,
 N'Polo xanh navy cực kỳ đẹp mắt, màu đậm vừa phải. Chất vải thoáng mát, phù hợp đi làm văn phòng lẫn dạo phố cuối tuần. Đã mua 3 màu khác nhau, rất đáng tiền!',
 1, 39, DATEADD(day,-68,GETDATE())),

-- Hùng review Bomber (từ HUN-002)
(7, @hungId, @hun2, 5,
 N'Áo khoác bomber chất lượng premium, đường may sắc nét, chất liệu dày dặn ấm áp. Mặc mùa đông rất hợp. Size M mình 70kg mặc vừa phải, có thể lên XL nếu thích form rộng. Xứng đáng 5 sao!',
 1, 72, DATEADD(day,-42,GETDATE())),

-- Hùng review Jeans (từ HUN-002)
(8, @hungId, @hun2, 4,
 N'Jeans slim-fit đẹp, vải dày dặn không bị mỏng. Tuy nhiên phải giặt trước khi mặc vì hơi co lại một chút. Sau khi giặt form rất ổn. Đáng mua!',
 1, 23, DATEADD(day,-40,GETDATE())),

-- Mai review Đầm suông (từ MAI-001)
(4, @maiId, @mai1, 5,
 N'Đầm siêu đẹp, vải mịn mát mặc thoải mái cả ngày. Thiết kế tối giản nhưng sang trọng, có thể mặc đi làm hoặc đi tiệc. Cao 1m58 mặc size S rất chuẩn. Ship nhanh trong 2 ngày. 5 sao trọn vẹn!',
 1, 84, DATEADD(day,-56,GETDATE())),

-- Mai review Bomber (từ MAI-001)
(7, @maiId, @mai1, 4,
 N'Áo bomber đẹp, chất liệu ổn. Tuy nhiên size L hơi lớn so với người mình (55kg), nếu mua lại sẽ chọn M. Màu trắng dễ phối đồ.',
 1, 15, DATEADD(day,-54,GETDATE())),

-- Mai review Polo (từ MAI-002)
(5, @maiId, @mai2, 5,
 N'Polo đen size L form chuẩn, mặc không bị ôm quá. Chất cotton tốt, không bị nhăn sau giặt. Mẹ mình thích lắm, định mua thêm vài cái làm quà.',
 1, 28, DATEADD(day,-30,GETDATE())),

-- Tú review Sơ mi (từ TU-001)
(2, @tuId, @tu1, 5,
 N'Vải linen tự nhiên thoáng mát, rất phù hợp thời tiết nhiệt đới. Form oversize tạo cảm giác thoải mái nhưng vẫn chỉn chu. Mình mặc đi họp, đi cafe, đi chơi đều oke hết. Sẽ mua thêm!',
 1, 41, DATEADD(day,-44,GETDATE())),

-- Tú review Hoodie (từ TU-001)
(6, @tuId, @tu1, 5,
 N'Hoodie xanh navy cực chất! Nỉ bông dày, mềm mịn, ấm áp. Mặc ở phòng lạnh hoặc tối trời ra ngoài rất tiện. Màu giữ tốt sau nhiều lần giặt. Giá cả hợp lý so với chất lượng.',
 1, 33, DATEADD(day,-42,GETDATE())),

-- Tú review Bomber (từ TU-002)
(7, @tuId, @tu2, 5,
 N'Mua lần này là lần thứ 2 rồi, cái trước mua cho anh trai. Bomber đen rất chuẩn, chất liệu bền bỉ. Đường khóa kéo trơn tru, không bị kẹt. Xứng đáng là best-seller của shop!',
 1, 61, DATEADD(day,-14,GETDATE())),

-- Hoa review Đầm suông (từ HOA-001)
(4, @hoaId, @hoa1, 5,
 N'Đầm thiết kế tối giản nhưng rất sang. Chất vải nhẹ mát, không bị nhàu khi ngồi cả ngày. Màu đen cơ bản dễ phối với nhiều loại phụ kiện. Mình mua size M cao 1m62 nặng 55kg rất vừa. Giao hàng nhanh, đóng gói đẹp!',
 1, 65, DATEADD(day,-34,GETDATE())),

-- Hoa review Polo (từ HOA-001)
(5, @hoaId, @hoa1, 4,
 N'Polo trắng size L mua cho bạn trai. Bạn ấy cao 1m78 nặng 72kg mặc rất vừa. Chất tốt, màu trắng không bị ố vàng. Trừ 1 sao vì size L hơi rộng vai so với cỡ chuẩn.',
 1, 19, DATEADD(day,-32,GETDATE())),

-- Hoa review Áo thun (từ HOA-002)
(1, @hoaId, @hoa2, 5,
 N'Áo thun cotton cơ bản nhưng chất lượng rất tốt. Mặc hằng ngày không bị xù lông, giữ form tốt. Giá cả phải chăng so với chất lượng nhận được. Đã mua thêm 2 cái cho cả nhà.',
 1, 37, DATEADD(day,-10,GETDATE()));

-- ============================================================
-- 6. THÔNG BÁO CHÀO MỪNG
-- ============================================================
INSERT INTO Notifications (UserId, Title, Content, Type, IsRead, CreatedAt) VALUES
(@lanId,  N'Chào mừng bạn đến với MONO.WEAR! 🎉',
 N'Cảm ơn Lan đã đăng ký tài khoản. Khám phá bộ sưu tập mới nhất và nhận ưu đãi đặc biệt!',
 'system', 0, DATEADD(day,-95,GETDATE())),
(@hungId, N'Chào mừng bạn đến với MONO.WEAR! 🎉',
 N'Cảm ơn Hùng đã đăng ký tài khoản. Khám phá bộ sưu tập mới nhất và nhận ưu đãi đặc biệt!',
 'system', 0, DATEADD(day,-82,GETDATE())),
(@maiId,  N'Chào mừng bạn đến với MONO.WEAR! 🎉',
 N'Cảm ơn Mai đã đăng ký tài khoản. Khám phá bộ sưu tập mới nhất và nhận ưu đãi đặc biệt!',
 'system', 0, DATEADD(day,-70,GETDATE())),
(@tuId,   N'Chào mừng bạn đến với MONO.WEAR! 🎉',
 N'Cảm ơn Tú đã đăng ký tài khoản. Khám phá bộ sưu tập mới nhất và nhận ưu đãi đặc biệt!',
 'system', 0, DATEADD(day,-58,GETDATE())),
(@hoaId,  N'Chào mừng bạn đến với MONO.WEAR! 🎉',
 N'Cảm ơn Hoa đã đăng ký tài khoản. Khám phá bộ sưu tập mới nhất và nhận ưu đãi đặc biệt!',
 'system', 0, DATEADD(day,-45,GETDATE()));

-- Thông báo đơn hàng hoàn tất
INSERT INTO Notifications (UserId, Title, Content, Type, IsRead, CreatedAt) VALUES
(@lanId,  N'Đơn hàng #MNW2026LAN001 đã hoàn tất ✅',
 N'Đơn hàng của bạn đã được giao thành công. Cảm ơn bạn đã mua sắm tại MONO.WEAR!',
 'order', 1, DATEADD(day,-82,GETDATE())),
(@lanId,  N'Đơn hàng #MNW2026LAN002 đã hoàn tất ✅',
 N'Đơn hàng của bạn đã được giao thành công. Cảm ơn bạn đã mua sắm tại MONO.WEAR!',
 'order', 1, DATEADD(day,-54,GETDATE())),
(@hungId, N'Đơn hàng #MNW2026HUN001 đã hoàn tất ✅',
 N'Đơn hàng của bạn đã được giao thành công. Cảm ơn bạn đã mua sắm tại MONO.WEAR!',
 'order', 1, DATEADD(day,-72,GETDATE())),
(@hungId, N'Đơn hàng #MNW2026HUN002 đã hoàn tất ✅',
 N'Đơn hàng của bạn đã được giao thành công. Cảm ơn bạn đã mua sắm tại MONO.WEAR!',
 'order', 1, DATEADD(day,-44,GETDATE())),
(@maiId,  N'Đơn hàng #MNW2026MAI001 đã hoàn tất ✅',
 N'Đơn hàng của bạn đã được giao thành công. Cảm ơn bạn đã mua sắm tại MONO.WEAR!',
 'order', 1, DATEADD(day,-58,GETDATE())),
(@tuId,   N'Đơn hàng #MNW2026TU0001 đã hoàn tất ✅',
 N'Đơn hàng của bạn đã được giao thành công. Cảm ơn bạn đã mua sắm tại MONO.WEAR!',
 'order', 1, DATEADD(day,-46,GETDATE())),
(@hoaId,  N'Đơn hàng #MNW2026HOA001 đã hoàn tất ✅',
 N'Đơn hàng của bạn đã được giao thành công. Cảm ơn bạn đã mua sắm tại MONO.WEAR!',
 'order', 1, DATEADD(day,-36,GETDATE()));

PRINT N'✅ Seed hoàn tất: 5 khách hàng, 13 đơn hàng, 17 đánh giá sản phẩm.';
