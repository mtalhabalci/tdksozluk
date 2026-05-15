using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AspnetCoreMvcStarter.Data;
using AspnetCoreMvcStarter.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

// Database: PostgreSQL (prod) veya SQLite (dev, Docker yoksa)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;
if (builder.Environment.IsDevelopment() && connectionString.Contains("Host="))
{
    // PostgreSQL'e bağlanabilir miyiz kontrol et
    try
    {
        using var testConn = new Npgsql.NpgsqlConnection(connectionString);
        testConn.Open();
        testConn.Close();
        builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
    }
    catch
    {
        // PostgreSQL yoksa SQLite'a düş
        var sqlitePath = Path.Combine(builder.Environment.ContentRootPath, "tdksozluk.db");
        builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite($"Data Source={sqlitePath}"));
        Console.WriteLine($"⚠ PostgreSQL bağlantısı kurulamadı, SQLite kullanılıyor: {sqlitePath}");
    }
}
else
{
    builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
}

// ASP.NET Identity
builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

// File upload limit: 25MB
builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = 25 * 1024 * 1024;
});
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 25 * 1024 * 1024;
});

// SQLite günlük yedekleme servisi
builder.Services.AddHostedService<AspnetCoreMvcStarter.Data.SqliteBackupService>();

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Auto-migrate & seed on startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var db = services.GetRequiredService<AppDbContext>();
    if (db.Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
        db.Database.EnsureCreated();
    else
        db.Database.Migrate();
    await DbSeeder.SeedAsync(services);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
