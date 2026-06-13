/* =====================================================================
   MONO.WEAR - Website thời trang (ASP.NET Core MVC + SQL Server)
   Script tạo Database + Bảng + Dữ liệu mẫu
   Chạy trên SQL Server (SSMS hoặc Azure Data Studio)
   ===================================================================== */

-- 1. TẠO DATABASE -----------------------------------------------------
IF DB_ID('MonoWear') IS NOT NULL
BEGIN
    ALTER DATABASE MonoWear SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE MonoWear;
END
GO
CREATE DATABASE MonoWear;
GO
USE MonoWear;
GO

/* =====================================================================
   2. DANH MỤC (Categories) — Nam, Nữ, Mới về, Sale, Bộ sưu tập...
   ===================================================================== */
CREATE TABLE Categories (
    CategoryId   INT IDENTITY(1,1) PRIMARY KEY,
    Name         NVARCHAR(100)  NOT NULL,
    Slug         VARCHAR(120)   NOT NULL UNIQUE,   -- nam, nu, moi-ve...
    ParentId     INT            NULL,              -- danh mục cha (đệ quy)
    DisplayOrder INT            NOT NULL DEFAULT 0,
    IsActive     BIT            NOT NULL DEFAULT 1,
    CreatedAt    DATETIME2      NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT FK_Categories_Parent FOREIGN KEY (ParentId)
        REFERENCES Categories(CategoryId)
);
GO

/* =====================================================================
   3. SẢN PHẨM (Products)
   ===================================================================== */
CREATE TABLE Products (
    ProductId     INT IDENTITY(1,1) PRIMARY KEY,
    CategoryId    INT            NOT NULL,
    Name          NVARCHAR(200)  NOT NULL,
    Slug          VARCHAR(220)   NOT NULL UNIQUE,
    Sku           VARCHAR(50)    NULL,
    ShortDesc     NVARCHAR(500)  NULL,
    Description   NVARCHAR(MAX)  NULL,
    Price         DECIMAL(18,0)  NOT NULL,          -- giá hiện tại (VND)
    OldPrice      DECIMAL(18,0)  NULL,              -- giá gạch ngang
    Material      NVARCHAR(200)  NULL,              -- chất liệu (specs)
    Origin        NVARCHAR(100)  NULL,              -- xuất xứ
    IsNew         BIT            NOT NULL DEFAULT 0,
    IsSale        BIT            NOT NULL DEFAULT 0,
    IsFeatured    BIT            NOT NULL DEFAULT 0,
    IsActive      BIT            NOT NULL DEFAULT 1,
    RatingAvg     DECIMAL(3,2)   NOT NULL DEFAULT 0, -- điểm TB (4.8)
    RatingCount   INT            NOT NULL DEFAULT 0,
    SoldCount     INT            NOT NULL DEFAULT 0,
    CreatedAt     DATETIME2      NOT NULL DEFAULT SYSDATETIME(),
    UpdatedAt     DATETIME2      NULL,
    CONSTRAINT FK_Products_Category FOREIGN KEY (CategoryId)
        REFERENCES Categories(CategoryId)
);
GO

/* Ảnh sản phẩm (gallery nhiều ảnh) */
CREATE TABLE ProductImages (
    ImageId      INT IDENTITY(1,1) PRIMARY KEY,
    ProductId    INT           NOT NULL,
    ImageUrl     NVARCHAR(500) NOT NULL,
    IsMain       BIT           NOT NULL DEFAULT 0,
    DisplayOrder INT           NOT NULL DEFAULT 0,
    CONSTRAINT FK_ProductImages_Product FOREIGN KEY (ProductId)
        REFERENCES Products(ProductId) ON DELETE CASCADE
);
GO

/* Màu sắc */
CREATE TABLE Colors (
    ColorId   INT IDENTITY(1,1) PRIMARY KEY,
    Name      NVARCHAR(50) NOT NULL,   -- Đen, Trắng, Kem...
    HexCode   VARCHAR(7)   NULL        -- #1a1a1a
);
GO

/* Kích cỡ */
CREATE TABLE Sizes (
    SizeId       INT IDENTITY(1,1) PRIMARY KEY,
    Name         VARCHAR(20) NOT NULL,  -- S, M, L, XL, 30, 42...
    DisplayOrder INT         NOT NULL DEFAULT 0
);
GO

