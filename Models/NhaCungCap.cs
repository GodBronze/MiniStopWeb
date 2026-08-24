using System;
using System.Collections.Generic;

namespace MiniStopWeb.Models;

public partial class NhaCungCap
{
    public string MaNcc { get; set; } = null!;

    public string TenNcc { get; set; } = null!;

    public string? Sdt { get; set; }

    public string? DiaChi { get; set; }

    public virtual ICollection<PhieuNhap> PhieuNhaps { get; set; } = new List<PhieuNhap>();
}
