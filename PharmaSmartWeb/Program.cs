using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PharmaSmartWeb.Infrastructure;
using PharmaSmartWeb.Models;

namespace PharmaSmartWeb
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();

            // ── زرع الصلاحيات الافتراضية عند أول تشغيل ──────────────────
            using (var scope = host.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    var db = services.GetRequiredService<ApplicationDbContext>();
                    
                    // ── ضمان وجود عمود IsRead في جدول التنبيهات ──────────────────
                    try {
                        await Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.ExecuteSqlRawAsync(db.Database, 
                            "ALTER TABLE systemnotifications ADD COLUMN IsRead TINYINT(1) DEFAULT 0 AFTER ActionText;");
                    } catch { /* العمود موجود بالفعل */ }

                    await PermissionSeeder.SeedAsync(db);
                }
                catch (Exception ex)
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "خطأ أثناء زرع الصلاحيات الافتراضية.");
                }
            }

            await host.RunAsync();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
                    webBuilder.UseUrls("http://+:" + port);
                    webBuilder.UseStartup<Startup>();
                });

    }
}
