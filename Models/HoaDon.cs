using System;
using System.Collections.Generic;

namespace MiniStopWeb.Models;

public partial class HoaDon
{
    public string MaHd { get; set; } = null!;

    public string? MaNv { get; set; }

    public string? MaKh { get; set; }

    public DateTime? NgayNhap { get; set; }

    public decimal? TongTien { get; set; }

    public string? TrangThai { get; set; }

    public virtual ICollection<ChiTietHoaDon> ChiTietHoaDons { get; set; } = new List<ChiTietHoaDon>();

    public virtual KhachHang? MaKhNavigation { get; set; }

    public virtual NhanVien? MaNvNavigation { get; set; }
}
