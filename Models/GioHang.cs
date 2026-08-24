using System;
using System.Collections.Generic;

namespace MiniStopWeb.Models;

public partial class GioHang
{
    public int MaGh { get; set; }

    public string? MaKh { get; set; }

    public string? MaSp { get; set; }

    public int SoLuong { get; set; }

    public DateTime? NgayThem { get; set; }

    public virtual KhachHang? MaKhNavigation { get; set; }

    public virtual SanPham? MaSpNavigation { get; set; }
}
