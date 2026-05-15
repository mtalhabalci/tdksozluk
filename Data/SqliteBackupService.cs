using Microsoft.EntityFrameworkCore;

namespace AspnetCoreMvcStarter.Data;

public class SqliteBackupService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<SqliteBackupService> _logger;
    private readonly IConfiguration _config;

    public SqliteBackupService(
        IServiceProvider services,
        IWebHostEnvironment env,
        ILogger<SqliteBackupService> logger,
        IConfiguration config)
    {
        _services = services;
        _env = env;
        _logger = logger;
        _config = config;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // SQLite kullanılmıyorsa servis devre dışı
        using (var scope = _services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            if (db.Database.ProviderName != "Microsoft.EntityFrameworkCore.Sqlite")
            {
                _logger.LogInformation("SQLite kullanılmıyor, yedekleme servisi devre dışı.");
                return;
            }
        }

        // Uygulama açılınca hemen bir yedek al
        TryBackup();

        while (!stoppingToken.IsCancellationRequested)
        {
            // Yarın gece yarısına kadar bekle
            var now = DateTime.Now;
            var nextMidnight = now.Date.AddDays(1);
            var delay = nextMidnight - now;
            _logger.LogInformation("Bir sonraki SQLite yedeği: {Next:yyyy-MM-dd HH:mm}", nextMidnight);

            await Task.Delay(delay, stoppingToken);

            TryBackup();
        }
    }

    private void TryBackup()
    {
        try
        {
            // DB yolunu connection string'den oku
            var connStr = _config.GetConnectionString("DefaultConnection");
            string dbPath;
            if (!string.IsNullOrWhiteSpace(connStr))
            {
                var sb = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder(connStr);
                dbPath = sb.DataSource;
            }
            else
            {
                dbPath = Path.Combine(_env.ContentRootPath, "data", "tdksozluk.db");
            }
            if (!File.Exists(dbPath)) return;

            // Yedek klasörü: appsettings'ten oku, yoksa uygulama altında Backups/
            var backupDir = _config["BackupPath"] is string bp && !string.IsNullOrWhiteSpace(bp)
                ? bp
                : Path.Combine(_env.ContentRootPath, "Backups");

            Directory.CreateDirectory(backupDir);

            var todayFile = Path.Combine(backupDir, $"tdksozluk_{DateTime.Now:yyyy-MM-dd}.db");

            if (File.Exists(todayFile))
            {
                _logger.LogInformation("Bugünün SQLite yedeği zaten mevcut: {File}", todayFile);
                return;
            }

            File.Copy(dbPath, todayFile, overwrite: false);
            _logger.LogInformation("SQLite yedeği alındı: {File}", todayFile);

            // 30 günden eski yedekleri temizle
            var cutoff = DateTime.Now.AddDays(-30);
            foreach (var old in Directory.GetFiles(backupDir, "tdksozluk_*.db"))
            {
                if (File.GetCreationTime(old) < cutoff)
                {
                    File.Delete(old);
                    _logger.LogInformation("Eski yedek silindi: {File}", old);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SQLite yedekleme sırasında hata oluştu.");
        }
    }
}
