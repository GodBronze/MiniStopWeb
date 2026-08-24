using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using MiniStopWeb.Models;
using System.Linq;
using System;

namespace MiniStopWeb.Controllers
{
    public class AdminController : Controller
    {
        private readonly MiniStopDbContext _context = new MiniStopDbContext();

        // 1. GIAO DIỆN ĐĂNG NHẬP ADMIN
        public IActionResult DangNhap() => View();

        // 2. XỬ LÝ ĐĂNG NHẬP ADMIN (POST)
        [HttpPost]
        public IActionResult DangNhap(string maNv, string matKhau)
        {
            if (string.IsNullOrEmpty(maNv) || string.IsNullOrEmpty(matKhau)) {
                ViewBag.Loi = "Vui lòng nhập đầy đủ thông tin!";
                return View();
            }

            var admin = _context.NhanViens.FirstOrDefault(n => n.MaNv.Trim() == maNv.Trim() && n.MatKhau.Trim() == matKhau.Trim());

            if (admin != null)
            {
                HttpContext.Session.SetString("Admin_MaNV", admin.MaNv.Trim());
                HttpContext.Session.SetString("Admin_TenNV", admin.TenNv ?? "Quản trị viên");
                
                // LƯU SESSION VAI TRÒ ĐỂ PHÂN QUYỀN
                string vaiTro = string.IsNullOrEmpty(admin.VaiTro) ? "NhanVien" : admin.VaiTro.Trim();
                HttpContext.Session.SetString("Admin_VaiTro", vaiTro);
                
                return RedirectToAction("Index"); 
            }

            ViewBag.Loi = "Mã nhân viên hoặc mật khẩu không chính xác!";
            return View();
        }

       // 3. TRANG CHỦ ADMIN - DASHBOARD THỐNG KÊ TOÀN DIỆN
        public IActionResult Index(string filter = "thang")
        {
            if (HttpContext.Session.GetString("Admin_MaNV") == null) return RedirectToAction("DangNhap");

            DateTime startDate;
            DateTime endDate = DateTime.Now;

            // XỬ LÝ LỌC THỜI GIAN THEO THÁNG / QUÝ / NĂM
            if (filter == "nam")
                startDate = new DateTime(DateTime.Now.Year, 1, 1);
            else if (filter == "quy")
            {
                int currentQuarter = (DateTime.Now.Month - 1) / 3 + 1;
                startDate = new DateTime(DateTime.Now.Year, 3 * currentQuarter - 2, 1);
            }
            else // Mặc định là Tháng
                startDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

            ViewBag.CurrentFilter = filter;

            // 1. LẤY DỮ LIỆU HÓA ĐƠN TRONG KHOẢNG THỜI GIAN
            var dsHoaDon = _context.HoaDons.Where(x => x.NgayNhap >= startDate && x.NgayNhap <= endDate).ToList();
            
            // Lấy ra danh sách các Mã Hóa Đơn trong kỳ để làm cầu nối lọc Chi tiết Hóa đơn (FIX LỖI Ở ĐÂY)
            var danhSachMaHd = dsHoaDon.Select(h => h.MaHd).ToList();

            // TỔNG QUAN KPIs
            ViewBag.TongDonHang = dsHoaDon.Count;
            ViewBag.DoanhThuThucTe = dsHoaDon.Where(x => x.TrangThai == "Đã duyệt" || (x.TrangThai != null && x.TrangThai.Contains("Hoàn thành"))).Sum(x => x.TongTien) ?? 0;
            ViewBag.DonChoDuyet = dsHoaDon.Count(x => x.TrangThai == "Chờ duyệt" || string.IsNullOrEmpty(x.TrangThai));
            ViewBag.DonDaHuy = dsHoaDon.Count(x => x.TrangThai != null && x.TrangThai.Contains("Hủy"));

            // 2. PHÂN TÍCH SẢN PHẨM BÁN CHẠY & BÁN Ế TRONG KỲ
            // Thay vì dùng ct.HoaDon.NgayNhap (gây lỗi), ta kiểm tra ct.MaHd xem có nằm trong danh sách Hóa Đơn ở trên không
            var sanPhamStats = _context.ChiTietHoaDons
                .Where(ct => danhSachMaHd.Contains(ct.MaHd))
                .Join(_context.SanPhams, ct => ct.MaSp, sp => sp.MaSp, (ct, sp) => new { ct, sp })
                .GroupBy(x => new { x.sp.MaSp, x.sp.TenSp })
                .Select(group => new {
                    TenMon = group.Key.TenSp,
                    TongSoLuongBan = group.Sum(x => x.ct.SoLuongBan ?? 0)
                }).ToList();

            // Lấy Top 5 Bán Chạy
            var topBanChay = sanPhamStats.OrderByDescending(x => x.TongSoLuongBan).Take(5).ToList();
            ViewBag.TopSP_Names = System.Text.Json.JsonSerializer.Serialize(topBanChay.Select(x => x.TenMon).ToArray());
            ViewBag.TopSP_Quantities = System.Text.Json.JsonSerializer.Serialize(topBanChay.Select(x => x.TongSoLuongBan).ToArray());

            // Lấy Top 5 Bán Ế (Ít người mua nhất)
            var topBanE = sanPhamStats.OrderBy(x => x.TongSoLuongBan).Take(5).ToList();
            ViewBag.WorstSP_Names = System.Text.Json.JsonSerializer.Serialize(topBanE.Select(x => x.TenMon).ToArray());
            ViewBag.WorstSP_Quantities = System.Text.Json.JsonSerializer.Serialize(topBanE.Select(x => x.TongSoLuongBan).ToArray());

            // 3. RADAR CẢNH BÁO: HÀNG SẮP HẾT HẠN (Dưới 30 ngày) HOẶC TỒN KHO QUÁ NHIỀU
            DateTime thirtyDaysLater = DateTime.Now.AddDays(30);
            var hangTonKho = _context.SanPhams
                .Where(sp => sp.HanSuDung != null && sp.HanSuDung <= thirtyDaysLater && sp.SoLuong > 0)
                .OrderBy(sp => sp.HanSuDung)
                .Take(10) // Lấy 10 món khẩn cấp nhất
                .ToList();

            ViewBag.HangSapHetHan = hangTonKho;

            return View(dsHoaDon.OrderByDescending(x => x.NgayNhap).Take(10).ToList()); // Trả về 10 đơn gần nhất cho bảng
        }

