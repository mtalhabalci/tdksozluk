using System.ComponentModel.DataAnnotations;

namespace AspnetCoreMvcStarter.Models;

public class Tema
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Ad { get; set; } = string.Empty;

    public int Sira { get; set; }

    public ICollection<Konu> Konular { get; set; } = new List<Konu>();
}

public class Konu
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Ad { get; set; } = string.Empty;

    public int Sira { get; set; }

    public int TemaId { get; set; }
    public Tema Tema { get; set; } = null!;

    public ICollection<MaddeBasi> MaddeBaslari { get; set; } = new List<MaddeBasi>();
}

public class KelimeTuru
{
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string Ad { get; set; } = string.Empty;

    [MaxLength(7)]
    public string Renk { get; set; } = "#000000"; // hex color
}

public class Bicim
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Ad { get; set; } = string.Empty;
}

public class Duzey
{
    public int Id { get; set; }

    [Required, MaxLength(10)]
    public string Ad { get; set; } = string.Empty;

    public int Sira { get; set; }
}
