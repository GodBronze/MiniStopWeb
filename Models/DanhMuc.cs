using System;
using System.Collections.Generic;

namespace MiniStopWeb.Models;

public partial class DanhMuc
{
    public string MaDm { get; set; } = null!;

    public string TenDm { get; set; } = null!;

    public virtual ICollection<SanPham> SanPhams { get; set; } = new List<SanPham>();
}