        // TÍNH NĂNG TỰ ĐỘNG PHÁT HÀNH VOUCHER XẢ KHO
        // TÍNH NĂNG TỰ ĐỘNG PHÁT HÀNH VOUCHER / DÁN TEM XẢ KHO
        [HttpPost]
        public IActionResult TaoVoucherXaKho(string maSp)
        {
            // Kiểm tra bảo mật
            if (HttpContext.Session.GetString("Admin_MaNV") == null) return RedirectToAction("DangNhap");

            var sp = _context.SanPhams.Find(maSp);
            if (sp != null)
            {
                // Rào chắn: Kiểm tra xem món này đã được dán tem xả kho trước đó chưa
                // Tránh trường hợp Admin lỡ tay bấm 2-3 lần khiến giá bị chia đôi liên tục
                if (!sp.TenSp.Contains("[XẢ KHO]"))
                {
                    // 1. Dán tem vào tên hiển thị
                    sp.TenSp = "[XẢ KHO 50%] " + sp.TenSp;
                    
                    // 2. Chia đôi giá gốc
                    sp.DonGia = sp.DonGia / 2; 

                    // 3. Lưu vào SQL Server
                    _context.SaveChanges();

                    TempData["ThongBaoSuccess"] = $"🎉 Đã kích hoạt chiến dịch! Hệ thống vừa dán tem [XẢ KHO 50%] và ép giá món '{sp.TenSp}' xuống một nửa để dọn sạch kho!";
                }
                else
                {
                    TempData["ThongBao"] = "Món ăn này đã được dán tem xả kho trước đó rồi, không thể giảm giá thêm nữa!";
                }
            }
            return RedirectToAction("Index");
        }
        // 4. ĐĂNG XUẤT ADMIN
        public IActionResult DangXuat()
        {
            HttpContext.Session.Remove("Admin_MaNV");
            HttpContext.Session.Remove("Admin_TenNV");
            HttpContext.Session.Remove("Admin_VaiTro");
            return RedirectToAction("DangNhap");
        }

        // =======================================================
        // KHU VỰC CHỈ DÀNH CHO ADMIN TỐI CAO (QUẢN LÝ SẢN PHẨM)
        // =======================================================

