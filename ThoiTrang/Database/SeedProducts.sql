/* =====================================================================
   Bổ sung dữ liệu sản phẩm cho MONO.WEAR (chạy sau MonoWear.sql)
   An toàn chạy lại: chỉ thêm sản phẩm chưa tồn tại (theo Slug)
   ===================================================================== */
USE MonoWear;
GO
SET NOCOUNT ON;

/* Hàm tiện: thêm sản phẩm nếu chưa có */
DECLARE @nam INT = (SELECT CategoryId FROM Categories WHERE Slug='nam');
DECLARE @nu  INT = (SELECT CategoryId FROM Categories WHERE Slug='nu');

/* Danh sách sản phẩm bổ sung (Nam + Nữ) */
DECLARE @P TABLE (Slug VARCHAR(220), Cat INT, Name NVARCHAR(200), Price DECIMAL(18,0),
                  OldPrice DECIMAL(18,0) NULL, IsNew BIT, IsSale BIT, IsFeatured BIT,
                  Rating DECIMAL(3,2), RatingCount INT, Sold INT, ShortDesc NVARCHAR(200));

INSERT INTO @P VALUES
 ('polo-classic-cao-cap', @nam, N'Polo classic cao cấp', 385000, NULL, 0,0,1, 4.9, 92, 320, N'Áo polo bo cổ thanh lịch'),
 ('hoodie-oversize-ni-bong', @nam, N'Hoodie oversize nỉ bông', 520000, NULL, 0,0,1, 4.8, 75, 260, N'Hoodie nỉ bông ấm áp'),
 ('ao-khoac-bomber-phoi', @nam, N'Áo khoác bomber phối', 890000, 1150000, 1,1,0, 4.7, 48, 130, N'Bomber phối màu cá tính'),
 ('jeans-slim-fit-cao-cap', @nam, N'Jeans slim-fit cao cấp', 650000, NULL, 0,0,0, 4.6, 61, 180, N'Quần jeans co giãn'),
 ('quan-kaki-ong-suong', @nam, N'Quần kaki ống suông', 480000, 600000, 0,1,0, 4.5, 40, 95, N'Kaki ống suông trẻ trung'),
 ('sneakers-da-minimal', @nam, N'Sneakers da minimal', 1250000, NULL, 1,0,1, 4.9, 110, 410, N'Giày sneakers da tối giản'),
 ('dam-linen-thanh-lich', @nu, N'Đầm linen thanh lịch', 820000, 990000, 1,1,1, 4.8, 67, 150, N'Đầm linen nhẹ nhàng'),
 ('chan-vay-but-chi', @nu, N'Chân váy bút chì', 450000, NULL, 0,0,0, 4.6, 52, 120, N'Chân váy ôm thanh lịch'),
 ('ao-blouse-lua', @nu, N'Áo blouse lụa mềm', 520000, 650000, 0,1,0, 4.7, 58, 140, N'Blouse lụa sang trọng'),
 ('set-do-cong-so', @nu, N'Set đồ công sở', 1100000, NULL, 1,0,1, 4.9, 44, 88, N'Set vest công sở thanh lịch');

INSERT INTO Products (CategoryId, Name, Slug, Price, OldPrice, IsNew, IsSale, IsFeatured, RatingAvg, RatingCount, SoldCount, ShortDesc, Material, Origin)
SELECT p.Cat, p.Name, p.Slug, p.Price, p.OldPrice, p.IsNew, p.IsSale, p.IsFeatured, p.Rating, p.RatingCount, p.Sold, p.ShortDesc, N'Cao cấp', N'Việt Nam'
FROM @P p
WHERE NOT EXISTS (SELECT 1 FROM Products x WHERE x.Slug = p.Slug);

/* Thêm ảnh chính + vài biến thể cho các sản phẩm mới chưa có biến thể */
DECLARE @blackS INT=(SELECT SizeId FROM Sizes WHERE Name='S'), @M INT=(SELECT SizeId FROM Sizes WHERE Name='M'),
        @L INT=(SELECT SizeId FROM Sizes WHERE Name='L');
DECLARE @cBlack INT=(SELECT ColorId FROM Colors WHERE Name=N'Đen'),
        @cWhite INT=(SELECT ColorId FROM Colors WHERE Name=N'Trắng'),
        @cNavy  INT=(SELECT ColorId FROM Colors WHERE Name=N'Xanh navy');

INSERT INTO ProductImages (ProductId, ImageUrl, IsMain, DisplayOrder)
SELECT ProductId, '/images/products/' + Slug + '.jpg', 1, 0
FROM Products p
WHERE NOT EXISTS (SELECT 1 FROM ProductImages i WHERE i.ProductId = p.ProductId);

INSERT INTO ProductVariants (ProductId, ColorId, SizeId, Stock)
SELECT p.ProductId, c.ColorId, s.SizeId, 20
FROM Products p
CROSS JOIN (SELECT @cBlack ColorId UNION ALL SELECT @cWhite UNION ALL SELECT @cNavy) c
CROSS JOIN (SELECT @blackS SizeId UNION ALL SELECT @M UNION ALL SELECT @L) s
WHERE NOT EXISTS (SELECT 1 FROM ProductVariants v WHERE v.ProductId = p.ProductId);

SELECT N'Tổng sản phẩm' AS Info, COUNT(*) AS SoLuong FROM Products;
GO
