using Microsoft.AspNetCore.Mvc;
using MiniStopWeb.Models;
using System.Text.Json; 
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text;
using System.Security.Cryptography;

namespace MiniStopWeb.Controllers
{
    public class GioHangController : Controller
    {
        private readonly MiniStopDbContext _context = new MiniStopDbContext();

        // ==========================================
        // HÀM PHỤ TRỢ: LẤY VÀ LƯU GIỎ HÀNG
        // ==========================================
        private List<CartItem> LayGioHang()
        {
            var sessionCart = HttpContext.Session.GetString("GioHang");
            if (sessionCart == null) return new List<CartItem>(); 
            return JsonSerializer.Deserialize<List<CartItem>>(sessionCart) ?? new List<CartItem>();
        }

        private void LuuGioHang(List<CartItem> gioHang)
        {
            var jsonCart = JsonSerializer.Serialize(gioHang);
            HttpContext.Session.SetString("GioHang", jsonCart);
        }

        // ==========================================
        // CÁC CHỨC NĂNG CƠ BẢN CỦA GIỎ HÀNG
        // ==========================================
        public IActionResult Index()
        {
            var gioHang = LayGioHang(); 
            return View(gioHang);       
        }

        [HttpPost]
        public IActionResult ThemVaoGio(string id)
        {
            string? maKhachHang = HttpContext.Session.GetString("MaKh");
            if (string.IsNullOrEmpty(maKhachHang))
                return Json(new { success = false, requiresLogin = true, message = "Bạn cần đăng nhập để thêm sản phẩm vào giỏ hàng!" });

            if (string.IsNullOrEmpty(id)) return Json(new { success = false, message = "Không tìm thấy mã sản phẩm!" });

            id = id.Trim(); 
            var gioHang = LayGioHang();
            var item = gioHang.FirstOrDefault(p => p.MaSp.Trim() == id);

            if (item != null)
            {
                item.SoLuong++; 
            }
            else
            {
                var sanPham = _context.SanPhams.FirstOrDefault(p => p.MaSp.Trim() == id);
                if (sanPham != null)
                {
                    gioHang.Add(new CartItem { MaSp = sanPham.MaSp.Trim(), TenSp = sanPham.TenSp ?? "", DonGia = sanPham.DonGia ?? 0, SoLuong = 1 });
                }
                else return Json(new { success = false, message = "Sản phẩm không tồn tại!" });
            }

            LuuGioHang(gioHang); 
            return Json(new { success = true, message = "Đã thêm sản phẩm vào giỏ hàng thành công!" });
        }

        public IActionResult XoaKhoiGio(string id)
        {
            if (string.IsNullOrEmpty(id)) return RedirectToAction("Index");
            var gioHang = LayGioHang();
            var item = gioHang.FirstOrDefault(p => p.MaSp.Trim() == id.Trim());
            if (item != null) { gioHang.Remove(item); LuuGioHang(gioHang); }
            return RedirectToAction("Index"); 
        }

        public IActionResult TangSoLuong(string id)
        {
            if (string.IsNullOrEmpty(id)) return RedirectToAction("Index");
            var gioHang = LayGioHang();
            var item = gioHang.FirstOrDefault(p => p.MaSp.Trim() == id.Trim());
            if (item != null) { item.SoLuong++; LuuGioHang(gioHang); }
            return RedirectToAction("Index");
        }

        public IActionResult GiamSoLuong(string id)
        {
            if (string.IsNullOrEmpty(id)) return RedirectToAction("Index");
            var gioHang = LayGioHang();
            var item = gioHang.FirstOrDefault(p => p.MaSp.Trim() == id.Trim());
            if (item != null) { item.SoLuong--; if (item.SoLuong <= 0) gioHang.Remove(item); LuuGioHang(gioHang); }
            return RedirectToAction("Index");
        }

