using System;
using System.Collections.Generic;

namespace MiniStopWeb.Models;

public partial class LoaiSanPham
{
    public string MaLoai { get; set; } = null!;

    public string TenLoai { get; set; } = null!;

    public string? MoTa { get; set; }

    public virtual ICollection<SanPham> SanPhams { get; set; } = new List<SanPham>();
}
