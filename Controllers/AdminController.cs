using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AspnetCoreMvcStarter.Data;
using AspnetCoreMvcStarter.Models;
using AspnetCoreMvcStarter.Models.ViewModels;

namespace AspnetCoreMvcStarter.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly AppDbContext _db;

    public AdminController(UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager, AppDbContext db)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _db = db;
    }

    // GET: /Admin/Users
    public async Task<IActionResult> Users()
    {
        var users = await _userManager.Users.OrderByDescending(u => u.CreatedAt).ToListAsync();
        var userList = new List<UserListViewModel>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            userList.Add(new UserListViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? "",
                Role = roles.FirstOrDefault() ?? "Viewer",
                CreatedAt = user.CreatedAt
            });
        }

        return View(userList);
    }

    // GET: /Admin/EditRole/userId
    public async Task<IActionResult> EditRole(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        var roles = await _userManager.GetRolesAsync(user);
        var allRoles = await _roleManager.Roles.Select(r => r.Name!).ToListAsync();

        return View(new EditUserRoleViewModel
        {
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? "",
            CurrentRole = roles.FirstOrDefault() ?? "Viewer",
            AvailableRoles = allRoles
        });
    }

    // POST: /Admin/EditRole
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditRole(EditUserRoleViewModel model)
    {
        var user = await _userManager.FindByIdAsync(model.UserId);
        if (user == null) return NotFound();

        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);
        await _userManager.AddToRoleAsync(user, model.NewRole);

        TempData["Success"] = $"{user.FullName} kullanıcısının rolü {model.NewRole} olarak güncellendi.";
        return RedirectToAction("Users");
    }

    // POST: /Admin/DeleteUser/userId
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteUser(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        // Admin kendini silemesin
        if (user.Id == _userManager.GetUserId(User))
        {
            TempData["Error"] = "Kendi hesabınızı silemezsiniz.";
            return RedirectToAction("Users");
        }

        await _userManager.DeleteAsync(user);
        TempData["Success"] = $"{user.FullName} kullanıcısı silindi.";
        return RedirectToAction("Users");
    }

    // ===================== TEMA CRUD =====================

    public async Task<IActionResult> Temalar()
    {
        var temalar = await _db.Temalar.OrderBy(t => t.Ad).ToListAsync();
        return View(temalar);
    }

    public IActionResult TemaEkle() => View(new Tema());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> TemaEkle(Tema model)
    {
        if (!ModelState.IsValid) return View(model);
        _db.Temalar.Add(model);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"\"{model.Ad}\" teması eklendi.";
        return RedirectToAction("Temalar");
    }

    public async Task<IActionResult> TemaDuzenle(int id)
    {
        var tema = await _db.Temalar.FindAsync(id);
        if (tema == null) return NotFound();
        return View(tema);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> TemaDuzenle(Tema model)
    {
        if (!ModelState.IsValid) return View(model);
        _db.Temalar.Update(model);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"\"{model.Ad}\" teması güncellendi.";
        return RedirectToAction("Temalar");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> TemaSil(int id)
    {
        var tema = await _db.Temalar.FindAsync(id);
        if (tema == null) return NotFound();
        _db.Temalar.Remove(tema);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"\"{tema.Ad}\" teması silindi.";
        return RedirectToAction("Temalar");
    }

    // ===================== KONU CRUD =====================

    public async Task<IActionResult> Konular(int? temaId)
    {
        var query = _db.Konular.Include(k => k.Tema).AsQueryable();
        if (temaId.HasValue)
            query = query.Where(k => k.TemaId == temaId.Value);

        ViewBag.Temalar = new SelectList(await _db.Temalar.OrderBy(t => t.Ad).ToListAsync(), "Id", "Ad", temaId);
        ViewBag.SeciliTemaId = temaId;
        return View(await query.OrderBy(k => k.Tema.Ad).ThenBy(k => k.Ad).ToListAsync());
    }

    public async Task<IActionResult> KonuEkle(int? temaId)
    {
        ViewBag.Temalar = new SelectList(await _db.Temalar.OrderBy(t => t.Ad).ToListAsync(), "Id", "Ad", temaId);
        return View(new Konu { TemaId = temaId ?? 0 });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> KonuEkle(Konu model)
    {
        ModelState.Remove(nameof(Konu.Tema));
        if (!ModelState.IsValid)
        {
            ViewBag.Temalar = new SelectList(await _db.Temalar.OrderBy(t => t.Ad).ToListAsync(), "Id", "Ad", model.TemaId);
            return View(model);
        }
        _db.Konular.Add(model);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"\"{model.Ad}\" konusu eklendi.";
        return RedirectToAction("Konular", new { temaId = model.TemaId });
    }

    public async Task<IActionResult> KonuDuzenle(int id)
    {
        var konu = await _db.Konular.FindAsync(id);
        if (konu == null) return NotFound();
        ViewBag.Temalar = new SelectList(await _db.Temalar.OrderBy(t => t.Ad).ToListAsync(), "Id", "Ad", konu.TemaId);
        return View(konu);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> KonuDuzenle(Konu model)
    {
        ModelState.Remove(nameof(Konu.Tema));
        if (!ModelState.IsValid)
        {
            ViewBag.Temalar = new SelectList(await _db.Temalar.OrderBy(t => t.Ad).ToListAsync(), "Id", "Ad", model.TemaId);
            return View(model);
        }
        _db.Konular.Update(model);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"\"{model.Ad}\" konusu güncellendi.";
        return RedirectToAction("Konular", new { temaId = model.TemaId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> KonuSil(int id)
    {
        var konu = await _db.Konular.FindAsync(id);
        if (konu == null) return NotFound();
        var temaId = konu.TemaId;
        _db.Konular.Remove(konu);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"\"{konu.Ad}\" konusu silindi.";
        return RedirectToAction("Konular", new { temaId });
    }

    // ===================== KELİME TÜRÜ CRUD =====================

    public async Task<IActionResult> KelimeTurleri()
    {
        return View(await _db.KelimeTurleri.OrderBy(k => k.Ad).ToListAsync());
    }

    public IActionResult KelimeTuruEkle() => View(new KelimeTuru());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> KelimeTuruEkle(KelimeTuru model)
    {
        if (!ModelState.IsValid) return View(model);
        _db.KelimeTurleri.Add(model);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"\"{model.Ad}\" kelime türü eklendi.";
        return RedirectToAction("KelimeTurleri");
    }

    public async Task<IActionResult> KelimeTuruDuzenle(int id)
    {
        var kt = await _db.KelimeTurleri.FindAsync(id);
        if (kt == null) return NotFound();
        return View(kt);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> KelimeTuruDuzenle(KelimeTuru model)
    {
        if (!ModelState.IsValid) return View(model);
        _db.KelimeTurleri.Update(model);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"\"{model.Ad}\" kelime türü güncellendi.";
        return RedirectToAction("KelimeTurleri");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> KelimeTuruSil(int id)
    {
        var kt = await _db.KelimeTurleri.FindAsync(id);
        if (kt == null) return NotFound();
        _db.KelimeTurleri.Remove(kt);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"\"{kt.Ad}\" kelime türü silindi.";
        return RedirectToAction("KelimeTurleri");
    }

    // ===================== BİÇEM CRUD =====================

    public async Task<IActionResult> Bicimler()
    {
        return View(await _db.Bicimler.OrderBy(b => b.Ad).ToListAsync());
    }

    public IActionResult BicimEkle() => View(new Bicim());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> BicimEkle(Bicim model)
    {
        if (!ModelState.IsValid) return View(model);
        _db.Bicimler.Add(model);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"\"{model.Ad}\" biçem eklendi.";
        return RedirectToAction("Bicimler");
    }

    public async Task<IActionResult> BicimDuzenle(int id)
    {
        var b = await _db.Bicimler.FindAsync(id);
        if (b == null) return NotFound();
        return View(b);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> BicimDuzenle(Bicim model)
    {
        if (!ModelState.IsValid) return View(model);
        _db.Bicimler.Update(model);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"\"{model.Ad}\" biçem güncellendi.";
        return RedirectToAction("Bicimler");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> BicimSil(int id)
    {
        var b = await _db.Bicimler.FindAsync(id);
        if (b == null) return NotFound();
        _db.Bicimler.Remove(b);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"\"{b.Ad}\" biçem silindi.";
        return RedirectToAction("Bicimler");
    }
}
