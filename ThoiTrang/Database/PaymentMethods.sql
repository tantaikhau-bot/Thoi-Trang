USE MonoWear;
GO
IF OBJECT_ID('PaymentMethods') IS NULL
BEGIN
CREATE TABLE PaymentMethods (
    PaymentMethodId INT IDENTITY(1,1) PRIMARY KEY,
    UserId    INT NOT NULL,
    Type      VARCHAR(20) NOT NULL DEFAULT 'momo',
    Label     NVARCHAR(80) NOT NULL,
    Detail    NVARCHAR(120) NULL,
    IsDefault BIT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT FK_PM_User FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE CASCADE
);
PRINT N'Tạo bảng PaymentMethods thành công';
END
ELSE PRINT N'Bảng PaymentMethods đã tồn tại';
GO
