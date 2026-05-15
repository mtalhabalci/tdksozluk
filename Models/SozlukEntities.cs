using System.ComponentModel.DataAnnotations;

namespace AspnetCoreMvcStarter.Models;

public class MaddeBasi
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Kelime { get; set; } = string.Empty;

    public int KonuId { get; set; }
    public Konu Konu { get; set; } = null!;

    public string OlusturanId { get; set; } = string.Empty;
    public AppUser Olusturan { get; set; } = null!;

    public DateTime OlusturmaTarihi { get; set; } = DateTime.UtcNow;
    public DateTime GuncellemeTarihi { get; set; } = DateTime.UtcNow;

    public ICollection<Anlam> Anlamlar { get; set; } = new List<Anlam>();
    public ICollection<Medya> Medyalar { get; set; } = new List<Medya>();
}

public class Anlam
{
    public int Id { get; set; }

    public int MaddeBasiId { get; set; }
    public MaddeBasi MaddeBasi { get; set; } = null!;

    public int Numara { get; set; } // 1, 2, 3... (madde başı bazında)

    public int KelimeTuruId { get; set; }
    public KelimeTuru KelimeTuru { get; set; } = null!;

    public int? BicimId { get; set; }
    public Bicim? Bicim { get; set; }

    public int? DuzeyId { get; set; }
    public Duzey? Duzey { get; set; }

    [Required]
    public string Tanim { get; set; } = string.Empty;

    public string OlusturanId { get; set; } = string.Empty;
    public AppUser Olusturan { get; set; } = null!;

    public DateTime OlusturmaTarihi { get; set; } = DateTime.UtcNow;

    public ICollection<OrnekCumle> OrnekCumleler { get; set; } = new List<OrnekCumle>();
}

public class OrnekCumle
{
    public int Id { get; set; }

    public int AnlamId { get; set; }
    public Anlam Anlam { get; set; } = null!;

    [Required]
    public string Cumle { get; set; } = string.Empty;

    public int Sira { get; set; }
}

public class Medya
{
    public int Id { get; set; }

    public int MaddeBasiId { get; set; }
    public MaddeBasi MaddeBasi { get; set; } = null!;

    [Required, MaxLength(500)]
    public string DosyaYolu { get; set; } = string.Empty;

    [Required, MaxLength(10)]
    public string DosyaTipi { get; set; } = string.Empty; // "gorsel" veya "ses"

    [MaxLength(500)]
    public string? Aciklama { get; set; }

    public DateTime YuklemeTarihi { get; set; } = DateTime.UtcNow;
}
