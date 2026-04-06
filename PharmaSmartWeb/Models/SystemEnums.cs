namespace PharmaSmartWeb.Models
{
    /// <summary>
    /// المرجع المركزي (Single Source of Truth) لكافة الثوابت في النظام.
    /// تم استبدال الأرقام الصامتة (Magic Numbers) بهذا الـ Enum لضمان نظافة الكود والتوافقية المعمارية.
    /// </summary>
    public enum SystemRoles
    {
        SuperAdmin = 1,
        Admin = 2,
        Pharmacist = 3,
        Accountant = 4
    }
}
