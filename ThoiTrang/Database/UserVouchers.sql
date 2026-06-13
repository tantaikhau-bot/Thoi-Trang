USE MonoWear;
GO
IF OBJECT_ID('UserVouchers') IS NULL
BEGIN
CREATE TABLE UserVouchers (
    UserVoucherId INT IDENTITY(1,1) PRIMARY KEY,
    UserId        INT NOT NULL,
    VoucherId     INT NOT NULL,
    IsUsed        BIT NOT NULL DEFAULT 0,
    SavedAt       DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    UsedAt        DATETIME2 NULL,
    CONSTRAINT FK_UV_User FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE CASCADE,
    CONSTRAINT FK_UV_Voucher FOREIGN KEY (VoucherId) REFERENCES Vouchers(VoucherId),
    CONSTRAINT UQ_UV UNIQUE (UserId, VoucherId)
);
PRINT N'Tạo bảng UserVouchers thành công';
END
ELSE PRINT N'Bảng UserVouchers đã tồn tại';
GO
