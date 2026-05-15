using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using AspnetCoreMvcStarter.Models;

namespace AspnetCoreMvcStarter.Data;

public class AppDbContext : IdentityDbContext<AppUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Tema> Temalar => Set<Tema>();
    public DbSet<Konu> Konular => Set<Konu>();
    public DbSet<KelimeTuru> KelimeTurleri => Set<KelimeTuru>();
    public DbSet<Bicim> Bicimler => Set<Bicim>();
    public DbSet<Duzey> Duzeyler => Set<Duzey>();
    public DbSet<MaddeBasi> MaddeBaslari => Set<MaddeBasi>();
    public DbSet<Anlam> Anlamlar => Set<Anlam>();
    public DbSet<OrnekCumle> OrnekCumleler => Set<OrnekCumle>();
    public DbSet<Medya> Medyalar => Set<Medya>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Tema -> Konu (cascade)
        builder.Entity<Konu>()
            .HasOne(k => k.Tema)
            .WithMany(t => t.Konular)
            .HasForeignKey(k => k.TemaId)
            .OnDelete(DeleteBehavior.Cascade);

        // Konu -> MaddeBasi (cascade)
        builder.Entity<MaddeBasi>()
            .HasOne(m => m.Konu)
            .WithMany(k => k.MaddeBaslari)
            .HasForeignKey(m => m.KonuId)
            .OnDelete(DeleteBehavior.Cascade);

        // MaddeBasi -> Anlam (cascade)
        builder.Entity<Anlam>()
            .HasOne(a => a.MaddeBasi)
            .WithMany(m => m.Anlamlar)
            .HasForeignKey(a => a.MaddeBasiId)
            .OnDelete(DeleteBehavior.Cascade);

        // Anlam -> OrnekCumle (cascade)
        builder.Entity<OrnekCumle>()
            .HasOne(o => o.Anlam)
            .WithMany(a => a.OrnekCumleler)
            .HasForeignKey(o => o.AnlamId)
            .OnDelete(DeleteBehavior.Cascade);

        // MaddeBasi -> Medya (cascade)
        builder.Entity<Medya>()
            .HasOne(m => m.MaddeBasi)
            .WithMany(mb => mb.Medyalar)
            .HasForeignKey(m => m.MaddeBasiId)
            .OnDelete(DeleteBehavior.Cascade);

        // Anlam -> KelimeTuru (restrict - tür silinirse anlam bozulmasın)
        builder.Entity<Anlam>()
            .HasOne(a => a.KelimeTuru)
            .WithMany()
            .HasForeignKey(a => a.KelimeTuruId)
            .OnDelete(DeleteBehavior.Restrict);

        // Anlam -> Bicim (optional, restrict)
        builder.Entity<Anlam>()
            .HasOne(a => a.Bicim)
            .WithMany()
            .HasForeignKey(a => a.BicimId)
            .OnDelete(DeleteBehavior.SetNull);

        // Anlam -> Duzey (optional, set null)
        builder.Entity<Anlam>()
            .HasOne(a => a.Duzey)
            .WithMany()
            .HasForeignKey(a => a.DuzeyId)
            .OnDelete(DeleteBehavior.SetNull);

        // MaddeBasi -> AppUser (restrict)
        builder.Entity<MaddeBasi>()
            .HasOne(m => m.Olusturan)
            .WithMany()
            .HasForeignKey(m => m.OlusturanId)
            .OnDelete(DeleteBehavior.Restrict);

        // Anlam -> AppUser (restrict)
        builder.Entity<Anlam>()
            .HasOne(a => a.Olusturan)
            .WithMany()
            .HasForeignKey(a => a.OlusturanId)
            .OnDelete(DeleteBehavior.Restrict);

        // İndexler
        builder.Entity<MaddeBasi>()
            .HasIndex(m => m.Kelime);

        builder.Entity<Tema>()
            .HasIndex(t => t.Sira);

        builder.Entity<Konu>()
            .HasIndex(k => new { k.TemaId, k.Sira });
    }
}
