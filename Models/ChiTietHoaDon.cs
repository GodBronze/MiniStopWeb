using System;
using System.Collections.Generic;

namespace MiniStopWeb.Models;

public partial class ChiTietHoaDon
{
    public string MaHd { get; set; } = null!;

    public string MaSp { get; set; } = null!;

    public int? SoLuongBan { get; set; }

    public decimal? DonGia { get; set; }

    public virtual HoaDon MaHdNavigation { get; set; } = null!;

    public virtual SanPham MaSpNavigation { get; set; } = null!;
}
