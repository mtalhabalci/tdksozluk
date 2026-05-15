using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AspnetCoreMvcStarter.Models.ViewModels;

// ===================== MADDE BAŞI =====================

public class MaddeBasiListViewModel
{
    public List<MaddeBasiListItem> MaddeBaslari { get; set; } = new();
    public int? TemaId { get; set; }
    public int? KonuId { get; set; }
    public string? Arama { get; set; }
    public SelectList? Temalar { get; set; }
    public SelectList? Konular { get; set; }
    public int ToplamSayfa { get; set; }
    public int MevcutSayfa { get; set; }
}

public class MaddeBasiListItem
{
    public int Id { get; set; }
    public string Kelime { get; set; } = string.Empty;
    public string TemaAd { get; set; } = string.Empty;
    public string KonuAd { get; set; } = string.Empty;
    public int AnlamSayisi { get; set; }
    public string OlusturanAd { get; set; } = string.Empty;
    public DateTime GuncellemeTarihi { get; set; }
}

public class MaddeBasiCreateViewModel
{
    [Required(ErrorMessage = "Kelime gereklidir")]
    [MaxLength(200)]
    [Display(Name = "Kelime")]
    public string Kelime { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tema seçiniz")]
    [Display(Name = "Tema")]
    public int TemaId { get; set; }

    [Required(ErrorMessage = "Konu seçiniz")]
    [Display(Name = "Konu")]
    public int KonuId { get; set; }

    public SelectList? Temalar { get; set; }
    public SelectList? Konular { get; set; }
}

public class MaddeBasiDetailViewModel
{
    public int Id { get; set; }
    public string Kelime { get; set; } = string.Empty;
    public string TemaAd { get; set; } = string.Empty;
    public string KonuAd { get; set; } = string.Empty;
    public int KonuId { get; set; }
    public string OlusturanAd { get; set; } = string.Empty;
    public DateTime OlusturmaTarihi { get; set; }
    public DateTime GuncellemeTarihi { get; set; }

    public List<AnlamDetailItem> Anlamlar { get; set; } = new();
    public List<MedyaItem> Medyalar { get; set; } = new();
}

public class AnlamDetailItem
{
    public int Id { get; set; }
    public int Numara { get; set; }
    public string KelimeTuruAd { get; set; } = string.Empty;
    public string KelimeTuruRenk { get; set; } = string.Empty;
    public string? BicimAd { get; set; }
    public string? DuzeyAd { get; set; }
    public string Tanim { get; set; } = string.Empty;
    public List<string> OrnekCumleler { get; set; } = new();
}

public class MedyaItem
{
    public int Id { get; set; }
    public string DosyaYolu { get; set; } = string.Empty;
    public string DosyaTipi { get; set; } = string.Empty;
    public string? Aciklama { get; set; }
}

// ===================== ANLAM =====================

public class AnlamCreateViewModel
{
    public int MaddeBasiId { get; set; }
    public string Kelime { get; set; } = string.Empty;

    [Required(ErrorMessage = "Kelime türü seçiniz")]
    [Display(Name = "Kelime Türü")]
    public int KelimeTuruId { get; set; }

    [Display(Name = "Biçem")]
    public int? BicimId { get; set; }

    [Display(Name = "Düzey")]
    public int? DuzeyId { get; set; }

    [Required(ErrorMessage = "Tanım gereklidir")]
    [Display(Name = "Tanım")]
    public string Tanim { get; set; } = string.Empty;

    [Display(Name = "Örnek Cümleler (her satıra bir cümle)")]
    public string? OrnekCumlelerText { get; set; }

    public SelectList? KelimeTurleri { get; set; }
    public SelectList? Bicimler { get; set; }
    public SelectList? Duzeyler { get; set; }
}

public class AnlamEditViewModel : AnlamCreateViewModel
{
    public int Id { get; set; }
    public int Numara { get; set; }
}

// ===================== MEDYA =====================

public class MedyaUploadViewModel
{
    public int MaddeBasiId { get; set; }
    public string Kelime { get; set; } = string.Empty;

    [Required(ErrorMessage = "Dosya seçiniz")]
    [Display(Name = "Dosya")]
    public IFormFile Dosya { get; set; } = null!;

    [Required(ErrorMessage = "Dosya tipi seçiniz")]
    [Display(Name = "Dosya Tipi")]
    public string DosyaTipi { get; set; } = string.Empty; // "gorsel" veya "ses"

    [MaxLength(500)]
    [Display(Name = "Açıklama")]
    public string? Aciklama { get; set; }
}

// ===================== GÖZAT =====================

public class BrowseViewModel
{
    public List<BrowseTemaItem> Temalar { get; set; } = new();
    public List<BrowseKonuItem>? Konular { get; set; }
    public List<MaddeBasiListItem>? Maddeler { get; set; }
    public int? SeciliTemaId { get; set; }
    public int? SeciliKonuId { get; set; }
    public string? SeciliTemaAd { get; set; }
    public string? SeciliKonuAd { get; set; }
}

public class BrowseTemaItem
{
    public int Id { get; set; }
    public string Ad { get; set; } = string.Empty;
    public int KonuSayisi { get; set; }
    public int MaddeSayisi { get; set; }
}

public class BrowseKonuItem
{
    public int Id { get; set; }
    public string Ad { get; set; } = string.Empty;
    public int MaddeSayisi { get; set; }
}
