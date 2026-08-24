namespace MiniStopWeb.Models
{
    public class CartItem
    {
        public string MaSp { get; set; } = string.Empty;
        public string TenSp { get; set; } = string.Empty;
        public decimal DonGia { get; set; }
        public int SoLuong { get; set; }
        public decimal ThanhTien => DonGia * SoLuong; 
    }
}