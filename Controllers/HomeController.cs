using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AspnetCoreMvcStarter.Data;
using AspnetCoreMvcStarter.Models;

namespace AspnetCoreMvcStarter.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly AppDbContext _db;

    public HomeController(ILogger<HomeController> logger, AppDbContext db)
    {
        _logger = logger;
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        if (!User.Identity?.IsAuthenticated ?? true)
            return RedirectToAction("Login", "Account");

        ViewBag.MaddeSayisi = await _db.MaddeBaslari.CountAsync();
        ViewBag.AnlamSayisi = await _db.Anlamlar.CountAsync();
        ViewBag.TemaSayisi = await _db.Temalar.CountAsync();
        ViewBag.KonuSayisi = await _db.Konular.CountAsync();
        ViewBag.MedyaSayisi = await _db.Medyalar.CountAsync();
        ViewBag.KullaniciSayisi = await _db.Users.CountAsync();

        ViewBag.SonEklenenler = await _db.MaddeBaslari
            .OrderByDescending(m => m.GuncellemeTarihi)
            .Take(10)
            .Include(m => m.Konu).ThenInclude(k => k.Tema)
            .Include(m => m.Olusturan)
            .Include(m => m.Anlamlar)
            .ToListAsync();

        return View();
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
}
