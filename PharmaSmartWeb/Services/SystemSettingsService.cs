using Microsoft.EntityFrameworkCore;
using PharmaSmartWeb.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace PharmaSmartWeb.Services
{
    // تنفيذ مزود إعدادات النظام - يقرأ العملة من قاعدة البيانات
    public class SystemSettingsService : ISystemSettingsService
    {
        private readonly ApplicationDbContext _context;
        private string _currency;

        public SystemSettingsService(ApplicationDbContext context)
        {
            _context = context;
        }

        // يقرأ العملة من أول فرع نشط في قاعدة البيانات، مع fallback لـ R.Y
        public string Currency
        {
            get
            {
                if (_currency == null)
                {
                    // تحميل متزامن (Sync over Async) - مقبول في DI Scoped لأن الخدمة مسجلة كـ Scoped
                    try
                    {
                        var branch = _context.Branches
                            .Include(b => b.DefaultCurrency)
                            .FirstOrDefault(b => b.IsActive == true);
                        _currency = branch?.DefaultCurrency?.CurrencyCode
                                 ?? branch?.DefaultCurrency?.CurrencyName
                                 ?? "R.Y";
                    }
                    catch
                    {
                        _currency = "R.Y";
                    }
                }
                return _currency;
            }
        }

        private CompanySettings _settingsCache;
        private CompanySettings GetSettings()
        {
            if (_settingsCache == null)
            {
                try
                {
                    _settingsCache = _context.CompanySettings.FirstOrDefault();
                }
                catch { }
            }
            return _settingsCache;
        }

        public string CompanyName
        {
            get
            {
                var settings = GetSettings();
                return settings?.CompanyName ?? "PharmaSmart ERP";
            }
        }

        public string CompanyLogoPath
        {
            get
            {
                var settings = GetSettings();
                return settings?.CompanyLogoPath ?? "/images/Logo.png";
            }
        }
    }
}