/* Biến thể sản phẩm (Màu + Size + tồn kho riêng) */
CREATE TABLE ProductVariants (
    VariantId   INT IDENTITY(1,1) PRIMARY KEY,
    ProductId   INT           NOT NULL,
    ColorId     INT           NULL,
    SizeId      INT           NULL,
    Sku         VARCHAR(60)   NULL,
    Stock       INT           NOT NULL DEFAULT 0,   -- số lượng tồn kho
    PriceExtra  DECIMAL(18,0) NOT NULL DEFAULT 0,   -- phụ thu theo biến thể
    CONSTRAINT FK_Variants_Product FOREIGN KEY (ProductId)
        REFERENCES Products(ProductId) ON DELETE CASCADE,
    CONSTRAINT FK_Variants_Color FOREIGN KEY (ColorId) REFERENCES Colors(ColorId),
    CONSTRAINT FK_Variants_Size  FOREIGN KEY (SizeId)  REFERENCES Sizes(SizeId)
);
GO

/* =====================================================================
   4. NGƯỜI DÙNG (Users) + Địa chỉ
   ===================================================================== */
CREATE TABLE Users (
    UserId       INT IDENTITY(1,1) PRIMARY KEY,
    FullName     NVARCHAR(150) NOT NULL,
    Email        NVARCHAR(150) NOT NULL UNIQUE,
    Phone        VARCHAR(20)   NULL,
    PasswordHash NVARCHAR(255) NOT NULL,           -- lưu hash, KHÔNG lưu plaintext
    AvatarUrl    NVARCHAR(500) NULL,
    Gender       NVARCHAR(10)  NULL,               -- Nam / Nữ / Khác
    BirthDate    DATE          NULL,
    Role         VARCHAR(20)   NOT NULL DEFAULT 'Customer', -- Customer / Admin
    IsActive     BIT           NOT NULL DEFAULT 1,
    CreatedAt    DATETIME2     NOT NULL DEFAULT SYSDATETIME()
);
GO

CREATE TABLE Addresses (
    AddressId    INT IDENTITY(1,1) PRIMARY KEY,
    UserId       INT           NOT NULL,
    ReceiverName NVARCHAR(150) NOT NULL,
    Phone        VARCHAR(20)   NOT NULL,
    Province     NVARCHAR(100) NOT NULL,           -- Tỉnh/Thành
    District     NVARCHAR(100) NOT NULL,           -- Quận/Huyện
    Ward         NVARCHAR(100) NULL,               -- Phường/Xã
    AddressLine  NVARCHAR(300) NOT NULL,           -- số nhà, đường
    IsDefault    BIT           NOT NULL DEFAULT 0,
    CONSTRAINT FK_Addresses_User FOREIGN KEY (UserId)
        REFERENCES Users(UserId) ON DELETE CASCADE
);
GO

/* =====================================================================
   5. GIỎ HÀNG (Cart) + YÊU THÍCH (Wishlist)
   ===================================================================== */
CREATE TABLE CartItems (
    CartItemId INT IDENTITY(1,1) PRIMARY KEY,
    UserId     INT       NOT NULL,
    VariantId  INT       NOT NULL,
    Quantity   INT       NOT NULL DEFAULT 1,
    CreatedAt  DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT FK_Cart_User    FOREIGN KEY (UserId)    REFERENCES Users(UserId) ON DELETE CASCADE,
    CONSTRAINT FK_Cart_Variant FOREIGN KEY (VariantId) REFERENCES ProductVariants(VariantId),
    CONSTRAINT UQ_Cart UNIQUE (UserId, VariantId)
);
GO

CREATE TABLE Wishlists (
    WishlistId INT IDENTITY(1,1) PRIMARY KEY,
    UserId     INT       NOT NULL,
    ProductId  INT       NOT NULL,
    CreatedAt  DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT FK_Wish_User    FOREIGN KEY (UserId)    REFERENCES Users(UserId) ON DELETE CASCADE,
    CONSTRAINT FK_Wish_Product FOREIGN KEY (ProductId) REFERENCES Products(ProductId),
    CONSTRAINT UQ_Wish UNIQUE (UserId, ProductId)
);
GO

/* =====================================================================
   6. VOUCHER / KHUYẾN MÃI
   ===================================================================== */
