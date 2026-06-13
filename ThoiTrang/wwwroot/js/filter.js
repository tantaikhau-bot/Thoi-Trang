// Bộ lọc danh mục phía client cho trang Nam/Nữ (lọc thẻ sản phẩm theo mục đã chọn)
(function () {
    function norm(s) { return (s || '').toLowerCase().trim(); }

    var STOP = ['và', 'nữ', 'nam', 'các', 'sản', 'phẩm', 'tất', 'cả'];
    // Tách từ khóa từ nhãn ("tất cả..." => null = hiện tất cả)
    function keywords(label) {
        var l = norm(label).replace(/\d+/g, ''); // bỏ số đếm
        if (l.indexOf('tất cả') === 0 || l.indexOf('tat ca') === 0) return null; // null = hiển thị tất cả
        var toks = l.split(/[^a-zà-ỹ]+/i)
                    .filter(function (w) { return w.length >= 2 && STOP.indexOf(w) < 0; });
        return toks.length ? toks : null; // không có từ khóa => coi như hiện tất cả
    }

    var cards = Array.prototype.slice.call(document.querySelectorAll('.products .product'));
    if (cards.length === 0) return;

    function cardText(c) {
        var n = c.querySelector('.product-name');
        var cat = c.querySelector('.product-cat');
        return norm((n ? n.textContent : '') + ' ' + (cat ? cat.textContent : ''));
    }

    function apply(labels) {
        var kwSets = labels.map(keywords).filter(Boolean);
        var shown = 0;
        cards.forEach(function (c) {
            var t = cardText(c);
            var show = kwSets.length === 0 || kwSets.some(function (ks) {
                return ks.some(function (k) { return t.indexOf(k) >= 0; });
            });
            c.style.display = show ? '' : 'none';
            if (show) shown++;
        });
        // Cập nhật bộ đếm "Tìm thấy X sản phẩm" nếu có
        var info = document.querySelector('.list-info, .page-stat-num, .products-count');
        if (info) {
            var m = info.textContent.match(/\d+/);
            if (m) info.innerHTML = info.innerHTML.replace(/\d+/, shown);
        }
    }

    var pills = Array.prototype.slice.call(document.querySelectorAll('.sub-pill, .filter-pill'));

    // Tìm các checkbox thuộc nhóm DANH MỤC (bỏ qua Chất liệu / Đánh giá...)
    var catBoxes = [];
    Array.prototype.slice.call(document.querySelectorAll('h4, .filter-group-title, .sidebar-group-title'))
        .forEach(function (h) {
            if (norm(h.textContent).indexOf('danh mục') === 0) {
                var grp = h.parentElement;
                catBoxes = Array.prototype.slice.call(grp.querySelectorAll('input[type=checkbox]'));
            }
        });

    function gather() {
        var labels = [];
        pills.forEach(function (p) {
            if (p.classList.contains('active') && norm(p.textContent).indexOf('tất cả') !== 0) labels.push(p.textContent);
        });
        catBoxes.forEach(function (cb) {
            if (cb.checked) {
                var lbl = cb.closest('label');
                labels.push(lbl ? lbl.textContent : '');
            }
        });
        return labels;
    }

    // Click chip danh mục (chọn 1)
    pills.forEach(function (pill) {
        pill.addEventListener('click', function () {
            pills.forEach(function (p) { p.classList.remove('active'); });
            pill.classList.add('active');
            // bỏ chọn checkbox để tránh xung đột
            catBoxes.forEach(function (cb) { cb.checked = false; });
            apply(gather());
        });
    });

    // Tick checkbox danh mục (chọn nhiều)
    catBoxes.forEach(function (cb) {
        cb.addEventListener('change', function () {
            // khi dùng checkbox thì đặt chip về "Tất cả"
            pills.forEach(function (p) { p.classList.remove('active'); });
            var allPill = pills.find(function (p) { return norm(p.textContent).indexOf('tất cả') === 0; });
            if (allPill) allPill.classList.add('active');
            apply(gather());
        });
    });

    // Nút "Xóa tất cả" bộ lọc
    var clearBtn = document.getElementById('clearFilter') || document.querySelector('.filter-clear');
    if (clearBtn) {
        clearBtn.addEventListener('click', function () {
            catBoxes.forEach(function (cb) { cb.checked = false; });
            pills.forEach(function (p) { p.classList.remove('active'); });
            var allPill = pills.find(function (p) { return norm(p.textContent).indexOf('tất cả') === 0; });
            if (allPill) allPill.classList.add('active');
            apply([]);
        });
    }

    // Áp dụng trạng thái ban đầu (theo checkbox đã tick sẵn trong HTML)
    apply(gather());
})();
