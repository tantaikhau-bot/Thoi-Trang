USE MonoWear;
GO
IF OBJECT_ID('ChatKnowledges') IS NULL
BEGIN
CREATE TABLE ChatKnowledges (
    ChatKnowledgeId INT IDENTITY(1,1) PRIMARY KEY,
    Topic     NVARCHAR(60)  NOT NULL,
    Keywords  NVARCHAR(500) NOT NULL,
    Answer    NVARCHAR(MAX) NOT NULL,
    Priority  INT NOT NULL DEFAULT 0,
    IsActive  BIT NOT NULL DEFAULT 1
);
PRINT N'Tạo bảng ChatKnowledges thành công';
END
GO

IF NOT EXISTS (SELECT 1 FROM ChatKnowledges)
BEGIN
INSERT INTO ChatKnowledges (Topic, Keywords, Answer, Priority) VALUES
(N'greeting', N'xin chào,chào shop,hello,alo,có ai không,chào em',
 N'Dạ em chào anh/chị ạ 👋 Em là trợ lý ảo của MONO.WEAR. Em có thể giúp anh/chị tìm sản phẩm, xem khuyến mãi, tra đơn hàng hoặc tư vấn chọn size ạ. Anh/chị cần gì nhỉ?', 1),

(N'ship', N'ship,vận chuyển,giao hàng,bao lâu,freeship,phí ship,phí giao,giao tận nơi,mất bao lâu,khi nào nhận',
 N'Dạ MONO.WEAR giao hàng toàn quốc 2-3 ngày, nội thành có giao nhanh trong 24h ạ 🚚. Phí ship 30.000₫, FREESHIP cho đơn từ 3.000.000₫ nha anh/chị!', 0),

(N'payment', N'thanh toán,trả tiền,cod,momo,vnpay,chuyển khoản,trả góp,quẹt thẻ,hình thức thanh toán',
 N'Dạ shop hỗ trợ COD (nhận hàng trả tiền), chuyển khoản ngân hàng, Ví Momo và VNPAY ạ 💳. Anh/chị chọn cách nào tiện nhất khi thanh toán nhé!', 0),

(N'return', N'đổi trả,bảo hành,hoàn tiền,trả hàng,đổi size,bị lỗi,không vừa,đổi hàng,chính sách đổi',
 N'Dạ shop hỗ trợ đổi trả trong 30 ngày nếu sản phẩm còn nguyên tem mác ạ 😊 Đổi size/màu hoàn toàn miễn phí. Anh/chị cứ yên tâm mua sắm nhé!', 0),

(N'size', N'size,cỡ,kích cỡ,chọn size,mặc vừa,số đo,bảng size,size nào,bao nhiêu kg',
 N'Dạ về size, anh/chị cho em xin chiều cao & cân nặng em tư vấn chuẩn nha 📏 Tham khảo: 45-55kg → S, 55-65kg → M, 65-75kg → L, 75-85kg → XL. Thích mặc rộng thì lên 1 size ạ!', 0),

(N'store', N'địa chỉ,cửa hàng,ở đâu,showroom,chi nhánh,tới shop,đến shop,xem trực tiếp',
 N'Dạ MONO.WEAR Store ở 123 Đồng Khởi, Quận 1, TP.HCM ạ 📍 Mở cửa 8:00 - 21:30 hằng ngày. Anh/chị ghé tham quan và thử đồ trực tiếp nhé!', 0),

(N'hours', N'giờ mở cửa,mấy giờ,giờ làm việc,mở cửa lúc,đóng cửa',
 N'Dạ cửa hàng mở cửa 8:00 - 21:30 tất cả các ngày trong tuần ạ ⏰. Đặt hàng online thì 24/7 luôn nha anh/chị!', 0),

(N'hotline', N'hotline,liên hệ,số điện thoại,cskh,chăm sóc khách,gọi cho shop,số hotline',
 N'Dạ anh/chị liên hệ hotline 1900 6789 (8:00-22:00) hoặc nhắn ngay tại đây em hỗ trợ liền ạ ☎️', 0),