CREATE TABLE Vouchers (
    VoucherId     INT IDENTITY(1,1) PRIMARY KEY,
    Code          VARCHAR(50)   NOT NULL UNIQUE,
    Description   NVARCHAR(300) NULL,
    DiscountType  VARCHAR(10)   NOT NULL DEFAULT 'amount', -- amount / percent
    DiscountValue DECIMAL(18,0) NOT NULL,           -- 50000 hoặc 10 (%)
    MinOrder      DECIMAL(18,0) NOT NULL DEFAULT 0,  -- đơn tối thiểu
    MaxDiscount   DECIMAL(18,0) NULL,                -- giảm tối đa (cho percent)
    Quantity      INT           NOT NULL DEFAULT 0,  -- số lượt còn lại
    StartDate     DATETIME2     NULL,
    EndDate       DATETIME2     NULL,
    IsActive      BIT           NOT NULL DEFAULT 1
);
GO

/* =====================================================================
   7. ĐƠN HÀNG (Orders) + Chi tiết
   ===================================================================== */
CREATE TABLE Orders (
    OrderId        INT IDENTITY(1,1) PRIMARY KEY,
    OrderCode      VARCHAR(30)   NOT NULL UNIQUE,    -- MNW1234567890
    UserId         INT           NULL,              -- NULL = khách vãng lai
    -- Thông tin người nhận (snapshot lúc đặt)
    ReceiverName   NVARCHAR(150) NOT NULL,
    ReceiverPhone  VARCHAR(20)   NOT NULL,
    ShippingAddress NVARCHAR(400) NOT NULL,
    Note           NVARCHAR(500) NULL,
    -- Tiền
    Subtotal       DECIMAL(18,0) NOT NULL,           -- tạm tính
    ProductDiscount DECIMAL(18,0) NOT NULL DEFAULT 0,
    VoucherId      INT           NULL,
    VoucherDiscount DECIMAL(18,0) NOT NULL DEFAULT 0,
    ShippingFee    DECIMAL(18,0) NOT NULL DEFAULT 0,
    TotalAmount    DECIMAL(18,0) NOT NULL,           -- thành tiền cuối
    -- Vận chuyển & thanh toán
    ShippingMethod VARCHAR(20)   NOT NULL DEFAULT 'standard', -- standard/express/store
    PaymentMethod  VARCHAR(20)   NOT NULL DEFAULT 'cod',      -- bank/momo/vnpay/cod
    PaymentStatus  VARCHAR(20)   NOT NULL DEFAULT 'Pending',  -- Pending/Paid/Failed
    OrderStatus    VARCHAR(20)   NOT NULL DEFAULT 'Pending',  -- Pending/Confirmed/Shipping/Completed/Cancelled
    CreatedAt      DATETIME2     NOT NULL DEFAULT SYSDATETIME(),
    UpdatedAt      DATETIME2     NULL,
    CONSTRAINT FK_Orders_User    FOREIGN KEY (UserId)    REFERENCES Users(UserId),
    CONSTRAINT FK_Orders_Voucher FOREIGN KEY (VoucherId) REFERENCES Vouchers(VoucherId)
);
GO

CREATE TABLE OrderDetails (
    OrderDetailId INT IDENTITY(1,1) PRIMARY KEY,
    OrderId       INT           NOT NULL,
    VariantId     INT           NULL,               -- có thể NULL nếu biến thể bị xóa
    -- Snapshot thông tin sản phẩm lúc mua
    ProductName   NVARCHAR(200) NOT NULL,
    VariantInfo   NVARCHAR(120) NULL,               -- "Kem · Size M"
    UnitPrice     DECIMAL(18,0) NOT NULL,
    Quantity      INT           NOT NULL,
    LineTotal     AS (UnitPrice * Quantity) PERSISTED,
    CONSTRAINT FK_OrderDetails_Order   FOREIGN KEY (OrderId)
        REFERENCES Orders(OrderId) ON DELETE CASCADE,
    CONSTRAINT FK_OrderDetails_Variant FOREIGN KEY (VariantId)
        REFERENCES ProductVariants(VariantId)
);
GO

/* =====================================================================
   8. ĐÁNH GIÁ (Reviews) + HỎI ĐÁP (Q&A)
   ===================================================================== */
CREATE TABLE Reviews (
    ReviewId     INT IDENTITY(1,1) PRIMARY KEY,
    ProductId    INT           NOT NULL,
    UserId       INT           NULL,
    OrderId      INT           NULL,                -- để xác thực "đã mua"
    Rating       TINYINT       NOT NULL CHECK (Rating BETWEEN 1 AND 5),
    Content      NVARCHAR(MAX) NULL,
    IsVerified   BIT           NOT NULL DEFAULT 0,  -- "Đã mua hàng"
    HelpfulCount INT           NOT NULL DEFAULT 0,
    CreatedAt    DATETIME2     NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT FK_Reviews_Product FOREIGN KEY (ProductId) REFERENCES Products(ProductId) ON DELETE CASCADE,
    CONSTRAINT FK_Reviews_User    FOREIGN KEY (UserId)    REFERENCES Users(UserId)
);
GO

