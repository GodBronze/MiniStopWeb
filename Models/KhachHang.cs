using System;
using System.Collections.Generic;

namespace MiniStopWeb.Models;

public partial class KhachHang
{
    public string MaKh { get; set; } = null!;

    public string TenKh { get; set; } = null!;

    public string? Sdt { get; set; }

    public string? DiaChi { get; set; }

    public int? DiemTichLuy { get; set; }

    public string? MatKhau { get; set; }

    public virtual ICollection<HoaDon> HoaDons { get; set; } = new List<HoaDon>();
}