(N'material', N'chất liệu,vải gì,cotton,linen,chất vải,vải có tốt,chất vải gì,thành phần',
 N'Dạ sản phẩm MONO.WEAR dùng chất liệu cao cấp như cotton, linen, denim... thoáng mát, bền đẹp và thân thiện da ạ 🌿 Anh/chị xem chi tiết chất liệu trong từng sản phẩm nhé!', 0),

(N'wash', N'giặt,bảo quản,giặt máy,ủi đồ,là đồ,giặt như thế nào,cách giặt',
 N'Dạ anh/chị nên giặt máy ở chế độ nhẹ với nước lạnh, lộn trái áo và phơi nơi thoáng mát để giữ form và màu lâu nhất ạ 🧺 Hạn chế sấy nhiệt cao nha!', 0),

(N'order_guide', N'đặt hàng online,mua online,đặt như thế nào,cách đặt,hướng dẫn mua,mua hàng sao',
 N'Dạ anh/chị chọn sản phẩm → bấm "Thêm vào giỏ" → vào Giỏ hàng → "Thanh toán" → điền địa chỉ & chọn phương thức là xong ạ 🛒 Cần đăng nhập để chốt đơn nha. Em có thể chốt giúp anh/chị luôn!', 0),

(N'membership', N'thành viên,hạng,vip,thẻ thành viên,ưu đãi thành viên,lên hạng',
 N'Dạ MONO.WEAR có 4 hạng: Bronze → Silver → Gold → Platinum, tích theo chi tiêu ạ 👑 Hạng càng cao ưu đãi càng lớn (giảm giá, freeship, quà sinh nhật). Anh/chị xem trong mục Hạng thành viên nhé!', 0),

(N'points', N'điểm thưởng,tích điểm,đổi điểm,điểm tích lũy,bao nhiêu điểm',
 N'Dạ anh/chị tích 1 điểm cho mỗi 1.000₫ chi tiêu ạ ✨ Điểm dùng để đổi voucher và quà. Xem điểm hiện có trong mục Điểm thưởng của tài khoản nha!', 0),

(N'quality', N'chính hãng,hàng thật,chất lượng,uy tín,hàng giả,có đảm bảo',
 N'Dạ MONO.WEAR cam kết 100% sản phẩm chính hãng, chất lượng cao ạ ✅ Đổi trả 30 ngày nếu không hài lòng. Anh/chị yên tâm nha!', 0),

(N'gift', N'gói quà,quà tặng,bọc quà,gift,tặng người yêu,gói tặng',
 N'Dạ shop có dịch vụ gói quà xinh xắn miễn phí ạ 🎁 Anh/chị ghi chú "gói quà" khi đặt đơn, em sẽ chuẩn bị chu đáo nhé!', 0),

(N'brand', N'mono wear,thương hiệu,về shop,giới thiệu,shop là ai,mono là gì',
 N'Dạ MONO.WEAR là thương hiệu thời trang tối giản, tinh tế, mang lại sự tự tin và phong cách hiện đại ạ ✨ Rất vui được phục vụ anh/chị!', 0),

(N'student', N'sinh viên,giảm sinh viên,học sinh,ưu đãi sinh viên',
 N'Dạ sinh viên xuất trình thẻ được giảm thêm 5% tại cửa hàng ạ 🎓 Mua online thì anh/chị săn voucher ở trang Sale nha!', 0),

(N'thanks', N'cảm ơn,thank,tks,tạm biệt,bye,tốt quá,oke shop,cám ơn',
 N'Dạ em cảm ơn anh/chị rất nhiều ạ 🌷 Chúc anh/chị một ngày tốt lành và luôn mặc đẹp cùng MONO.WEAR! Cần gì cứ nhắn em nhé ❤️', 0);
PRINT N'Đã nạp kho tri thức chatbot';
END
ELSE PRINT N'Kho tri thức đã có dữ liệu';
GO
SELECT COUNT(*) AS SoCauTraLoi FROM ChatKnowledges;
GO
