using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AspnetCoreMvcStarter.Data;
using AspnetCoreMvcStarter.Models;
using AspnetCoreMvcStarter.Models.ViewModels;

namespace AspnetCoreMvcStarter.Controllers;

[Authorize]
public class SozlukController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;
    private readonly IWebHostEnvironment _env;
    private const int PageSize = 20;
    private static readonly string[] AllowedImageExts = [".jpg", ".jpeg", ".png", ".gif", ".webp", ".svg"];
    private static readonly string[] AllowedAudioExts = [".mp3", ".wav", ".ogg", ".m4a"];
    private const long MaxFileSize = 25 * 1024 * 1024; // 25MB

    public SozlukController(AppDbContext db, UserManager<AppUser> userManager, IWebHostEnvironment env)
    {
        _db = db;
        _userManager = userManager;
        _env = env;
    }

    // ===================== LİSTELEME =====================

    public async Task<IActionResult> Index(int? temaId, int? konuId, string? arama, int sayfa = 1)
    {
        var query = _db.MaddeBaslari
            .Include(m => m.Konu).ThenInclude(k => k.Tema)
            .Include(m => m.Olusturan)
            .Include(m => m.Anlamlar)
            .AsQueryable();

        if (temaId.HasValue)
            query = query.Where(m => m.Konu.TemaId == temaId.Value);

        if (konuId.HasValue)
            query = query.Where(m => m.KonuId == konuId.Value);

        if (!string.IsNullOrWhiteSpace(arama))
            query = query.Where(m => m.Kelime.Contains(arama));

        var toplam = await query.CountAsync();
        var maddeler = await query
            .OrderBy(m => m.Kelime)
            .Skip((sayfa - 1) * PageSize)
            .Take(PageSize)
            .Select(m => new MaddeBasiListItem
            {
                Id = m.Id,
                Kelime = m.Kelime,
                TemaAd = m.Konu.Tema.Ad,
                KonuAd = m.Konu.Ad,
                AnlamSayisi = m.Anlamlar.Count,
                OlusturanAd = m.Olusturan.FullName,
                GuncellemeTarihi = m.GuncellemeTarihi
            })
            .ToListAsync();

        var temalar = await _db.Temalar.OrderBy(t => t.Ad).ToListAsync();

        SelectList? konular = null;
        if (temaId.HasValue)
        {
            var konuList = await _db.Konular.Where(k => k.TemaId == temaId.Value).OrderBy(k => k.Ad).ToListAsync();
            konular = new SelectList(konuList, "Id", "Ad", konuId);
        }

        return View(new MaddeBasiListViewModel
        {
            MaddeBaslari = maddeler,
            TemaId = temaId,
            KonuId = konuId,
            Arama = arama,
            Temalar = new SelectList(temalar, "Id", "Ad", temaId),
            Konular = konular,
            ToplamSayfa = (int)Math.Ceiling(toplam / (double)PageSize),
            MevcutSayfa = sayfa
        });
    }

    // ===================== GÖZAT =====================

    public async Task<IActionResult> Browse(int? temaId, int? konuId)
    {
        var temalar = await _db.Temalar
            .OrderBy(t => t.Ad)
            .Select(t => new BrowseTemaItem
            {
                Id = t.Id,
                Ad = t.Ad,
                KonuSayisi = t.Konular.Count,
                MaddeSayisi = t.Konular.SelectMany(k => k.MaddeBaslari).Count()
            })
            .ToListAsync();

        List<BrowseKonuItem>? konular = null;
        List<MaddeBasiListItem>? maddeler = null;
        string? seciliTema = null;
        string? seciliKonu = null;

        if (temaId.HasValue)
        {
            var tema = await _db.Temalar.FindAsync(temaId.Value);
            seciliTema = tema?.Ad;
            konular = await _db.Konular
                .Where(k => k.TemaId == temaId.Value)
                .OrderBy(k => k.Ad)
                .Select(k => new BrowseKonuItem
                {
                    Id = k.Id,
                    Ad = k.Ad,
                    MaddeSayisi = k.MaddeBaslari.Count
                })
                .ToListAsync();
        }

        if (konuId.HasValue)
        {
            var konu = await _db.Konular.Include(k => k.Tema).FirstOrDefaultAsync(k => k.Id == konuId.Value);
            seciliKonu = konu?.Ad;
            seciliTema = konu?.Tema.Ad;
            temaId = konu?.TemaId;

            // reload konular for sidebar
            if (konu != null)
            {
                konular = await _db.Konular
                    .Where(k => k.TemaId == konu.TemaId)
                    .OrderBy(k => k.Ad)
                    .Select(k => new BrowseKonuItem
                    {
                        Id = k.Id,
                        Ad = k.Ad,
                        MaddeSayisi = k.MaddeBaslari.Count
                    })
                    .ToListAsync();
            }

            maddeler = await _db.MaddeBaslari
                .Where(m => m.KonuId == konuId.Value)
                .OrderBy(m => m.Kelime)
                .Include(m => m.Konu).ThenInclude(k => k.Tema)
                .Include(m => m.Olusturan)
                .Include(m => m.Anlamlar)
                .Select(m => new MaddeBasiListItem
                {
                    Id = m.Id,
                    Kelime = m.Kelime,
                    TemaAd = m.Konu.Tema.Ad,
                    KonuAd = m.Konu.Ad,
                    AnlamSayisi = m.Anlamlar.Count,
                    OlusturanAd = m.Olusturan.FullName,
                    GuncellemeTarihi = m.GuncellemeTarihi
                })
                .ToListAsync();
        }

        return View(new BrowseViewModel
        {
            Temalar = temalar,
            Konular = konular,
            Maddeler = maddeler,
            SeciliTemaId = temaId,
            SeciliKonuId = konuId,
            SeciliTemaAd = seciliTema,
            SeciliKonuAd = seciliKonu
        });
    }

    // ===================== ARAMA API =====================

    [HttpGet]
    public async Task<IActionResult> Search(string q)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            return Json(Array.Empty<object>());

        var results = await _db.MaddeBaslari
            .Where(m => m.Kelime.Contains(q))
            .OrderBy(m => m.Kelime)
            .Take(10)
            .Select(m => new { m.Id, m.Kelime, Konu = m.Konu.Ad })
            .ToListAsync();

        return Json(results);
    }

    // ===================== OLUŞTURMA =====================

    [Authorize(Roles = "Admin,Editor")]
    public async Task<IActionResult> Create()
    {
        var model = new MaddeBasiCreateViewModel
        {
            Temalar = new SelectList(await _db.Temalar.OrderBy(t => t.Ad).ToListAsync(), "Id", "Ad")
        };
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Editor")]
    public async Task<IActionResult> Create(MaddeBasiCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.Temalar = new SelectList(await _db.Temalar.OrderBy(t => t.Ad).ToListAsync(), "Id", "Ad", model.TemaId);
            if (model.TemaId > 0)
                model.Konular = new SelectList(await _db.Konular.Where(k => k.TemaId == model.TemaId).OrderBy(k => k.Ad).ToListAsync(), "Id", "Ad", model.KonuId);
            return View(model);
        }

        var userId = _userManager.GetUserId(User)!;
        var madde = new MaddeBasi
        {
            Kelime = model.Kelime.Trim(),
            KonuId = model.KonuId,
            OlusturanId = userId
        };

        _db.MaddeBaslari.Add(madde);
        await _db.SaveChangesAsync();

        TempData["Success"] = $"\"{madde.Kelime}\" madde başı oluşturuldu. Şimdi anlam ekleyebilirsiniz.";
        return RedirectToAction("Detail", new { id = madde.Id });
    }

    // ===================== DETAY =====================

    public async Task<IActionResult> Detail(int id)
    {
        var madde = await _db.MaddeBaslari
            .Include(m => m.Konu).ThenInclude(k => k.Tema)
            .Include(m => m.Olusturan)
            .Include(m => m.Anlamlar).ThenInclude(a => a.KelimeTuru)
            .Include(m => m.Anlamlar).ThenInclude(a => a.Bicim)
            .Include(m => m.Anlamlar).ThenInclude(a => a.Duzey)
            .Include(m => m.Anlamlar).ThenInclude(a => a.OrnekCumleler)
            .Include(m => m.Medyalar)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (madde == null) return NotFound();

        var vm = new MaddeBasiDetailViewModel
        {
            Id = madde.Id,
            Kelime = madde.Kelime,
            TemaAd = madde.Konu.Tema.Ad,
            KonuAd = madde.Konu.Ad,
            KonuId = madde.KonuId,
            OlusturanAd = madde.Olusturan.FullName,
            OlusturmaTarihi = madde.OlusturmaTarihi,
            GuncellemeTarihi = madde.GuncellemeTarihi,
            Anlamlar = madde.Anlamlar.OrderBy(a => a.Numara).Select(a => new AnlamDetailItem
            {
                Id = a.Id,
                Numara = a.Numara,
                KelimeTuruAd = a.KelimeTuru.Ad,
                KelimeTuruRenk = a.KelimeTuru.Renk,
                BicimAd = a.Bicim?.Ad,
                DuzeyAd = a.Duzey?.Ad,
                Tanim = a.Tanim,
                OrnekCumleler = a.OrnekCumleler.OrderBy(o => o.Sira).Select(o => o.Cumle).ToList()
            }).ToList(),
            Medyalar = madde.Medyalar.Select(m => new MedyaItem
            {
                Id = m.Id,
                DosyaYolu = m.DosyaYolu,
                DosyaTipi = m.DosyaTipi,
                Aciklama = m.Aciklama
            }).ToList()
        };

        return View(vm);
    }

    // ===================== DÜZENLEME (KELİME) =====================

    [Authorize(Roles = "Admin,Editor")]
    public async Task<IActionResult> Edit(int id)
    {
        var madde = await _db.MaddeBaslari.Include(m => m.Konu).FirstOrDefaultAsync(m => m.Id == id);
        if (madde == null) return NotFound();

        var model = new MaddeBasiCreateViewModel
        {
            Kelime = madde.Kelime,
            KonuId = madde.KonuId,
            TemaId = madde.Konu.TemaId,
            Temalar = new SelectList(await _db.Temalar.OrderBy(t => t.Ad).ToListAsync(), "Id", "Ad", madde.Konu.TemaId),
            Konular = new SelectList(await _db.Konular.Where(k => k.TemaId == madde.Konu.TemaId).OrderBy(k => k.Ad).ToListAsync(), "Id", "Ad", madde.KonuId)
        };
        ViewBag.MaddeId = id;
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Editor")]
    public async Task<IActionResult> Edit(int id, MaddeBasiCreateViewModel model)
    {
        var madde = await _db.MaddeBaslari.FindAsync(id);
        if (madde == null) return NotFound();

        if (!ModelState.IsValid)
        {
            model.Temalar = new SelectList(await _db.Temalar.OrderBy(t => t.Ad).ToListAsync(), "Id", "Ad", model.TemaId);
            model.Konular = new SelectList(await _db.Konular.Where(k => k.TemaId == model.TemaId).OrderBy(k => k.Ad).ToListAsync(), "Id", "Ad", model.KonuId);
            ViewBag.MaddeId = id;
            return View(model);
        }

        madde.Kelime = model.Kelime.Trim();
        madde.KonuId = model.KonuId;
        madde.GuncellemeTarihi = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        TempData["Success"] = "Madde başı güncellendi.";
        return RedirectToAction("Detail", new { id });
    }

    // ===================== SİLME =====================

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Editor")]
    public async Task<IActionResult> Delete(int id)
    {
        var madde = await _db.MaddeBaslari.Include(m => m.Medyalar).FirstOrDefaultAsync(m => m.Id == id);
        if (madde == null) return NotFound();

        // İlişkili medya dosyalarını diskten sil
        foreach (var medya in madde.Medyalar)
        {
            var filePath = Path.Combine(_env.WebRootPath, medya.DosyaYolu.TrimStart('/'));
            if (System.IO.File.Exists(filePath))
                System.IO.File.Delete(filePath);
        }

        _db.MaddeBaslari.Remove(madde);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"\"{madde.Kelime}\" madde başı silindi.";
        return RedirectToAction("Index");
    }

    // ===================== ANLAM EKLE =====================

    [Authorize(Roles = "Admin,Editor")]
    public async Task<IActionResult> AnlamEkle(int maddeId)
    {
        var madde = await _db.MaddeBaslari.FindAsync(maddeId);
        if (madde == null) return NotFound();

        return View(await BuildAnlamCreateVM(maddeId, madde.Kelime));
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Editor")]
    public async Task<IActionResult> AnlamEkle(AnlamCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var m = await _db.MaddeBaslari.FindAsync(model.MaddeBasiId);
            model.Kelime = m?.Kelime ?? "";
            model.KelimeTurleri = new SelectList(await _db.KelimeTurleri.OrderBy(k => k.Ad).ToListAsync(), "Id", "Ad", model.KelimeTuruId);
            model.Bicimler = new SelectList(await _db.Bicimler.OrderBy(b => b.Ad).ToListAsync(), "Id", "Ad", model.BicimId);
            model.Duzeyler = new SelectList(await _db.Duzeyler.OrderBy(d => d.Sira).ToListAsync(), "Id", "Ad", model.DuzeyId);
            return View(model);
        }

        var maxNumara = await _db.Anlamlar
            .Where(a => a.MaddeBasiId == model.MaddeBasiId)
            .MaxAsync(a => (int?)a.Numara) ?? 0;

        var userId = _userManager.GetUserId(User)!;
        var anlam = new Anlam
        {
            MaddeBasiId = model.MaddeBasiId,
            Numara = maxNumara + 1,
            KelimeTuruId = model.KelimeTuruId,
            BicimId = model.BicimId,
            DuzeyId = model.DuzeyId,
            Tanim = model.Tanim.Trim(),
            OlusturanId = userId
        };

        _db.Anlamlar.Add(anlam);
        await _db.SaveChangesAsync();

        // Örnek cümleleri ekle
        if (!string.IsNullOrWhiteSpace(model.OrnekCumlelerText))
        {
            var satirlar = model.OrnekCumlelerText.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            int sira = 1;
            foreach (var satir in satirlar)
            {
                _db.OrnekCumleler.Add(new OrnekCumle
                {
                    AnlamId = anlam.Id,
                    Cumle = satir,
                    Sira = sira++
                });
            }
            await _db.SaveChangesAsync();
        }

        // Madde başı güncelleme tarihini güncelle
        var madde = await _db.MaddeBaslari.FindAsync(model.MaddeBasiId);
        if (madde != null)
        {
            madde.GuncellemeTarihi = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        TempData["Success"] = $"Anlam #{anlam.Numara} eklendi.";
        return RedirectToAction("Detail", new { id = model.MaddeBasiId });
    }

    // ===================== ANLAM DÜZENLE =====================

    [Authorize(Roles = "Admin,Editor")]
    public async Task<IActionResult> AnlamDuzenle(int id)
    {
        var anlam = await _db.Anlamlar
            .Include(a => a.MaddeBasi)
            .Include(a => a.OrnekCumleler)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (anlam == null) return NotFound();

        var model = new AnlamEditViewModel
        {
            Id = anlam.Id,
            MaddeBasiId = anlam.MaddeBasiId,
            Kelime = anlam.MaddeBasi.Kelime,
            Numara = anlam.Numara,
            KelimeTuruId = anlam.KelimeTuruId,
            BicimId = anlam.BicimId,
            DuzeyId = anlam.DuzeyId,
            Tanim = anlam.Tanim,
            OrnekCumlelerText = string.Join("\n", anlam.OrnekCumleler.OrderBy(o => o.Sira).Select(o => o.Cumle)),
            KelimeTurleri = new SelectList(await _db.KelimeTurleri.OrderBy(k => k.Ad).ToListAsync(), "Id", "Ad", anlam.KelimeTuruId),
            Bicimler = new SelectList(await _db.Bicimler.OrderBy(b => b.Ad).ToListAsync(), "Id", "Ad", anlam.BicimId),
            Duzeyler = new SelectList(await _db.Duzeyler.OrderBy(d => d.Sira).ToListAsync(), "Id", "Ad", anlam.DuzeyId)
        };

        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Editor")]
    public async Task<IActionResult> AnlamDuzenle(AnlamEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.KelimeTurleri = new SelectList(await _db.KelimeTurleri.OrderBy(k => k.Ad).ToListAsync(), "Id", "Ad", model.KelimeTuruId);
            model.Bicimler = new SelectList(await _db.Bicimler.OrderBy(b => b.Ad).ToListAsync(), "Id", "Ad", model.BicimId);
            model.Duzeyler = new SelectList(await _db.Duzeyler.OrderBy(d => d.Sira).ToListAsync(), "Id", "Ad", model.DuzeyId);
            return View(model);
        }

        var anlam = await _db.Anlamlar.Include(a => a.OrnekCumleler).FirstOrDefaultAsync(a => a.Id == model.Id);
        if (anlam == null) return NotFound();

        anlam.KelimeTuruId = model.KelimeTuruId;
        anlam.BicimId = model.BicimId;
        anlam.DuzeyId = model.DuzeyId;
        anlam.Tanim = model.Tanim.Trim();

        // Örnek cümleleri güncelle: önce sil, sonra yeniden ekle
        _db.OrnekCumleler.RemoveRange(anlam.OrnekCumleler);

        if (!string.IsNullOrWhiteSpace(model.OrnekCumlelerText))
        {
            var satirlar = model.OrnekCumlelerText.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            int sira = 1;
            foreach (var satir in satirlar)
            {
                _db.OrnekCumleler.Add(new OrnekCumle
                {
                    AnlamId = anlam.Id,
                    Cumle = satir,
                    Sira = sira++
                });
            }
        }

        var madde = await _db.MaddeBaslari.FindAsync(anlam.MaddeBasiId);
        if (madde != null) madde.GuncellemeTarihi = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        TempData["Success"] = $"Anlam #{anlam.Numara} güncellendi.";
        return RedirectToAction("Detail", new { id = anlam.MaddeBasiId });
    }

    // ===================== ANLAM SİL =====================

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Editor")]
    public async Task<IActionResult> AnlamSil(int id)
    {
        var anlam = await _db.Anlamlar.FindAsync(id);
        if (anlam == null) return NotFound();

        var maddeId = anlam.MaddeBasiId;
        _db.Anlamlar.Remove(anlam);

        var madde = await _db.MaddeBaslari.FindAsync(maddeId);
        if (madde != null) madde.GuncellemeTarihi = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        // Numaraları yeniden sırala
        var kalanlar = await _db.Anlamlar.Where(a => a.MaddeBasiId == maddeId).OrderBy(a => a.Numara).ToListAsync();
        for (int i = 0; i < kalanlar.Count; i++)
            kalanlar[i].Numara = i + 1;
        await _db.SaveChangesAsync();

        TempData["Success"] = "Anlam silindi.";
        return RedirectToAction("Detail", new { id = maddeId });
    }

    // ===================== MEDYA YÜKLE =====================

    [Authorize(Roles = "Admin,Editor")]
    public async Task<IActionResult> MedyaYukle(int maddeId)
    {
        var madde = await _db.MaddeBaslari.FindAsync(maddeId);
        if (madde == null) return NotFound();

        return View(new MedyaUploadViewModel { MaddeBasiId = maddeId, Kelime = madde.Kelime });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Editor")]
    public async Task<IActionResult> MedyaYukle(MedyaUploadViewModel model)
    {
        var madde = await _db.MaddeBaslari.FindAsync(model.MaddeBasiId);
        if (madde == null) return NotFound();
        model.Kelime = madde.Kelime;

        if (!ModelState.IsValid)
            return View(model);

        var dosya = model.Dosya;

        // Boyut kontrolü
        if (dosya.Length > MaxFileSize)
        {
            ModelState.AddModelError("Dosya", "Dosya boyutu 25 MB'ı aşamaz.");
            return View(model);
        }

        // Uzantı kontrolü
        var ext = Path.GetExtension(dosya.FileName).ToLowerInvariant();
        var allowedExts = model.DosyaTipi == "gorsel" ? AllowedImageExts : AllowedAudioExts;

        if (!allowedExts.Contains(ext))
        {
            var valid = string.Join(", ", allowedExts);
            ModelState.AddModelError("Dosya", $"Geçersiz dosya formatı. İzin verilen: {valid}");
            return View(model);
        }

        // Dosyayı kaydet
        var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", model.DosyaTipi);
        Directory.CreateDirectory(uploadsDir);

        var uniqueName = $"{Guid.NewGuid()}{ext}";
        var filePath = Path.Combine(uploadsDir, uniqueName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await dosya.CopyToAsync(stream);
        }

        var medya = new Medya
        {
            MaddeBasiId = model.MaddeBasiId,
            DosyaYolu = $"/uploads/{model.DosyaTipi}/{uniqueName}",
            DosyaTipi = model.DosyaTipi,
            Aciklama = model.Aciklama?.Trim()
        };

        _db.Medyalar.Add(medya);
        madde.GuncellemeTarihi = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        TempData["Success"] = $"{(model.DosyaTipi == "gorsel" ? "Görsel" : "Ses dosyası")} yüklendi.";
        return RedirectToAction("Detail", new { id = model.MaddeBasiId });
    }

    // ===================== MEDYA SİL =====================

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Editor")]
    public async Task<IActionResult> MedyaSil(int id)
    {
        var medya = await _db.Medyalar.FindAsync(id);
        if (medya == null) return NotFound();

        var maddeId = medya.MaddeBasiId;

        // Diskten sil
        var filePath = Path.Combine(_env.WebRootPath, medya.DosyaYolu.TrimStart('/'));
        if (System.IO.File.Exists(filePath))
            System.IO.File.Delete(filePath);

        _db.Medyalar.Remove(medya);
        await _db.SaveChangesAsync();

        TempData["Success"] = "Medya silindi.";
        return RedirectToAction("Detail", new { id = maddeId });
    }

    // ===================== API: Konuları Getir (AJAX) =====================

    [HttpGet]
    public async Task<IActionResult> GetKonular(int temaId)
    {
        var konular = await _db.Konular
            .Where(k => k.TemaId == temaId)
            .OrderBy(k => k.Ad)
            .Select(k => new { k.Id, k.Ad })
            .ToListAsync();
        return Json(konular);
    }

    // ===================== HELPERS =====================

    private async Task<AnlamCreateViewModel> BuildAnlamCreateVM(int maddeId, string kelime)
    {
        return new AnlamCreateViewModel
        {
            MaddeBasiId = maddeId,
            Kelime = kelime,
            KelimeTurleri = new SelectList(await _db.KelimeTurleri.OrderBy(k => k.Ad).ToListAsync(), "Id", "Ad"),
            Bicimler = new SelectList(await _db.Bicimler.OrderBy(b => b.Ad).ToListAsync(), "Id", "Ad"),
            Duzeyler = new SelectList(await _db.Duzeyler.OrderBy(d => d.Sira).ToListAsync(), "Id", "Ad")
        };
    }
}