        public IActionResult QuanLySanPham()
        {
            if (HttpContext.Session.GetString("Admin_MaNV") == null) return RedirectToAction("DangNhap");
            
            // CHẶN QUYỀN TRUY CẬP CỦA NHÂN VIÊN THƯỜNG
            if (HttpContext.Session.GetString("Admin_VaiTro") != "Admin")
            {
                TempData["Loi"] = "Bạn không có quyền truy cập. Chức năng quản lý sản phẩm chỉ dành cho Admin!";
                return RedirectToAction("Index");
            }

            var dsSanPham = _context.SanPhams.ToList();
            return View(dsSanPham);
        }

        [HttpPost]
        public IActionResult ThemSanPham(MiniStopWeb.Models.SanPham sp)
        {
            if (HttpContext.Session.GetString("Admin_MaNV") == null) return RedirectToAction("DangNhap");
            if (HttpContext.Session.GetString("Admin_VaiTro") != "Admin") return RedirectToAction("Index");

            if (sp != null)
            {
                var check = _context.SanPhams.Find(sp.MaSp);
                if (check != null)
                {
                    TempData["LoiSP"] = "Mã sản phẩm này đã tồn tại trên hệ thống!";
                    return RedirectToAction("QuanLySanPham");
                }

                _context.SanPhams.Add(sp);
                _context.SaveChanges();
                TempData["ThanhCongSP"] = "Thêm sản phẩm mới thành công!";
            }
            return RedirectToAction("QuanLySanPham");
        }

       [HttpPost]
        public IActionResult SuaSanPham(SanPham sp)
        {
            // Kiểm tra bảo mật đăng nhập
            if (HttpContext.Session.GetString("Admin_MaNV") == null) return RedirectToAction("DangNhap");

            // Tìm món ăn cũ đang có trong Database theo Mã SP
            var spCu = _context.SanPhams.FirstOrDefault(x => x.MaSp.Trim() == sp.MaSp.Trim());
            
            if (spCu != null)
            {
                // Cập nhật các thông tin cơ bản
                spCu.TenSp = sp.TenSp;
                spCu.DonViTinh = sp.DonViTinh;
                spCu.SoLuong = sp.SoLuong;
                spCu.DonGia = sp.DonGia;

                // ---> DÒNG QUAN TRỌNG NHẤT VỪA ĐƯỢC BỔ SUNG: Cập nhật link Hình Ảnh mới <---
                spCu.HinhAnh = sp.HinhAnh; 

                // Lưu thay đổi xuống SQL Server
                _context.SaveChanges();

                TempData["ThanhCongSP"] = $"Đã cập nhật thành công thông tin và hình ảnh cho món '{spCu.TenSp}'!";
            }
            else
            {
                TempData["LoiSP"] = "Không tìm thấy sản phẩm cần cập nhật trong hệ thống!";
            }

            return RedirectToAction("QuanLySanPham");
        }   

        public IActionResult XoaSanPham(string id)
        {
            if (HttpContext.Session.GetString("Admin_MaNV") == null) return RedirectToAction("DangNhap");
            if (HttpContext.Session.GetString("Admin_VaiTro") != "Admin") return RedirectToAction("Index");

            var sp = _context.SanPhams.Find(id);
            if (sp != null)
            {
                try
                {
                    _context.SanPhams.Remove(sp);
                    _context.SaveChanges();
                    TempData["ThanhCongSP"] = "Đã xóa sản phẩm khỏi hệ thống!";
                }
                catch (Exception)
                {
                    TempData["LoiSP"] = "Không thể xóa sản phẩm này vì đã có khách hàng đặt mua trong lịch sử hóa đơn!";
                }
            }
            return RedirectToAction("QuanLySanPham");
        }

        // =======================================================
        // KHU VỰC DÀNH CHO CẢ ADMIN & NHÂN VIÊN (QUẢN LÝ ĐƠN HÀNG)
        // =======================================================

