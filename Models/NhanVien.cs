using System;
using System.Collections.Generic;

namespace MiniStopWeb.Models;

public partial class NhanVien
{
    public string MaNv { get; set; } = null!;

    public string TenNv { get; set; } = null!;

    public string? Sdt { get; set; }

    public string? DiaChi { get; set; }

    public string? MatKhau { get; set; }

    public string? VaiTro { get; set; }

    public virtual ICollection<HoaDon> HoaDons { get; set; } = new List<HoaDon>();

    public virtual ICollection<PhieuNhap> PhieuNhaps { get; set; } = new List<PhieuNhap>();
}
