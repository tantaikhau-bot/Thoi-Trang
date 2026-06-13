USE MonoWear;
GO
IF OBJECT_ID('ChatMessages') IS NULL
BEGIN
CREATE TABLE ChatMessages (
    ChatMessageId INT IDENTITY(1,1) PRIMARY KEY,
    UserId        INT NOT NULL,
    FromAdmin     BIT NOT NULL DEFAULT 0,
    Content       NVARCHAR(1000) NOT NULL,
    IsRead        BIT NOT NULL DEFAULT 0,
    CreatedAt     DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT FK_Chat_User FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE CASCADE
);
CREATE INDEX IX_Chat_User ON ChatMessages(UserId);
PRINT N'Tạo bảng ChatMessages thành công';
END
ELSE PRINT N'Bảng ChatMessages đã tồn tại';
GO
