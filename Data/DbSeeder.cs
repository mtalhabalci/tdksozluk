using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using AspnetCoreMvcStarter.Models;

namespace AspnetCoreMvcStarter.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<AppUser>>();
        var db = serviceProvider.GetRequiredService<AppDbContext>();
        var config = serviceProvider.GetRequiredService<IConfiguration>();
        var adminPassword = config["AdminSeedPassword"] ?? "Admin123";

        // Roller
        string[] roles = ["Admin", "Editor", "Viewer"];
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        // Varsayılan admin kullanıcı
        const string adminEmail = "admin@tdksozluk.com";
        if (await userManager.FindByEmailAsync(adminEmail) == null)
        {
            var admin = new AppUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = "Sistem Yöneticisi",
                EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(admin, adminPassword);
            if (result.Succeeded)
                await userManager.AddToRoleAsync(admin, "Admin");
        }

        // Kelime Türleri
        if (!await db.KelimeTurleri.AnyAsync())
        {
            db.KelimeTurleri.AddRange(
                new KelimeTuru { Ad = "İsim", Renk = "#F0AD4E" },       // sarı
                new KelimeTuru { Ad = "Sıfat", Renk = "#9B59B6" },      // mor
                new KelimeTuru { Ad = "Fiil", Renk = "#3498DB" },        // mavi
                new KelimeTuru { Ad = "Zarf", Renk = "#2ECC71" },        // yeşil
                new KelimeTuru { Ad = "Zamir", Renk = "#E74C3C" },       // kırmızı
                new KelimeTuru { Ad = "Edat", Renk = "#1ABC9C" },        // turkuaz
                new KelimeTuru { Ad = "Bağlaç", Renk = "#E67E22" },      // turuncu
                new KelimeTuru { Ad = "Ünlem", Renk = "#95A5A6" }        // gri
            );
        }

        // Biçem türleri
        if (!await db.Bicimler.AnyAsync())
        {
            db.Bicimler.AddRange(
                new Bicim { Ad = "mecaz" },
                new Bicim { Ad = "argo" },
                new Bicim { Ad = "halk ağzı" },
                new Bicim { Ad = "eskimiş" },
                new Bicim { Ad = "teklifsiz" }
            );
        }

        // Düzeyler
        if (!await db.Duzeyler.AnyAsync())
        {
            db.Duzeyler.AddRange(
                new Duzey { Ad = "A1", Sira = 1 },
                new Duzey { Ad = "A2", Sira = 2 },
                new Duzey { Ad = "B1", Sira = 3 },
                new Duzey { Ad = "B2", Sira = 4 },
                new Duzey { Ad = "C1", Sira = 5 },
                new Duzey { Ad = "C2", Sira = 6 }
            );
        }

        await db.SaveChangesAsync();
    }
}
