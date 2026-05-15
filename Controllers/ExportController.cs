using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AspnetCoreMvcStarter.Data;

namespace AspnetCoreMvcStarter.Controllers;

[Authorize(Roles = "Admin")]
public class ExportController : Controller
{
    private readonly AppDbContext _db;

    public ExportController(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var stats = new ExportStatsViewModel
        {
            TemaSayisi = await _db.Temalar.CountAsync(),
            KonuSayisi = await _db.Konular.CountAsync(),
            MaddeSayisi = await _db.MaddeBaslari.CountAsync(),
            AnlamSayisi = await _db.Anlamlar.CountAsync(),
            OrnekCumleSayisi = await _db.OrnekCumleler.CountAsync(),
            MedyaSayisi = await _db.Medyalar.CountAsync()
        };
        return View(stats);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Json(int? temaId)
    {
        var data = await BuildExportData(temaId);

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        var json = JsonSerializer.Serialize(data, options);
        var bytes = Encoding.UTF8.GetBytes(json);
        var fileName = $"tdksozluk_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json";

        return File(bytes, "application/json", fileName);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Sql(int? temaId)
    {
        var data = await BuildExportData(temaId);
        var sb = new StringBuilder();

        sb.AppendLine("-- TDK Sözlük Export");
        sb.AppendLine($"-- Tarih: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine("-- Format: PostgreSQL INSERT statements");
        sb.AppendLine();
        sb.AppendLine("BEGIN;");
        sb.AppendLine();

        // Temalar
        foreach (var tema in data.Temalar)
        {
            sb.AppendLine($"INSERT INTO temalar (ad, sira) VALUES ({EscSql(tema.Ad)}, {tema.Sira});");
        }
        sb.AppendLine();

        // Konular
        foreach (var tema in data.Temalar)
        {
            foreach (var konu in tema.Konular)
            {
                sb.AppendLine($"INSERT INTO konular (ad, sira, tema_id) VALUES ({EscSql(konu.Ad)}, {konu.Sira}, (SELECT id FROM temalar WHERE ad = {EscSql(tema.Ad)} LIMIT 1));");
            }
        }
        sb.AppendLine();

        // Kelime Türleri
        foreach (var kt in data.KelimeTurleri)
        {
            sb.AppendLine($"INSERT INTO kelime_turleri (ad, renk) VALUES ({EscSql(kt.Ad)}, {EscSql(kt.Renk)});");
        }
        sb.AppendLine();

        // Biçemler
        foreach (var b in data.Bicimler)
        {
            sb.AppendLine($"INSERT INTO bicimler (ad) VALUES ({EscSql(b)});");
        }
        sb.AppendLine();

        // Düzeyler
        foreach (var d in data.Duzeyler)
        {
            sb.AppendLine($"INSERT INTO duzeyler (ad) VALUES ({EscSql(d)});");
        }
        sb.AppendLine();

        // Madde Başları + Anlamlar + Örnek Cümleler
        foreach (var tema in data.Temalar)
        {
            foreach (var konu in tema.Konular)
            {
                foreach (var madde in konu.Maddeler)
                {
                    sb.AppendLine($"INSERT INTO madde_baslari (kelime, konu_id) VALUES ({EscSql(madde.Kelime)}, (SELECT id FROM konular WHERE ad = {EscSql(konu.Ad)} LIMIT 1));");

                    foreach (var anlam in madde.Anlamlar)
                    {
                        var bicimPart = anlam.Bicim != null ? $", (SELECT id FROM bicimler WHERE ad = {EscSql(anlam.Bicim)} LIMIT 1)" : ", NULL";
                        var duzeyPart = anlam.Duzey != null ? $", (SELECT id FROM duzeyler WHERE ad = {EscSql(anlam.Duzey)} LIMIT 1)" : ", NULL";

                        sb.AppendLine($"INSERT INTO anlamlar (madde_basi_id, numara, kelime_turu_id, bicim_id, duzey_id, tanim) VALUES ((SELECT id FROM madde_baslari WHERE kelime = {EscSql(madde.Kelime)} LIMIT 1), {anlam.Numara}, (SELECT id FROM kelime_turleri WHERE ad = {EscSql(anlam.KelimeTuru)} LIMIT 1){bicimPart}{duzeyPart}, {EscSql(anlam.Tanim)});");

                        int sira = 1;
                        foreach (var ornek in anlam.OrnekCumleler)
                        {
                            sb.AppendLine($"INSERT INTO ornek_cumleler (anlam_id, cumle, sira) VALUES ((SELECT id FROM anlamlar WHERE tanim = {EscSql(anlam.Tanim)} AND madde_basi_id = (SELECT id FROM madde_baslari WHERE kelime = {EscSql(madde.Kelime)} LIMIT 1) LIMIT 1), {EscSql(ornek)}, {sira++});");
                        }
                    }
                }
            }
        }

        sb.AppendLine();
        sb.AppendLine("COMMIT;");

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        var fileName = $"tdksozluk_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.sql";

        return File(bytes, "text/sql", fileName);
    }

    private async Task<ExportDataModel> BuildExportData(int? temaId)
    {
        var temaQuery = _db.Temalar.OrderBy(t => t.Ad).AsQueryable();
        if (temaId.HasValue)
            temaQuery = temaQuery.Where(t => t.Id == temaId.Value);

        var temalar = await temaQuery
            .Include(t => t.Konular).ThenInclude(k => k.MaddeBaslari).ThenInclude(m => m.Anlamlar).ThenInclude(a => a.KelimeTuru)
            .Include(t => t.Konular).ThenInclude(k => k.MaddeBaslari).ThenInclude(m => m.Anlamlar).ThenInclude(a => a.Bicim)
            .Include(t => t.Konular).ThenInclude(k => k.MaddeBaslari).ThenInclude(m => m.Anlamlar).ThenInclude(a => a.Duzey)
            .Include(t => t.Konular).ThenInclude(k => k.MaddeBaslari).ThenInclude(m => m.Anlamlar).ThenInclude(a => a.OrnekCumleler)
            .Include(t => t.Konular).ThenInclude(k => k.MaddeBaslari).ThenInclude(m => m.Medyalar)
            .ToListAsync();

        var kelimeTurleri = await _db.KelimeTurleri.OrderBy(k => k.Ad).ToListAsync();
        var bicimler = await _db.Bicimler.OrderBy(b => b.Ad).ToListAsync();
        var duzeyler = await _db.Duzeyler.OrderBy(d => d.Sira).ToListAsync();

        return new ExportDataModel
        {
            ExportTarihi = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
            KelimeTurleri = kelimeTurleri.Select(k => new ExportKelimeTuru { Ad = k.Ad, Renk = k.Renk }).ToList(),
            Bicimler = bicimler.Select(b => b.Ad).ToList(),
            Duzeyler = duzeyler.Select(d => d.Ad).ToList(),
            Temalar = temalar.Select(t => new ExportTema
            {
                Ad = t.Ad,
                Sira = t.Sira,
                Konular = t.Konular.OrderBy(k => k.Ad).Select(k => new ExportKonu
                {
                    Ad = k.Ad,
                    Sira = k.Sira,
                    Maddeler = k.MaddeBaslari.OrderBy(m => m.Kelime).Select(m => new ExportMadde
                    {
                        Kelime = m.Kelime,
                        Anlamlar = m.Anlamlar.OrderBy(a => a.Numara).Select(a => new ExportAnlam
                        {
                            Numara = a.Numara,
                            KelimeTuru = a.KelimeTuru.Ad,
                            Bicim = a.Bicim?.Ad,
                            Duzey = a.Duzey?.Ad,
                            Tanim = a.Tanim,
                            OrnekCumleler = a.OrnekCumleler.OrderBy(o => o.Sira).Select(o => o.Cumle).ToList()
                        }).ToList(),
                        Medyalar = m.Medyalar.Select(med => new ExportMedya
                        {
                            DosyaYolu = med.DosyaYolu,
                            DosyaTipi = med.DosyaTipi,
                            Aciklama = med.Aciklama
                        }).ToList()
                    }).ToList()
                }).ToList()
            }).ToList()
        };
    }

    private static string EscSql(string? value)
    {
        if (value == null) return "NULL";
        return "'" + value.Replace("'", "''") + "'";
    }
}

// ===================== Export Models =====================

public class ExportStatsViewModel
{
    public int TemaSayisi { get; set; }
    public int KonuSayisi { get; set; }
    public int MaddeSayisi { get; set; }
    public int AnlamSayisi { get; set; }
    public int OrnekCumleSayisi { get; set; }
    public int MedyaSayisi { get; set; }
}

public class ExportDataModel
{
    public string ExportTarihi { get; set; } = string.Empty;
    public List<ExportKelimeTuru> KelimeTurleri { get; set; } = new();
    public List<string> Bicimler { get; set; } = new();
    public List<string> Duzeyler { get; set; } = new();
    public List<ExportTema> Temalar { get; set; } = new();
}

public class ExportKelimeTuru
{
    public string Ad { get; set; } = string.Empty;
    public string Renk { get; set; } = string.Empty;
}

public class ExportTema
{
    public string Ad { get; set; } = string.Empty;
    public int Sira { get; set; }
    public List<ExportKonu> Konular { get; set; } = new();
}

public class ExportKonu
{
    public string Ad { get; set; } = string.Empty;
    public int Sira { get; set; }
    public List<ExportMadde> Maddeler { get; set; } = new();
}

public class ExportMadde
{
    public string Kelime { get; set; } = string.Empty;
    public List<ExportAnlam> Anlamlar { get; set; } = new();
    public List<ExportMedya> Medyalar { get; set; } = new();
}

public class ExportAnlam
{
    public int Numara { get; set; }
    public string KelimeTuru { get; set; } = string.Empty;
    public string? Bicim { get; set; }
    public string? Duzey { get; set; }
    public string Tanim { get; set; } = string.Empty;
    public List<string> OrnekCumleler { get; set; } = new();
}

public class ExportMedya
{
    public string DosyaYolu { get; set; } = string.Empty;
    public string DosyaTipi { get; set; } = string.Empty;
    public string? Aciklama { get; set; }
}
