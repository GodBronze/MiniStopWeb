using System;
using System.Collections.Generic;

namespace MiniStopWeb.Models;

public partial class ChiTietPhieuNhap
{
    public string MaPn { get; set; } = null!;

    public string MaSp { get; set; } = null!;

    public int? SoLuongNhap { get; set; }

    public virtual PhieuNhap MaPnNavigation { get; set; } = null!;

    public virtual SanPham MaSpNavigation { get; set; } = null!;
}
