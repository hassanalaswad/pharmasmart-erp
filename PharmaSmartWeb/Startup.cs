using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using PharmaSmartWeb.Models;
using Microsoft.AspNetCore.Authentication;

namespace PharmaSmartWeb
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();
            services.AddMemoryCache();

            services.AddTransient<IClaimsTransformation, PharmaSmartWeb.Security.ClaimsTransformer>();
            services.AddScoped<PharmaSmartWeb.Services.IAccountingEngine, PharmaSmartWeb.Services.AccountingEngine>();
            services.AddScoped<Microsoft.AspNetCore.Identity.IPasswordHasher<PharmaSmartWeb.Models.Users>, Microsoft.AspNetCore.Identity.PasswordHasher<PharmaSmartWeb.Models.Users>>();

            // ── قاعدة البيانات ─────────────────────────────────────────────
            string connectionString = Environment.GetEnvironmentVariable("DATABASE_URL") 
                ?? "server=localhost;database=dblast;uid=root;pwd=;";
            
            // دعم روابط Cloud (مثل Aiven) التي تبدأ بـ mysql://
            if (connectionString.StartsWith("mysql://", StringComparison.OrdinalIgnoreCase))
            {
                var uri = new Uri(connectionString);
                var userInfo = uri.UserInfo.Split(':');
                string user = userInfo[0];
                string pass = userInfo.Length > 1 ? userInfo[1] : "";
                string dbName = uri.AbsolutePath.TrimStart('/');
                connectionString = $"Server={uri.Host};Port={uri.Port};Database={dbName};Uid={user};Pwd={pass};SslMode=Required;";
            }
            
            // تحديد إصدار MySQL مسبقاً بدلاً من AutoDetect لمنع تعليق (Crash) التطبيق عند الإقلاع البطيء
            var serverVersion = new MySqlServerVersion(new Version(8, 0, 30));

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseMySql(connectionString, serverVersion,
                    mysqlOptions => mysqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(10),
                        errorNumbersToAdd: null)));

            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.Cookie.Name = "PharmaSmart_FastToken";
                    options.LoginPath = "/Account/Login";        
                    options.AccessDeniedPath = "/Home/AccessDenied"; 
                });

            // ==============================================================
            // 🛡️ تسجيل شبكة السياسات المركزية (Policy Matrix Configuration)
            // ==============================================================
            services.AddAuthorization(options =>
            {
                // سياسات الأدوية الموجودة مسبقًا
                options.AddPolicy("CanAddDrug", policy => policy.RequireClaim("Permission", "Drugs.Add"));
                options.AddPolicy("CanEditDrug", policy => policy.RequireClaim("Permission", "Drugs.Edit"));
                options.AddPolicy("CanDeleteDrug", policy => policy.RequireClaim("Permission", "Drugs.Delete"));

                // ⚙️ 1. سياسات لوحة الإعدادات (Settings Hub)
                options.AddPolicy("Roles.View", policy => policy.RequireClaim("Permission", "Roles.View"));
                options.AddPolicy("Roles.Edit", policy => policy.RequireClaim("Permission", "Roles.Edit"));
                options.AddPolicy("Users.View", policy => policy.RequireClaim("Permission", "Users.View"));

                // 🛒 2. سياسات المبيعات والمرتجعات (Sales Hub)
                options.AddPolicy("Sales.Add",  policy => policy.RequireClaim("Permission", "Sales.Add"));
                options.AddPolicy("Sales.View", policy => policy.RequireClaim("Permission", "Sales.View"));
                options.AddPolicy("SalesReturn.View", policy => policy.RequireClaim("Permission", "SalesReturn.View"));

                // 📦 3. سياسات المشتريات والمخزون (Inventory Hub)
                options.AddPolicy("Purchases.View", policy => policy.RequireClaim("Permission", "Purchases.View"));
                options.AddPolicy("Drug.View", policy => policy.RequireClaim("Permission", "Drug.View"));

                // 💰 4. سياسات الإدارة المالية (Finance Hub)
                options.AddPolicy("Journal.View", policy => policy.RequireClaim("Permission", "JournalEntries.View"));
                options.AddPolicy("Accounts.View", policy => policy.RequireClaim("Permission", "Accounts.View"));
            });

            // 🚀 الخطوة الأهم: حقن خدمة التنقل الديناميكي لتعمل في الواجهة
            services.AddHttpContextAccessor();
            services.AddScoped<PharmaSmartWeb.Services.INavigationService, PharmaSmartWeb.Services.NavigationService>();
            
            // إضافة مزود الإعدادات للنظام (Dynamic Currency Architecture)
            services.AddScoped<PharmaSmartWeb.Services.ISystemSettingsService, PharmaSmartWeb.Services.SystemSettingsService>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseStatusCodePagesWithReExecute("/Home/HandleError/{0}");
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}