        // ==========================================
        // LUỒNG THANH TOÁN & ĐẶT HÀNG
        // ==========================================
        [HttpGet]
        public IActionResult ThanhToan()
        {
            string? maKhachHang = HttpContext.Session.GetString("MaKh");
            if (string.IsNullOrEmpty(maKhachHang)) return RedirectToAction("DangNhap", "TaiKhoan"); 

            var gioHang = LayGioHang();
            if (gioHang == null || gioHang.Count == 0) return RedirectToAction("Index", "SanPham"); 

            ViewBag.PhiTamTinh = gioHang.Sum(x => x.ThanhTien);
            return View(gioHang);
        }

        [HttpPost]
        public async Task<IActionResult> XuLyThanhToan(string TenNguoiNhan, string SoDienThoai, string DiaChi, decimal PhiShip, string PhuongThucTT)
        {
            string? maKhachHang = HttpContext.Session.GetString("MaKh");
            if (string.IsNullOrEmpty(maKhachHang)) return RedirectToAction("DangNhap", "TaiKhoan"); 

            var gioHang = LayGioHang();
            if (gioHang == null || gioHang.Count == 0) return RedirectToAction("Index", "SanPham");

            string maHDMoi = "HD" + new Random().Next(10000000, 99999999).ToString();
            decimal phiNenTang = 2000;
            decimal tongTienHang = gioHang.Sum(x => x.ThanhTien);
            decimal tongThanhToan = tongTienHang + PhiShip + phiNenTang;

            // 1. LƯU HÓA ĐƠN VÀO CƠ SỞ DỮ LIỆU SQL SERVER
            var hoaDon = new HoaDon
            {
                MaHd = maHDMoi,
                MaKh = maKhachHang, 
                NgayNhap = DateTime.Now, 
                TongTien = tongThanhToan,
                TrangThai = (PhuongThucTT == "MOMO") ? "Chờ thanh toán MoMo" : "Chờ duyệt"
            };
            _context.HoaDons.Add(hoaDon);

            // 2. LƯU CHI TIẾT HÓA ĐƠN
            foreach (var item in gioHang)
            {
                var chiTiet = new ChiTietHoaDon { MaHd = maHDMoi, MaSp = item.MaSp.Trim(), SoLuongBan = item.SoLuong, DonGia = item.DonGia };
                _context.ChiTietHoaDons.Add(chiTiet);
            }
            
            // LƯU DATA MỘT LẦN DUY NHẤT Ở ĐÂY
            _context.SaveChanges(); 

            // ==============================================
            // GỌI CỔNG THANH TOÁN MOMO SANDBOX
            // ==============================================
            if (PhuongThucTT == "MOMO") 
            {
                // LƯU Ý: NẾU BẠN CHẠY CỔNG KHÁC 5280 THÌ HÃY SỬA CON SỐ NÀY NHÉ
                string returnUrl = "https://ezequiel-nonfarcical-carole.ngrok-free.dev/GioHang/MoMoReturn"; 
                
                string endpoint = "https://test-payment.momo.vn/v2/gateway/api/create";
                string partnerCode = "MOMOQFSH20250717_TEST";
                string accessKey = "m1rfCAFskm5T7ec6";
                string secretKey = "JSyZ4UGLYE5lEX1oZIOTJwVvTtVPz4G2";
                string orderInfo = "Thanh toan don hang MiniStop " + maHDMoi;
                string requestType = "captureWallet";
                
                // Ép kiểu số tiền về Số Nguyên
                long amountNumber = (long)tongThanhToan; 
                string amountString = amountNumber.ToString(); 

                string orderId = maHDMoi;
                string requestId = Guid.NewGuid().ToString();
                string extraData = "";

                // Băm chữ ký bằng chuỗi gốc
                string rawHash = $"accessKey={accessKey}&amount={amountString}&extraData={extraData}&ipnUrl={returnUrl}&orderId={orderId}&orderInfo={orderInfo}&partnerCode={partnerCode}&redirectUrl={returnUrl}&requestId={requestId}&requestType={requestType}";
                string signature = ComputeHmacSha256(rawHash, secretKey);

                // Tạo JSON gửi đi
                var message = new
                {
                    partnerCode = partnerCode,
                    partnerName = "MiniStop",
                    storeId = "MomoTestStore",
                    requestId = requestId,
                    amount = amountNumber, 
                    orderId = orderId,
                    orderInfo = orderInfo,
                    redirectUrl = returnUrl,
                    ipnUrl = returnUrl,
                    lang = "vi",
                    extraData = extraData,
                    requestType = requestType,
                    signature = signature
                };

                using (HttpClient client = new HttpClient())
                {
                    var jsonMessage = JsonSerializer.Serialize(message);
                    var content = new StringContent(jsonMessage, Encoding.UTF8, "application/json");
                    var response = await client.PostAsync(endpoint, content);
                    var responseString = await response.Content.ReadAsStringAsync();
                    
                    using (JsonDocument doc = JsonDocument.Parse(responseString))
                    {
                        if (doc.RootElement.TryGetProperty("payUrl", out JsonElement payUrlElement))
                        {
                            // TẠO QR THÀNH CÔNG -> MỚI XÓA GIỎ HÀNG VÀ CHUYỂN TRANG
                            HttpContext.Session.Remove("GioHang"); 
                            return Redirect(payUrlElement.GetString()); 
                        }
                        else
                        {
                            // NẾU LỖI -> HIỂN THỊ THẲNG RA MÀN HÌNH ĐỂ DEBUG
                            return Content("MOMO TỪ CHỐI GIAO DỊCH. Lỗi chi tiết từ máy chủ MoMo: \n" + responseString, "application/json");
                        }
                    }
                }
            }

            // ==============================================
            // THANH TOÁN BẰNG TIỀN MẶT (COD)
            // ==============================================
            HttpContext.Session.Remove("GioHang");
            TempData["ThongBao"] = "Đặt hàng thành công! Đơn hàng sẽ được giao đến bạn sớm nhất.";
            return View("ThanhToanThanhCong");
        }

