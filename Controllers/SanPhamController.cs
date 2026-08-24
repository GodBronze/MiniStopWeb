using Microsoft.AspNetCore.Mvc;
using MiniStopWeb.Models;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace MiniStopWeb.Controllers
{
    public class SanPhamController : Controller
    {
        private readonly MiniStopDbContext _context = new MiniStopDbContext();

        // Hàm hỗ trợ: Chuyển tiếng Việt có dấu thành không dấu (Ví dụ: "Bánh mì" -> "Banh mi")
        private string XoaDauTiengViet(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "";
            text = text.ToLower().Trim();
            string[] array1 = new string[] { "á", "à", "ả", "ã", "ạ", "â", "ấ", "ầ", "ẩ", "ẫ", "ậ", "ă", "ắ", "ằ", "ẳ", "ẵ", "ặ", "đ", "é", "è", "ẻ", "ẽ", "ẹ", "ê", "ế", "ề", "ể", "ễ", "ệ", "í", "ì", "ỉ", "ĩ", "ị", "ó", "ò", "ỏ", "õ", "ọ", "ô", "ố", "ồ", "ổ", "ỗ", "ộ", "ơ", "ớ", "ờ", "ở", "ỡ", "ợ", "ú", "ù", "ủ", "ũ", "ụ", "ư", "ứ", "ừ", "ử", "ữ", "ự", "ý", "ỳ", "ỷ", "ỹ", "ỵ" };
            string[] array2 = new string[] { "a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "d", "e", "e", "e", "e", "e", "e", "e", "e", "e", "e", "e", "i", "i", "i", "i", "i", "o", "o", "o", "o", "o", "o", "o", "o", "o", "o", "o", "o", "o", "o", "o", "o", "o", "u", "u", "u", "u", "u", "u", "u", "u", "u", "u", "u", "y", "y", "y", "y", "y" };
            for (int i = 0; i < array1.Length; i++)
            {
                text = text.Replace(array1[i], array2[i]);
            }
            return text;
        }

        // 1. TRANG HIỂN THỊ VÀ LỌC SẢN PHẨM (Đã thêm minPrice, maxPrice)
        public IActionResult Index(string? maDm, string? keyword, string? sort, decimal? minPrice, decimal? maxPrice)
        {
            ViewBag.DanhMucs = _context.DanhMucs.ToList();
            ViewBag.MaDmHienTai = maDm;
            ViewBag.KeywordHienTai = keyword;
            ViewBag.SortHienTai = sort; 
            ViewBag.MinPrice = minPrice; // Lưu lại để hiển thị trên UI
            ViewBag.MaxPrice = maxPrice;

            var query = _context.SanPhams.AsQueryable();

            // A. Lọc theo Danh mục
            if (!string.IsNullOrEmpty(maDm))
            {
                query = query.Where(sp => sp.MaDm.Trim() == maDm.Trim());
            }

            // B. TÌM KIẾM TIẾNG VIỆT KHÔNG DẤU
            if (!string.IsNullOrEmpty(keyword))
            {
                string keywordKhongDau = XoaDauTiengViet(keyword);
                query = query.Where(sp => sp.TenKhongDau.Contains(keywordKhongDau) || sp.TenSp.Contains(keyword));
            }

            // C. LỌC THEO KHOẢNG GIÁ (TÍNH NĂNG MỚI)
            if (minPrice.HasValue)
            {
                query = query.Where(sp => (sp.GiaKhuyenMai ?? sp.DonGia) >= minPrice.Value);
            }
            if (maxPrice.HasValue)
            {
                query = query.Where(sp => (sp.GiaKhuyenMai ?? sp.DonGia) <= maxPrice.Value);
            }

            // D. SẮP XẾP SẢN PHẨM
            switch (sort)
            {
                case "noibat": query = query.Where(sp => sp.IsNoiBat == true); break;
                case "banchay": query = query.OrderByDescending(sp => sp.ChiTietHoaDons.Sum(ct => ct.SoLuongBan)); break;
                case "giamgia": query = query.Where(sp => sp.GiaKhuyenMai != null && sp.GiaKhuyenMai < sp.DonGia); break;
                case "moi": query = query.OrderByDescending(sp => sp.NgayTao); break;
                case "giatang": query = query.OrderBy(sp => sp.GiaKhuyenMai ?? sp.DonGia); break;
                case "giagiam": query = query.OrderByDescending(sp => sp.GiaKhuyenMai ?? sp.DonGia); break;
                default: query = query.OrderByDescending(sp => sp.NgayTao); break;
            }

            var dsSanPham = query.ToList();
            return View(dsSanPham);
        }

        // 2. TÌM KIẾM NHẢY CÓC (LIVE SEARCH KHÔNG DẤU)
        [HttpGet]
        public IActionResult TimKiemNhanh(string keyword)
        {
            if (string.IsNullOrEmpty(keyword)) return Json(new List<object>());

            string keywordKhongDau = XoaDauTiengViet(keyword);

            var results = _context.SanPhams
                .Where(sp => sp.TenKhongDau.Contains(keywordKhongDau) || sp.TenSp.Contains(keyword))
                .Take(5)
                .Select(sp => new {
                    maSp = sp.MaSp.Trim(),
                    tenSp = sp.TenSp,
                    // Lấy giá khuyến mãi nếu có, không thì lấy giá gốc
                    donGia = sp.GiaKhuyenMai ?? sp.DonGia 
                })
                .ToList();

            return Json(results);
        }
        // 3. TRANG CHI TIẾT SẢN PHẨM
        public IActionResult ChiTiet(string id)
        {
            if (string.IsNullOrEmpty(id)) return RedirectToAction("Index");

            // 1. Tìm sản phẩm theo ID (Đã thêm Trim() để tránh lỗi khoảng trắng trong SQL)
            var sanPham = _context.SanPhams.FirstOrDefault(sp => sp.MaSp.Trim() == id.Trim());
            
            if (sanPham == null) return RedirectToAction("Index");

            // 2. SẢN PHẨM CÙNG DANH MỤC (Giữ nguyên của bạn để hiển thị khối "Sản phẩm liên quan")
            ViewBag.SanPhamLienQuan = _context.SanPhams
                .Where(sp => sp.MaDm == sanPham.MaDm && sp.MaSp.Trim() != sanPham.MaSp.Trim())
                .Take(4)
                .ToList();

            // =========================================================================
            // 3. THUẬT TOÁN HUI (HIGH-UTILITY ITEMSET MINING): KHAI PHÁ LỢI ÍCH CAO
            // =========================================================================
            
            // Bước 3.1: Lấy danh sách Mã Hóa Đơn (MaHd) đã từng chứa món ăn này (Món A)
            var cacHoaDonChuaMonNay = _context.ChiTietHoaDons
                .Where(ct => ct.MaSp.Trim() == sanPham.MaSp.Trim())
                .Select(ct => ct.MaHd)
                .Distinct()
                .ToList();

            // Bước 3.2: Quét trong các hóa đơn đó, tính TỔNG LỢI ÍCH (Utility) của các món KHÁC (Món B, C...)
            var topMonMuaKemHUI = _context.ChiTietHoaDons
                .Where(ct => cacHoaDonChuaMonNay.Contains(ct.MaHd) && ct.MaSp.Trim() != sanPham.MaSp.Trim())
                .GroupBy(ct => ct.MaSp)
                .Select(group => new { 
                    MaSp = group.Key, 
                    SupportCount = group.Count(), // Độ phổ biến (Số lần xuất hiện cùng)
                    // Công thức HUI: Tổng (Số lượng bán * Đơn giá bán) trong các hóa đơn mua cùng
                    TotalUtility = group.Sum(ct => (ct.SoLuongBan ?? 0) * (ct.DonGia ?? 0)) 
                })
                // Sắp xếp ưu tiên theo TỔNG LỢI ÍCH KINH TẾ (TotalUtility) giảm dần thay vì SupportCount
                .OrderByDescending(x => x.TotalUtility) 
                .Take(4) // Lấy Top 4 món đem lại lợi ích cao nhất
                .Select(x => x.MaSp)
                .ToList();

            // Bước 3.3: Lấy thông tin chi tiết của 4 món gợi ý từ bảng SanPham để truyền sang View
            ViewBag.GoiYApriori = _context.SanPhams
                .Where(sp => topMonMuaKemHUI.Contains(sp.MaSp))
                .ToList();
            // =========================================================================

            return View(sanPham);
        }
    }
}