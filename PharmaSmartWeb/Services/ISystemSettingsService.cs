namespace PharmaSmartWeb.Services
{
    // واجهة مزود إعدادات النظام
    public interface ISystemSettingsService
    {
        // جلب رمز العملة الحالي
        string Currency { get; }
        
        // جلب اسم المؤسسة
        string CompanyName { get; }
        
        // جلب مسار الشعار
        string CompanyLogoPath { get; }
    }
}