CREATE TABLE ProductQuestions (
    QuestionId  INT IDENTITY(1,1) PRIMARY KEY,
    ProductId   INT           NOT NULL,
    UserId      INT           NULL,
    Question    NVARCHAR(MAX) NOT NULL,
    Answer      NVARCHAR(MAX) NULL,                 -- shop trả lời
    AnsweredAt  DATETIME2     NULL,
    CreatedAt   DATETIME2     NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT FK_Questions_Product FOREIGN KEY (ProductId) REFERENCES Products(ProductId) ON DELETE CASCADE,
    CONSTRAINT FK_Questions_User    FOREIGN KEY (UserId)    REFERENCES Users(UserId)
);
GO

/* =====================================================================
   9. THÔNG BÁO (Notifications)
   ===================================================================== */
CREATE TABLE Notifications (
    NotificationId INT IDENTITY(1,1) PRIMARY KEY,
    UserId    INT           NULL,                   -- NULL = thông báo chung
    Title     NVARCHAR(200) NOT NULL,
    Content   NVARCHAR(500) NULL,
    Type      VARCHAR(20)   NOT NULL DEFAULT 'info', -- info/order/promo
    IsRead    BIT           NOT NULL DEFAULT 0,
    CreatedAt DATETIME2     NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT FK_Notifications_User FOREIGN KEY (UserId)
        REFERENCES Users(UserId) ON DELETE CASCADE
);
GO

/* =====================================================================
   10. CHỈ MỤC (Index) tăng tốc truy vấn
   ===================================================================== */
CREATE INDEX IX_Products_Category ON Products(CategoryId);
CREATE INDEX IX_Variants_Product  ON ProductVariants(ProductId);
CREATE INDEX IX_Orders_User       ON Orders(UserId);
CREATE INDEX IX_Orders_Status     ON Orders(OrderStatus);
CREATE INDEX IX_Reviews_Product   ON Reviews(ProductId);
GO

/* =====================================================================
   11. DỮ LIỆU MẪU (Seed data)
   ===================================================================== */
-- Danh mục
INSERT INTO Categories (Name, Slug, DisplayOrder) VALUES
 (N'Nam', 'nam', 1),
 (N'Nữ', 'nu', 2),
 (N'Mới về', 'moi-ve', 3),
 (N'Bộ sưu tập', 'bo-suu-tap', 4),
 (N'Sale', 'sale', 5);

-- Màu sắc
INSERT INTO Colors (Name, HexCode) VALUES
 (N'Đen', '#1a1a1a'), (N'Trắng', '#fafaf6'), (N'Xám', '#888888'),
 (N'Kem', '#f1efe8'), (N'Xanh navy', '#2c3e5e'), (N'Nâu', '#6b4423');

-- Kích cỡ
INSERT INTO Sizes (Name, DisplayOrder) VALUES
 ('S',1),('M',2),('L',3),('XL',4),('XXL',5),('30',6),('32',7),('42',8);

-- Người dùng (Admin + Customer). PasswordHash demo - thay bằng hash thật khi dùng
INSERT INTO Users (FullName, Email, Phone, PasswordHash, Role) VALUES
 (N'Quản trị viên', 'admin@monowear.vn', '0900000000', 'HASH_ADMIN', 'Admin'),
 (N'Trần Minh Khôi', 'khoi@example.com', '0901234567', 'HASH_USER', 'Customer');

-- Địa chỉ
INSERT INTO Addresses (UserId, ReceiverName, Phone, Province, District, Ward, AddressLine, IsDefault) VALUES
 (2, N'Trần Minh Khôi', '0901234567', N'TP. Hồ Chí Minh', N'Quận 1', N'Phường Bến Nghé', N'123 Nguyễn Huệ', 1);

