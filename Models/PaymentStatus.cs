using System;
using System.Collections.Generic;

namespace MiniStopWeb.Models;

public partial class PaymentStatus
{
    public int MaThanhToan { get; set; }

    public string? MaHd { get; set; }

    public string? HinhThucThanhToan { get; set; }

    public string? TrangThai { get; set; }

    public DateTime? NgayThanhToan { get; set; }

    public virtual HoaDon? MaHdNavigation { get; set; }
}
