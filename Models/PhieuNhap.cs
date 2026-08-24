using System;
using System.Collections.Generic;

namespace MiniStopWeb.Models;

public partial class PhieuNhap
{
    public string MaPn { get; set; } = null!;

    public string? MaNv { get; set; }

    public string? MaNcc { get; set; }

    public DateTime? NgayNhap { get; set; }

    public virtual ICollection<ChiTietPhieuNhap> ChiTietPhieuNhaps { get; set; } = new List<ChiTietPhieuNhap>();

    public virtual NhaCungCap? MaNccNavigation { get; set; }

    public virtual NhanVien? MaNvNavigation { get; set; }
}
