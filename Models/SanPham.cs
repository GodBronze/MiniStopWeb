using System;
using System.Collections.Generic;

namespace MiniStopWeb.Models;

public partial class SanPham
{
    public string MaSp { get; set; } = null!;

    public string TenSp { get; set; } = null!;

    public string? MaDm { get; set; }

    public decimal? DonGia { get; set; }

    public int? SoLuong { get; set; }

    public string? DonViTinh { get; set; }

    public string? HinhAnh { get; set; }

    public string? MoTa { get; set; }

    public string? TenKhongDau { get; set; }

    public bool? IsNoiBat { get; set; }

    public DateTime? NgayTao { get; set; }

    public decimal? GiaKhuyenMai { get; set; }
    public DateTime? HanSuDung { get; set; }

    public virtual ICollection<ChiTietHoaDon> ChiTietHoaDons { get; set; } = new List<ChiTietHoaDon>();

    public virtual ICollection<ChiTietPhieuNhap> ChiTietPhieuNhaps { get; set; } = new List<ChiTietPhieuNhap>();

    public virtual DanhMuc? MaDmNavigation { get; set; }
}
