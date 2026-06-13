-- ============================================================
-- Bảng Collections (CMS Bộ sưu tập) + seed 4 bộ sưu tập hiện có
-- sqlcmd -S localhost -E -C -I -f 65001 -d MonoWear -i Collections.sql
-- ============================================================
SET NOCOUNT ON;

IF OBJECT_ID('dbo.Collections','U') IS NULL
BEGIN
    CREATE TABLE Collections (
        CollectionId INT IDENTITY(1,1) PRIMARY KEY,
        Label        NVARCHAR(100) NULL,
        Title        NVARCHAR(200) NOT NULL,
        Description  NVARCHAR(800) NULL,
        Icon         NVARCHAR(50) NULL,
        CoverClass   NVARCHAR(50) NULL,
        LinkUrl      NVARCHAR(200) NULL,
        LinkText     NVARCHAR(80) NULL,
        DisplayOrder INT NOT NULL DEFAULT 0,
        IsActive     BIT NOT NULL DEFAULT 1,
        CreatedAt    DATETIME2 NOT NULL DEFAULT SYSDATETIME()
    );
    PRINT N'Đã tạo bảng Collections';
END
ELSE PRINT N'Bảng đã tồn tại';
GO

IF NOT EXISTS (SELECT 1 FROM Collections)
BEGIN
    INSERT INTO Collections (Label, Title, Description, Icon, CoverClass, LinkUrl, LinkText, DisplayOrder) VALUES
    (N'CAMPAIGN AUTUMN ''26', N'Sự Tĩnh Lặng Của Thành Thị',
     N'Khắc họa nhịp điệu bình lặng giữa lòng phố thị tấp nập qua những tone màu đất ấm áp, phom dáng oversize giải phóng chuyển động cơ thể.',
     N'ti-hanger', N'bg-concept-1', N'/Home/CampaignAutumn', N'Khám phá chi tiết', 1),
    (N'LIMITED EDITION COLLECTION', N'Dấu Ấn Lập Thể Tối Giản',
     N'Bộ sưu tập viên mãn phá vỡ kết cấu đường may thông thường, kiến tạo cấu trúc hình khối độc lập trên bề mặt chất liệu organic thô mộc.',
     N'ti-needle-thread', N'bg-concept-2', N'/Home/LimitedEdition', N'Xem phiên bản giới hạn', 2),
    (N'RAW MATERIALS FOCUS', N'Bản Giao Hưởng Của Sợi Tự Nhiên',
     N'Nâng niu làn da bằng nguồn sợi thô thượng hạng từ sợi lanh dệt tay phối lụa tơ tằm nguyên bản. Sự cân bằng hoàn hảo giữa tính thô mộc và tinh tế cao cấp.',
     N'ti-feather', N'bg-concept-3', N'/Home/RawMaterials', N'Tìm hiểu chất liệu', 3),
    (N'EDITORIAL CAPSULE', N'Cấu Trúc Tương Lai Cổ Điển',
     N'Tái định nghĩa thời trang may đo (Tailoring) qua các đường cắt bất đối xứng mềm mại. Mang lại vẻ ngoài cá tính ấn tượng nhưng vẫn lịch lãm, trường tồn.',
     N'ti-scissors', N'bg-concept-4', N'/Home/EditorialCapsule', N'Xem Lookbook Capsule', 4);
    PRINT N'✅ Đã seed 4 bộ sưu tập';
END
ELSE PRINT N'Đã có dữ liệu, bỏ qua seed';