        // ==========================================
        // HÀM HỨNG KẾT QUẢ TỪ MOMO TRẢ VỀ SAU KHI THANH TOÁN
        // ==========================================
        public IActionResult MoMoReturn()
        {
            string resultCode = Request.Query["resultCode"];
            string orderId = Request.Query["orderId"];

            var hoaDon = _context.HoaDons.Find(orderId);

            if (resultCode == "0") // 0 là mã thành công của MoMo
            {
                if (hoaDon != null)
                {
                    hoaDon.TrangThai = "Đã thanh toán (MoMo)"; 
                    _context.SaveChanges();
                }
                TempData["ThongBao"] = $"Giao dịch thành công! Đã thanh toán trực tuyến qua Ví MoMo cho mã đơn {orderId}.";
            }
            else
            {
                if (hoaDon != null)
                {
                    hoaDon.TrangThai = "Lỗi thanh toán / Hủy"; 
                    _context.SaveChanges();
                }
                TempData["Loi"] = "Giao dịch thất bại hoặc đã bị khách hàng hủy trên ứng dụng MoMo!";
            }
            return View("ThanhToanThanhCong");
        }

        // ==========================================
        // THUẬT TOÁN TẠO CHỮ KÝ BẢO MẬT HMAC-SHA256 CHO MOMO
        // ==========================================
        private string ComputeHmacSha256(string message, string secretKey)
        {
            byte[] keyByte = Encoding.UTF8.GetBytes(secretKey);
            byte[] messageBytes = Encoding.UTF8.GetBytes(message);
            using (var hmacsha256 = new HMACSHA256(keyByte))
            {
                byte[] hashmessage = hmacsha256.ComputeHash(messageBytes);
                string hex = BitConverter.ToString(hashmessage);
                hex = hex.Replace("-", "").ToLower();
                return hex;
            }
        }
    }
}