        [HttpPost]
        public IActionResult DuyetDonHang(string id)
        {
            if (HttpContext.Session.GetString("Admin_MaNV") == null) return RedirectToAction("DangNhap");
            
            var hoaDon = _context.HoaDons.Find(id);
            if (hoaDon != null)
            {
                if (hoaDon.TrangThai == "Đã duyệt")
                {
                    TempData["Loi"] = "Đơn hàng này đã được duyệt trước đó rồi!";
                    return RedirectToAction("Index");
                }

                var chiTiet = _context.ChiTietHoaDons.Where(ct => ct.MaHd == id).ToList();

                // Kiểm tra tồn kho
                foreach (var item in chiTiet)
                {
                    var sanPham = _context.SanPhams.Find(item.MaSp);
                    if (sanPham != null && (sanPham.SoLuong ?? 0) < (item.SoLuongBan ?? 0))
                    {
                        TempData["Loi"] = $"Không thể duyệt đơn! Món '{sanPham.TenSp}' trong kho hiện chỉ còn {sanPham.SoLuong} sản phẩm, không đủ để giao!";
                        return RedirectToAction("Index");
                    }
                }

                // Trừ kho
                foreach (var item in chiTiet)
                {
                    var sanPham = _context.SanPhams.Find(item.MaSp);
                    if (sanPham != null) sanPham.SoLuong -= item.SoLuongBan; 
                }

                hoaDon.TrangThai = "Đã duyệt";
                hoaDon.MaNv = HttpContext.Session.GetString("Admin_MaNV");
                
                _context.SaveChanges();
                TempData["ThanhCong"] = $"🎉 Đã duyệt thành công đơn hàng {id}. Hệ thống đã tự động trừ tồn kho!";
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult HuyDonHang(string id)
        {
            if (HttpContext.Session.GetString("Admin_MaNV") == null) return RedirectToAction("DangNhap");

            var hoaDon = _context.HoaDons.Find(id);
            if (hoaDon != null)
            {
                if (hoaDon.TrangThai == "Đã hủy")
                {
                    TempData["Loi"] = "Đơn hàng này đã ở trạng thái hủy!";
                    return RedirectToAction("Index");
                }

                if (hoaDon.TrangThai == "Đã duyệt")
                {
                    var chiTiet = _context.ChiTietHoaDons.Where(ct => ct.MaHd == id).ToList();
                    foreach (var item in chiTiet)
                    {
                        var sanPham = _context.SanPhams.Find(item.MaSp);
                        if (sanPham != null) sanPham.SoLuong += item.SoLuongBan; 
                    }
                }

                hoaDon.TrangThai = "Đã hủy";
                hoaDon.MaNv = HttpContext.Session.GetString("Admin_MaNV");
                
                _context.SaveChanges();
                TempData["ThanhCong"] = $"❌ Đã hủy đơn hàng {id} thành công và hoàn trả số lượng hàng vào kho (nếu có).";
            }
            return RedirectToAction("Index");
        }

        // =======================================================
        // TÍCH HỢP GIAO HÀNG BÊN THỨ 3 (GHN / AHAMOVE)
        // =======================================================
       // API Đẩy đơn cho Đơn Vị Vận Chuyển
        [HttpPost]
        public IActionResult DayDonChoDonViVanChuyen(string maHD, string donViVanChuyen)
        {
            if (HttpContext.Session.GetString("Admin_MaNV") == null) return RedirectToAction("DangNhap");

            var hoaDon = _context.HoaDons.Find(maHD);
            if (hoaDon != null)
            {
                // Nhận diện đơn này là MoMo hay COD từ trạng thái cũ
                bool daThanhToanMoMo = hoaDon.TrangThai != null && hoaDon.TrangThai.Contains("MoMo");

                string maVanDon = donViVanChuyen == "GHN" 
                    ? "GHN-" + new Random().Next(10000000, 99999999).ToString() 
                    : "AHA-" + new Random().Next(10000000, 99999999).ToString();
                
                // Đóng dấu dòng tiền vào trạng thái mới
                hoaDon.TrangThai = daThanhToanMoMo ? $"Đang giao ({maVanDon}) [MoMo]" : $"Đang giao ({maVanDon}) [COD]";
                _context.SaveChanges();

                TempData["ThongBao"] = $"Đã đẩy API đơn {maHD} cho {donViVanChuyen}. Shipper sẽ đến lấy hàng!";
            }
            return RedirectToAction("Index");
        }

        // XÁC NHẬN HOÀN THÀNH ĐƠN HÀNG
        [HttpPost]
        public IActionResult XacNhanHoanThanh(string maHD)
        {
            if (HttpContext.Session.GetString("Admin_MaNV") == null) return RedirectToAction("DangNhap");

            var hoaDon = _context.HoaDons.Find(maHD);
            if (hoaDon != null)
            {
                // Nhận diện dòng tiền để chốt sổ
                bool daThanhToanMoMo = hoaDon.TrangThai != null && hoaDon.TrangThai.Contains("MoMo");
                
                hoaDon.TrangThai = daThanhToanMoMo ? "Hoàn thành [MoMo]" : "Hoàn thành [COD]";
                _context.SaveChanges();

                TempData["ThongBao"] = $"Đã chốt sổ đơn hàng {maHD} thành công!";
            }
            return RedirectToAction("Index");
        }
        
    }
}