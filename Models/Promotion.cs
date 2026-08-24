using System;
using System.Collections.Generic;

namespace MiniStopWeb.Models;

public partial class Promotion
{
    public string MaKm { get; set; } = null!;

    public string TenKm { get; set; } = null!;

    public string? MoTa { get; set; }

    public decimal? GiaTriGiam { get; set; }

    public DateOnly? NgayBatDau { get; set; }

    public DateOnly? NgayKetThuc { get; set; }

    public string? ApDungCho { get; set; }

    public virtual ICollection<HoaDon> HoaDons { get; set; } = new List<HoaDon>();
}
