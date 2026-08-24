using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;

namespace MiniStopWeb.Controllers
{
    public class AdminChatController : Controller
    {
        // Chuỗi kết nối SQL Server chuẩn của bạn
        private readonly string _connectionString = "Data Source=localhost\\MINHTHANG;Initial Catalog=MiniStopDB;Integrated Security=True;TrustServerCertificate=True";

        // 1. Trả về giao diện trang quản lý Chat
        public IActionResult Index()
        {
            // (Tùy chọn) Kiểm tra đăng nhập
            // if (HttpContext.Session.GetString("Admin_MaNV") == null) return RedirectToAction("DangNhap", "Admin");
            return View();
        }

        // 2. Lấy danh sách khách hàng (ĐÃ NÂNG CẤP: ĐẾM TIN NHẮN CHƯA ĐỌC)
        [HttpGet]
        public IActionResult GetDanhSachKhachHang()
        {
            var list = new List<object>();
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                // Gom nhóm theo mã khách, đếm số tin chưa đọc (UnreadCount)
                string query = @"SELECT MaKh, MAX(TenKh) AS TenKh, 
                                        SUM(CASE WHEN IsAdmin = 0 AND IsRead = 0 THEN 1 ELSE 0 END) AS UnreadCount
                                 FROM ChatSupport 
                                 GROUP BY MaKh 
                                 ORDER BY MAX(CreatedAt) DESC";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new {
                                maKh = reader.GetString(0),
                                tenKh = reader.GetString(1),
                                unreadCount = reader.GetInt32(2) // Lấy số tin nhắn chưa đọc
                            });
                        }
                    }
                }
            }
            return Json(list);
        }

        // 3. Lấy chi tiết đoạn chat (ĐÃ NÂNG CẤP: ĐÁNH DẤU "ĐÃ ĐỌC" KHI BẤM VÀO)
        [HttpGet]
        public IActionResult GetChiTietChat(string maKh)
        {
            var messages = new List<object>();
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                
                // Cập nhật trạng thái "Đã đọc" ngay khi Admin bấm vào xem tin nhắn
                string updateQuery = "UPDATE ChatSupport SET IsRead = 1 WHERE MaKh = @MaKh AND IsAdmin = 0";
                using (SqlCommand updateCmd = new SqlCommand(updateQuery, conn))
                {
                    updateCmd.Parameters.AddWithValue("@MaKh", maKh);
                    updateCmd.ExecuteNonQuery();
                }

                // Lấy danh sách tin nhắn ra để hiển thị
                string query = "SELECT IsAdmin, Message FROM ChatSupport WHERE MaKh = @MaKh ORDER BY CreatedAt ASC";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@MaKh", maKh);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            messages.Add(new {
                                isAdmin = reader.GetBoolean(0),
                                message = reader.GetString(1)
                            });
                        }
                    }
                }
            }
            return Json(messages);
        }

        // 4. Xử lý khi Admin bấm nút Gửi tin nhắn
        [HttpPost]
        public IActionResult GuiTinNhan(string maKh, string message)
        {
            if (string.IsNullOrEmpty(maKh) || string.IsNullOrEmpty(message)) 
                return Json(new { success = false });

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                // IsAdmin = 1 nghĩa là tin nhắn này xuất phát từ nhân viên MiniStop
                string query = "INSERT INTO ChatSupport (MaKh, TenKh, IsAdmin, Message, CreatedAt, IsRead) VALUES (@MaKh, 'Admin', 1, @Message, GETDATE(), 0)";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@MaKh", maKh);
                    cmd.Parameters.AddWithValue("@Message", message);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
            return Json(new { success = true });
        }
    }
}