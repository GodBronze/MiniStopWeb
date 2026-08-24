using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;

namespace MiniStopWeb.Controllers
{
    // ========================================================
    // CÁC CLASS HỖ TRỢ DỊCH DỮ LIỆU TỪ JAVASCRIPT GỬI LÊN
    // ========================================================
    public class ChatRequest
    {
        public List<MessageItem> History { get; set; }
    }

    public class MessageItem
    {
        public string role { get; set; }
        public List<PartItem> parts { get; set; }
    }

    public class PartItem
    {
        public string text { get; set; }
    }

    public class ChatController : Controller
    {
        // 1. API Key của Gemini
        private readonly string _apiKey = "AQ.Ab8RN6I6ZldAqO70ke_yHVOV8lgHrNxiDmtBZUPmIB26mb6RAw";
        
        // 2. Chuỗi kết nối SQL Server 
        // ⚠️ LƯU Ý: Bạn hãy sửa dòng này thành chuỗi kết nối SQL Server thực tế trong máy của bạn nhé!
         private readonly string _connectionString = "Data Source=localhost\\MINHTHANG;Initial Catalog=MiniStopDB;Integrated Security=True;TrustServerCertificate=True";


        // ========================================================
        // CHỨC NĂNG 1: CHAT VỚI TRÍ TUỆ NHÂN TẠO (AI GEMINI)
        // ========================================================
        [HttpPost]
        public async Task<IActionResult> GetResponse([FromBody] ChatRequest request)
        {
            if (request == null || request.History == null || request.History.Count == 0)
                return Json(new { reply = "Bạn hãy nhập câu hỏi để mình hỗ trợ nhé!" });

            string systemPrompt = @"Bạn là nhân viên tư vấn sành sỏi, cực kỳ thân thiện của cửa hàng tiện lợi MiniStop.
            Nhiệm vụ:
            1. Tư vấn thân thiện, xưng 'Mình' và gọi khách là 'Bạn'. Trả lời cực kỳ ngắn gọn.
            2. NẾU KHÁCH HỎI THỰC ĐƠN: Liệt kê rõ ràng bằng gạch đầu dòng.
            3. BẮT BUỘC: Trước khi kết thúc câu trả lời, hãy khéo léo xin số điện thoại và email của khách hàng để gửi tặng mã giảm giá 20%.";

            string productContext = "Thực đơn hiện tại của MiniStop: Bánh mì thịt nướng (20.000đ), Hamburger bò (35.000đ), Nước ngọt Coca (10.000đ), Nước suối (5.000đ).";

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={_apiKey}";
                    
                    var requestBody = new
                    {
                        systemInstruction = new 
                        { 
                            parts = new[] { new { text = systemPrompt + "\n" + productContext } } 
                        },
                        contents = request.History 
                    };

                    string jsonBody = JsonSerializer.Serialize(requestBody);
                    var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await client.PostAsync(url, content);
                    
                    if (!response.IsSuccessStatusCode)
                    {
                        return Json(new { reply = "Dạ hiện tại MiniStop đang có đầy đủ các món ăn nhanh và đồ uống. Để em tiện tư vấn chi tiết và gửi tặng anh/chị mã giảm giá 20%, anh/chị cho em xin số điện thoại và email nhé!" });
                    }

                    string responseJson = await response.Content.ReadAsStringAsync();
                    
                    using (JsonDocument doc = JsonDocument.Parse(responseJson))
                    {
                        var root = doc.RootElement;
                        var candidates = root.GetProperty("candidates");
                        string replyText = candidates[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString();
                        
                        return Json(new { reply = replyText });
                    }
                }
            }
            catch
            {
                return Json(new { reply = "Dạ vâng, cửa hàng MiniStop luôn sẵn sàng phục vụ ạ. Dạ anh/chị có thể để lại số điện thoại và email để nhận voucher 20% không ạ?" });
            }
        }

        // ========================================================
        // CHỨC NĂNG 2: GỬI TIN NHẮN TỪ KHÁCH HÀNG CHO ADMIN
        // ========================================================
        [HttpPost]
        public IActionResult GuiTinNhanAdmin(string message)
        {
            var maKh = HttpContext.Session.GetString("MaKh");
            var tenKh = HttpContext.Session.GetString("TenKh");

            // Rào chắn bảo mật: Không có session thì không cho chat
            if (string.IsNullOrEmpty(maKh))
            {
                return Json(new { success = false, error = "Vui lòng đăng nhập để chat với Admin!" });
            }

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                // Lưu tin nhắn vào bảng ChatSupport (IsAdmin = 0 nghĩa là khách hàng gửi)
                string query = "INSERT INTO ChatSupport (MaKh, TenKh, IsAdmin, Message, CreatedAt, IsRead) VALUES (@MaKh, @TenKh, 0, @Message, GETDATE(), 0)";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@MaKh", maKh);
                    cmd.Parameters.AddWithValue("@TenKh", string.IsNullOrEmpty(tenKh) ? "Khách hàng" : tenKh);
                    cmd.Parameters.AddWithValue("@Message", message);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
            return Json(new { success = true });
        }

        // ========================================================
        // CHỨC NĂNG 3: TẢI TOÀN BỘ LỊCH SỬ CHAT VỚI ADMIN
        // ========================================================
        [HttpGet]
        public IActionResult LayTinNhanKhachHang()
        {
            var maKh = HttpContext.Session.GetString("MaKh");
            
            // Nếu chưa đăng nhập thì trả về mảng rỗng
            if (string.IsNullOrEmpty(maKh)) return Json(new List<object>());

            var messages = new List<object>();
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                // Lấy toàn bộ lịch sử chat của khách hàng này, sắp xếp theo thời gian (cũ nhất ở trên, mới nhất ở dưới)
                string query = "SELECT IsAdmin, Message FROM ChatSupport WHERE MaKh = @MaKh ORDER BY CreatedAt ASC";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@MaKh", maKh);
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            messages.Add(new
                            {
                                isAdmin = reader.GetBoolean(0),
                                message = reader.GetString(1)
                            });
                        }
                    }
                }
            }
            return Json(messages);
        }
        [HttpGet]
        public IActionResult DemTinNhanChuaDoc()
        {
            var maKh = HttpContext.Session.GetString("MaKh");
            if (string.IsNullOrEmpty(maKh)) return Json(new { count = 0 });

            int count = 0;
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                // Đếm tin nhắn do Admin gửi (IsAdmin=1) và Khách chưa đọc (IsRead=0)
                string query = "SELECT COUNT(*) FROM ChatSupport WHERE MaKh = @MaKh AND IsAdmin = 1 AND IsRead = 0";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@MaKh", maKh);
                    conn.Open();
                    count = (int)cmd.ExecuteScalar();
                }
            }
            return Json(new { count = count });
        }

        [HttpPost]
        public IActionResult DanhDauDaDoc()
        {
            var maKh = HttpContext.Session.GetString("MaKh");
            if (string.IsNullOrEmpty(maKh)) return Json(new { success = false });

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                // Cập nhật trạng thái đã đọc khi khách mở khung chat
                string query = "UPDATE ChatSupport SET IsRead = 1 WHERE MaKh = @MaKh AND IsAdmin = 1";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@MaKh", maKh);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
            return Json(new { success = true });
        }
    }
    
}