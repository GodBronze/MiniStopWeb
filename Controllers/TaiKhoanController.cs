using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using MiniStopWeb.Models;
using System;
using System.Linq;

namespace MiniStopWeb.Controllers
{
    public class TaiKhoanController : Controller
    {
        // Tự khởi tạo trực tiếp DB Context giống hệt bên Giỏ Hàng
        private readonly MiniStopDbContext _context = new MiniStopDbContext();

        // 1. GIAO DIỆN ĐĂNG NHẬP
        public IActionResult DangNhap() => View();
        
        // ... (các hàm DangNhap, DangKy bên dưới bạn vẫn giữ nguyên nhé)

        // XL ĐĂNG NHẬP (POST)
        [HttpPost]
        public IActionResult DangNhap(string sdt, string matKhau)
        {
            if (string.IsNullOrEmpty(sdt) || string.IsNullOrEmpty(matKhau)) {
                ViewBag.Loi = "Vui lòng nhập đầy đủ thông tin!";
                return View();
            }

            // Tìm khách hàng khớp SĐT và Mật khẩu (Dùng Trim() chống lỗi khoảng trắng)
            var khachHang = _context.KhachHangs.FirstOrDefault(k => k.Sdt.Trim() == sdt.Trim() && k.MatKhau.Trim() == matKhau.Trim());

            if (khachHang != null)
            {
                // Lưu ID và Tên khách hàng vào Session để dùng ở các trang khác
                HttpContext.Session.SetString("MaKh", khachHang.MaKh.Trim());
                HttpContext.Session.SetString("TenKh", khachHang.TenKh ?? "Khách Hàng");
                
                return RedirectToAction("Index", "Home"); // Đăng nhập xong đẩy về trang chủ
            }

            ViewBag.Loi = "Số điện thoại hoặc Mật khẩu không chính xác!";
            return View();
        }

        // 2. GIAO DIỆN ĐĂNG KÝ
        public IActionResult DangKy() => View();

        // XL ĐĂNG KÝ (POST)
        [HttpPost]
        public IActionResult DangKy(string tenKh, string sdt, string diaChi, string matKhau)
        {
            var checkGhung = _context.KhachHangs.FirstOrDefault(k => k.Sdt.Trim() == sdt.Trim());
            if (checkGhung != null)
            {
                ViewBag.Loi = "Số điện thoại này đã được đăng ký cho tài khoản khác!";
                return View();
            }

            // Tự sinh mã khách hàng ngẫu nhiên KHxxxxxx (Đảm bảo độ dài của Khóa chính)
            string maKHMoi = "KH" + new Random().Next(100000, 999999).ToString();

            var kh = new KhachHang
            {
                MaKh = maKHMoi,
                TenKh = tenKh,
                Sdt = sdt,
                DiaChi = diaChi,
                MatKhau = matKhau,
                DiemTichLuy = 0 // Tài khoản mới khởi tạo điểm bằng 0
            };

            _context.KhachHangs.Add(kh);
            _context.SaveChanges();

            return RedirectToAction("DangNhap"); // Đăng ký xong tự chuyển sang trang Đăng nhập
        }

        // 3. ĐĂNG XUẤT
        public IActionResult DangXuat()
        {
            HttpContext.Session.Remove("MaKh");
            HttpContext.Session.Remove("TenKh");
            return RedirectToAction("Index", "Home");
        }
    }
}