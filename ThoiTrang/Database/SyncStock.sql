-- ============================================================
-- Đồng bộ tồn kho & đã bán theo đơn Completed thật (chạy 1 lần)
-- sqlcmd -S localhost -E -C -I -f 65001 -d MonoWear -i SyncStock.sql
-- ============================================================
SET NOCOUNT ON;

-- 1) SoldCount mỗi sản phẩm = tổng số lượng đã bán từ các đơn HOÀN TẤT
UPDATE p SET p.SoldCount = ISNULL(t.qty, 0)
FROM Products p
LEFT JOIN (
    SELECT v.ProductId, SUM(od.Quantity) AS qty
    FROM OrderDetails od
    JOIN Orders o ON od.OrderId = o.OrderId
    JOIN ProductVariants v ON od.VariantId = v.VariantId
    WHERE o.OrderStatus = 'Completed'
    GROUP BY v.ProductId
) t ON p.ProductId = t.ProductId;

-- 2) Trừ tồn kho mỗi biến thể theo số đã bán (đơn Completed), chỉ áp dụng cho đơn CHƯA trừ
UPDATE v SET v.Stock = CASE WHEN v.Stock - ISNULL(t.qty,0) < 0 THEN 0 ELSE v.Stock - ISNULL(t.qty,0) END
FROM ProductVariants v
LEFT JOIN (
    SELECT od.VariantId, SUM(od.Quantity) AS qty
    FROM OrderDetails od
    JOIN Orders o ON od.OrderId = o.OrderId
    WHERE o.OrderStatus = 'Completed' AND o.StockDeducted = 0
    GROUP BY od.VariantId
) t ON v.VariantId = t.VariantId
WHERE t.qty IS NOT NULL;

-- 3) Đánh dấu các đơn Completed là đã trừ kho (tránh trừ lại lần sau)
UPDATE Orders SET StockDeducted = 1 WHERE OrderStatus = 'Completed';

PRINT N'✅ Đã đồng bộ tồn kho & đã bán theo đơn Completed.';
