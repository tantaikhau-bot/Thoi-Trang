// ===== Tìm kiếm thông minh: gợi ý + lịch sử + từ khóa phổ biến =====
(function () {
    var HISTORY_KEY = 'monoSearchHistory';
    var popularCache = null;

    function getHistory() {
        try { return JSON.parse(localStorage.getItem(HISTORY_KEY)) || []; } catch (e) { return []; }
    }
    function saveHistory(term) {
        term = (term || '').trim();
        if (!term) return;
        var h = getHistory().filter(function (x) { return x.toLowerCase() !== term.toLowerCase(); });
        h.unshift(term);
        if (h.length > 8) h = h.slice(0, 8);
        try { localStorage.setItem(HISTORY_KEY, JSON.stringify(h)); } catch (e) {}
    }
    function clearHistory() { try { localStorage.removeItem(HISTORY_KEY); } catch (e) {} }

    function go(term) {
        term = (term || '').trim();
        if (!term) return;
        saveHistory(term);
        window.location.href = '/Home/Search?q=' + encodeURIComponent(term);
    }

    function fmt(n) { return (n || 0).toLocaleString('vi-VN') + '₫'; }
    function esc(s) { return (s || '').replace(/[&<>"]/g, function (c) { return { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;' }[c]; }); }
    function highlight(text, q) {
        if (!q) return esc(text);
        var i = text.toLowerCase().indexOf(q.toLowerCase());
        if (i < 0) return esc(text);
        return esc(text.slice(0, i)) + '<b style="color:#d4537e;">' + esc(text.slice(i, i + q.length)) + '</b>' + esc(text.slice(i + q.length));
    }

    function loadPopular(cb) {
        if (popularCache) { cb(popularCache); return; }
        fetch('/Home/PopularSearches').then(function (r) { return r.json(); }).then(function (d) { popularCache = d || []; cb(popularCache); }).catch(function () { cb([]); });
    }

    function setupBox(input) {
        if (!input || input.dataset.searchReady) return;
        input.dataset.searchReady = '1';
        var box = input.closest('.search-box') || input.parentElement;
        box.style.position = 'relative';

        var dd = document.createElement('div');
        dd.className = 'mono-search-dd';
        dd.style.cssText = 'position:absolute;top:calc(100% + 8px);left:0;right:0;background:#fff;border:1px solid #eee;border-radius:14px;box-shadow:0 16px 40px rgba(0,0,0,.12);z-index:9999;overflow:hidden;display:none;max-height:70vh;overflow-y:auto;';
        box.appendChild(dd);

        var debounce, items = [], activeIdx = -1;

        function close() { dd.style.display = 'none'; activeIdx = -1; }
        function open() { dd.style.display = 'block'; }

        function render(html) { dd.innerHTML = html; open(); items = Array.prototype.slice.call(dd.querySelectorAll('[data-go]')); activeIdx = -1; }

        function renderEmpty() {
            var h = getHistory();
            var html = '';
            if (h.length) {
                html += '<div style="display:flex;justify-content:space-between;align-items:center;padding:12px 16px 6px;font-size:11px;letter-spacing:1px;color:#999;">'
                    + '<span><i class="ti ti-history"></i> TÌM KIẾM GẦN ĐÂY</span>'
                    + '<span id="msClear" style="cursor:pointer;color:#d4537e;">Xóa</span></div>';
                h.forEach(function (t) {
                    html += '<div data-go="' + esc(t) + '" style="display:flex;align-items:center;gap:10px;padding:9px 16px;cursor:pointer;font-size:14px;">'
                        + '<i class="ti ti-clock" style="color:#bbb;"></i>' + esc(t) + '</div>';
                });
            }
            html += '<div style="padding:12px 16px 6px;font-size:11px;letter-spacing:1px;color:#999;"><i class="ti ti-flame"></i> XU HƯỚNG TÌM KIẾM</div><div id="msPopular" style="padding:0 12px 12px;display:flex;flex-wrap:wrap;gap:8px;">Đang tải...</div>';
            render(html);

            var clr = dd.querySelector('#msClear');
            if (clr) clr.addEventListener('mousedown', function (e) { e.preventDefault(); clearHistory(); renderEmpty(); });

            loadPopular(function (list) {
                var pop = dd.querySelector('#msPopular');
                if (!pop) return;
                if (!list.length) { pop.innerHTML = '<span style="color:#aaa;font-size:13px;">Chưa có dữ liệu</span>'; return; }
                pop.innerHTML = list.map(function (t) {
                    return '<span data-go="' + esc(t) + '" style="padding:7px 13px;background:#f5f5f3;border-radius:999px;font-size:13px;cursor:pointer;">' + esc(t) + '</span>';
                }).join('');
                items = Array.prototype.slice.call(dd.querySelectorAll('[data-go]'));
                bindGo();
            });
            bindGo();
        }

        function renderResults(q, data) {
            var html = '';
            if (data.categories && data.categories.length) {
                html += '<div style="padding:12px 16px 6px;font-size:11px;letter-spacing:1px;color:#999;">DANH MỤC</div>';
                data.categories.forEach(function (c) {
                    var url = c.slug === 'nam' ? '/Home/Nam' : (c.slug === 'nu' ? '/Home/Nu' : '/Home/Search?q=' + encodeURIComponent(c.name));
                    html += '<a href="' + url + '" style="display:flex;align-items:center;gap:10px;padding:9px 16px;font-size:14px;color:#1a1a1a;"><i class="ti ti-category" style="color:#d4537e;"></i>' + highlight(c.name, q) + '</a>';
                });
            }
            if (data.products && data.products.length) {
                html += '<div style="padding:12px 16px 6px;font-size:11px;letter-spacing:1px;color:#999;">SẢN PHẨM</div>';
                data.products.forEach(function (p) {
                    html += '<a href="/Home/CTSP/' + p.id + '" style="display:flex;align-items:center;gap:12px;padding:10px 16px;color:#1a1a1a;">'
                        + '<div style="width:38px;height:38px;background:#f1efe8;border-radius:8px;display:flex;align-items:center;justify-content:center;flex-shrink:0;"><i class="ti ti-shirt" style="color:rgba(0,0,0,.25);"></i></div>'
                        + '<div style="flex:1;min-width:0;"><div style="font-size:14px;font-weight:500;white-space:nowrap;overflow:hidden;text-overflow:ellipsis;">' + highlight(p.name, q) + '</div>'
                        + '<div style="font-size:12px;color:#888;">' + esc(p.category) + '</div></div>'
                        + '<div style="font-size:14px;font-weight:600;color:#d4537e;">' + fmt(p.price) + '</div></a>';
                });
            }
            if (!html) {
                html = '<div style="padding:20px 16px;text-align:center;color:#999;font-size:14px;">Không có gợi ý cho "<b>' + esc(q) + '</b>"</div>';
            }
            html += '<div data-go="' + esc(q) + '" style="padding:12px 16px;border-top:1px solid #f0f0f0;font-size:14px;color:#d4537e;cursor:pointer;font-weight:500;"><i class="ti ti-search"></i> Xem tất cả kết quả cho "' + esc(q) + '"</div>';
            render(html);
            bindGo();
        }

        function bindGo() {
            dd.querySelectorAll('[data-go]').forEach(function (el) {
                el.addEventListener('mousedown', function (e) { e.preventDefault(); go(el.dataset.go); });
            });
        }

        function fetchSuggest(q) {
            fetch('/Home/SearchSuggest?q=' + encodeURIComponent(q))
                .then(function (r) { return r.json(); })
                .then(function (d) { renderResults(q, d); })
                .catch(function () { close(); });
        }

        input.addEventListener('input', function () {
            var q = input.value.trim();
            clearTimeout(debounce);
            if (!q) { renderEmpty(); return; }
            debounce = setTimeout(function () { fetchSuggest(q); }, 180);
        });

        input.addEventListener('focus', function () {
            var q = input.value.trim();
            if (!q) renderEmpty(); else fetchSuggest(q);
        });

        input.addEventListener('keydown', function (e) {
            if (dd.style.display === 'none') { if (e.key === 'Enter') go(input.value); return; }
            if (e.key === 'ArrowDown') { e.preventDefault(); activeIdx = Math.min(activeIdx + 1, items.length - 1); paint(); }
            else if (e.key === 'ArrowUp') { e.preventDefault(); activeIdx = Math.max(activeIdx - 1, -1); paint(); }
            else if (e.key === 'Enter') {
                e.preventDefault();
                if (activeIdx >= 0 && items[activeIdx]) {
                    var el = items[activeIdx];
                    if (el.dataset.go) go(el.dataset.go); else el.click();
                } else go(input.value);
            } else if (e.key === 'Escape') { close(); input.blur(); }
        });

        function paint() {
            items.forEach(function (el, i) { el.style.background = i === activeIdx ? '#f5f5f3' : ''; });
            if (activeIdx >= 0 && items[activeIdx]) items[activeIdx].scrollIntoView({ block: 'nearest' });
        }

        document.addEventListener('click', function (e) { if (!box.contains(e.target)) close(); });
    }

    function init() {
        // Bỏ qua ô tìm kiếm của trang Admin (lọc nội bộ) và import
        document.querySelectorAll('.search-box input').forEach(setupBox);
    }
    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', init);
    else init();
})();