-- Sản phẩm
INSERT INTO Products (CategoryId, Name, Slug, Price, OldPrice, Material, Origin, IsNew, IsSale, IsFeatured, RatingAvg, RatingCount, SoldCount, ShortDesc)
VALUES
 (1, N'Áo thun cotton Essential', 'ao-thun-cotton-essential', 299000, 399000, N'100% Cotton', N'Việt Nam', 1, 1, 1, 4.8, 124, 530, N'Áo thun cotton mềm mại, form regular'),
 (1, N'Áo sơ mi linen oversize', 'ao-so-mi-linen-oversize', 525000, 650000, N'Linen', N'Việt Nam', 1, 1, 0, 4.7, 88, 210, N'Sơ mi linen thoáng mát'),
 (1, N'Quần tailored khaki', 'quan-tailored-khaki', 620000, NULL, N'Kaki cao cấp', N'Việt Nam', 0, 0, 1, 4.6, 64, 150, N'Quần tây dáng slim'),
 (2, N'Đầm suông tối giản', 'dam-suong-toi-gian', 750000, 900000, N'Cotton blend', N'Việt Nam', 1, 1, 1, 4.9, 56, 98, N'Đầm suông thanh lịch');

-- Ảnh sản phẩm
INSERT INTO ProductImages (ProductId, ImageUrl, IsMain, DisplayOrder) VALUES
 (1, '/images/products/tee-1.jpg', 1, 0),
 (1, '/images/products/tee-2.jpg', 0, 1),
 (2, '/images/products/shirt-1.jpg', 1, 0),
 (3, '/images/products/pants-1.jpg', 1, 0),
 (4, '/images/products/dress-1.jpg', 1, 0);

-- Biến thể (Màu + Size + tồn kho)
INSERT INTO ProductVariants (ProductId, ColorId, SizeId, Stock) VALUES
 (1, 1, 1, 20), (1, 1, 2, 35), (1, 1, 3, 15),   -- Áo thun đen S/M/L
 (1, 2, 2, 25), (1, 2, 3, 10),                   -- Áo thun trắng M/L
 (2, 4, 2, 12), (2, 4, 3, 8),                    -- Sơ mi kem M/L
 (3, 6, 6, 18), (3, 6, 7, 14),                   -- Quần nâu 30/32
 (4, 1, 1, 9),  (4, 1, 2, 11);                   -- Đầm đen S/M

-- Voucher
INSERT INTO Vouchers (Code, Description, DiscountType, DiscountValue, MinOrder, MaxDiscount, Quantity, IsActive) VALUES
 ('WELCOME50',  N'Giảm 50K cho đơn từ 500K', 'amount', 50000, 500000, NULL, 100, 1),
 ('SALE10',     N'Giảm 10% tối đa 100K',     'percent', 10, 300000, 100000, 200, 1),
 ('FREESHIP',   N'Miễn phí vận chuyển',       'amount', 30000, 0, NULL, 500, 1);

-- Thông báo chung
INSERT INTO Notifications (UserId, Title, Content, Type) VALUES
 (NULL, N'Chào mừng đến MONO.WEAR', N'Khám phá bộ sưu tập mới mùa Thu 2026', 'promo'),
 (2, N'Đơn hàng đang xử lý', N'Đơn hàng của bạn đã được tiếp nhận', 'order');

-- Đánh giá mẫu
INSERT INTO Reviews (ProductId, UserId, Rating, Content, IsVerified, HelpfulCount) VALUES
 (1, 2, 5, N'Vải đẹp, mặc thoải mái, đúng size!', 1, 12),
 (1, 2, 4, N'Chất ổn so với giá', 1, 3);

-- Q&A mẫu
INSERT INTO ProductQuestions (ProductId, UserId, Question, Answer, AnsweredAt) VALUES
 (1, 2, N'Áo có bị co rút khi giặt không?', N'Sản phẩm đã xử lý chống co rút, bạn yên tâm nhé!', SYSDATETIME());
GO

/* =====================================================================
   12. ĐƠN HÀNG MẪU (demo luồng checkout)
   ===================================================================== */
INSERT INTO Orders (OrderCode, UserId, ReceiverName, ReceiverPhone, ShippingAddress, Note,
                    Subtotal, ProductDiscount, ShippingFee, TotalAmount,
                    ShippingMethod, PaymentMethod, PaymentStatus, OrderStatus)
VALUES
 ('MNW0000000001', 2, N'Trần Minh Khôi', '0901234567',
  N'123 Nguyễn Huệ, Phường Bến Nghé, Quận 1, TP. Hồ Chí Minh', N'Giao giờ hành chính',
  598000, 100000, 30000, 528000,
  'standard', 'bank', 'Paid', 'Confirmed');

INSERT INTO OrderDetails (OrderId, VariantId, ProductName, VariantInfo, UnitPrice, Quantity) VALUES
 (1, 2, N'Áo thun cotton Essential', N'Đen · Size M', 299000, 2);
GO

PRINT N'✅ Tạo database MonoWear thành công!';
GO
