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
using PharmaSmartWeb.Infrastructure;

namespace PharmaSmartWeb
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IWebHostEnvironment environment)
        {
            Configuration = configuration;
            Environment = environment;
        }

        public IConfiguration Configuration { get; }
        public IWebHostEnvironment Environment { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews(options =>
            {
                // 🔒 الحماية الشاملة: كل صفحة تتطلب مصادقة بشكل افتراضي
                // لا يمكن الوصول لأي صفحة إلا بعد تسجيل الدخول
                options.Filters.Add(new Microsoft.AspNetCore.Mvc.Authorization.AuthorizeFilter());
            });
            services.AddMemoryCache();

            // طلبات fetch/AJAX: إرسال الرمز في الهيدر RequestVerificationToken
            services.AddAntiforgery(options => options.HeaderName = "RequestVerificationToken");

            services.AddTransient<IClaimsTransformation, PharmaSmartWeb.Security.ClaimsTransformer>();
            services.AddScoped<PharmaSmartWeb.Services.IAccountingEngine, PharmaSmartWeb.Services.AccountingEngine>();
            services.AddScoped<Microsoft.AspNetCore.Identity.IPasswordHasher<PharmaSmartWeb.Models.Users>, Microsoft.AspNetCore.Identity.PasswordHasher<PharmaSmartWeb.Models.Users>>();

            // ── قاعدة البيانات ─────────────────────────────────────────────
            string connectionString;
            try
            {
                connectionString = MySqlConnectionResolver.ResolveConnectionString(Configuration);
            }
            catch (InvalidOperationException)
            {
                if (Environment.IsDevelopment())
                {
                    connectionString = "server=127.0.0.1;port=3306;database=dblast;user=root;password=;";
                }
                else
                {
                    throw;
                }
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
            
            services.AddScoped<PharmaSmartWeb.Services.ISystemSettingsService, PharmaSmartWeb.Services.SystemSettingsService>();

            // ─── خدمة التنبؤ بالطلب الخارجي (Google Vertex AI) ───────────────
            // تُستخدم عند رفع ملف Excel خارجي — بيانات قاعدة البيانات تبقى مع Prophet
            services.AddHttpClient<PharmaSmartWeb.Services.GoogleVertexAiForecastService>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(20);
                client.DefaultRequestHeaders.Add("User-Agent", "PharmaSmartWeb/1.0");
            });
            services.AddScoped<PharmaSmartWeb.Services.IForecastApiService,
                               PharmaSmartWeb.Services.GoogleVertexAiForecastService>();

            // ─── خدمة إرسال التنبيهات عبر الواتساب ───────────────────────────
            services.AddHttpClient<PharmaSmartWeb.Services.IWhatsAppService, PharmaSmartWeb.Services.WhatsAppService>();

            // ─── محرك الإشعارات التلقائي والخدمات الخلفية ─────────────────────────
            services.AddScoped<PharmaSmartWeb.Services.NotificationEngine>();
            services.AddHostedService<PharmaSmartWeb.Infrastructure.NotificationBackgroundService>();
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