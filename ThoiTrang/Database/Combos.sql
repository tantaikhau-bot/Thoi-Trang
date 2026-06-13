-- ============================================================
-- Bảng Combo + ComboItems + seed combo thật từ sản phẩm
-- sqlcmd -S localhost -E -C -I -f 65001 -d MonoWear -i Combos.sql
-- ============================================================
SET NOCOUNT ON;

IF OBJECT_ID('dbo.ComboItems','U') IS NULL
AND OBJECT_ID('dbo.Combos','U') IS NULL
BEGIN
    CREATE TABLE Combos (
        ComboId     INT IDENTITY(1,1) PRIMARY KEY,
        Name        NVARCHAR(200) NOT NULL,
        Description NVARCHAR(500) NULL,
        ComboPrice  DECIMAL(18,0) NOT NULL,
        OldPrice    DECIMAL(18,0) NOT NULL DEFAULT 0,
        Badge       NVARCHAR(20) NULL,
        IsActive    BIT NOT NULL DEFAULT 1,
        CreatedAt   DATETIME2 NOT NULL DEFAULT SYSDATETIME()
    );
    CREATE TABLE ComboItems (
        ComboItemId INT IDENTITY(1,1) PRIMARY KEY,
        ComboId     INT NOT NULL,
        ProductId   INT NOT NULL,
        Quantity    INT NOT NULL DEFAULT 1,
        CONSTRAINT FK_ComboItems_Combos FOREIGN KEY (ComboId) REFERENCES Combos(ComboId) ON DELETE CASCADE,
        CONSTRAINT FK_ComboItems_Products FOREIGN KEY (ProductId) REFERENCES Products(ProductId)
    );
    PRINT N'Đã tạo bảng Combos + ComboItems';
END
ELSE PRINT N'Bảng đã tồn tại';
GO

-- Seed nếu chưa có combo
IF NOT EXISTS (SELECT 1 FROM Combos)
BEGIN
    -- Combo 1: 3 áo (thun + sơ mi + polo) — giá ưu đãi
    INSERT INTO Combos (Name, Description, ComboPrice, OldPrice, Badge) VALUES
    (N'Combo Smart Casual', N'Áo thun + Sơ mi linen + Polo cao cấp — phong cách thanh lịch hằng ngày', 1000000, 1209000, N'fire');
    DECLARE @c1 INT = SCOPE_IDENTITY();
    INSERT INTO ComboItems (ComboId, ProductId, Quantity) VALUES
    (@c1, 1, 1), (@c1, 2, 1), (@c1, 5, 1);

    -- Combo 2: Office Outfit (sơ mi + quần khaki)
    INSERT INTO Combos (Name, Description, ComboPrice, OldPrice, Badge) VALUES
    (N'Combo Office Outfit', N'Sơ mi linen + Quần tailored khaki — chỉn chu nơi công sở', 1000000, 1145000, N'new');
    DECLARE @c2 INT = SCOPE_IDENTITY();
    INSERT INTO ComboItems (ComboId, ProductId, Quantity) VALUES
    (@c2, 2, 1), (@c2, 3, 1);

    -- Combo 3: Street Style (hoodie + jeans + bomber)
    INSERT INTO Combos (Name, Description, ComboPrice, OldPrice, Badge) VALUES
    (N'Combo Street Style', N'Hoodie + Jeans slim-fit + Áo khoác bomber — cá tính năng động', 1800000, 2060000, N'fire');
    DECLARE @c3 INT = SCOPE_IDENTITY();
    INSERT INTO ComboItems (ComboId, ProductId, Quantity) VALUES
    (@c3, 6, 1), (@c3, 8, 1), (@c3, 7, 1);

    -- Combo 4: Nữ thanh lịch (đầm + chân váy + blouse)
    INSERT INTO Combos (Name, Description, ComboPrice, OldPrice, Badge) VALUES
    (N'Combo Nữ Thanh Lịch', N'Đầm suông + Chân váy bút chì + Áo blouse lụa — nữ tính tinh tế', 1500000, 1720000, NULL);
    DECLARE @c4 INT = SCOPE_IDENTITY();
    INSERT INTO ComboItems (ComboId, ProductId, Quantity) VALUES
    (@c4, 4, 1), (@c4, 12, 1), (@c4, 13, 1);

    PRINT N'✅ Đã seed 4 combo';
END
ELSE PRINT N'Đã có combo, bỏ qua seed';
