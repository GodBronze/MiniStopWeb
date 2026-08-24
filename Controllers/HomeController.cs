using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MiniStopWeb.Models;
using System.Linq; // Thêm thư viện này để dùng .ToList() và .Take()

namespace MiniStopWeb.Controllers
{
    public class HomeController : Controller
    {
        
        private readonly MiniStopDbContext _context = new MiniStopDbContext();

        public IActionResult Index()
        {
            var dsSanPham = _context.SanPhams.Take(8).ToList();
            ViewBag.DanhMucs = _context.DanhMucs.ToList();
            return View(dsSanPham);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        public IActionResult KhuyenMai()
        {
            return View();
        }

        public IActionResult CuaHang()
        {
            return View();
        }

        public IActionResult LienHe()
        {
            return View();
        }
    }
}