//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;
//using PharmaSmartWeb.Models;
//using System.Threading.Tasks;
//using Microsoft.AspNetCore.Authorization;
//using System.Security.Claims;
//using System.Collections.Generic;
//using Microsoft.AspNetCore.Authentication;
//using Microsoft.AspNetCore.Authentication.Cookies;
//using System.Linq;
//using System;

//namespace PharmaSmartWeb.Controllers
//{
//    // 🚀 يرث من BaseController للوصول لمحرك العزل وخدمة RecordLog
//    public class AccountController : BaseController
//    {
//        public AccountController(ApplicationDbContext context) : base(context)
//        {
//        }

//        // ==========================================
//        // 1. شاشة تسجيل الدخول (GET)
//        // ==========================================
//        [HttpGet]
//        // 🛡️ الحل التقني: منع الكاش يضمن توليد مفتاح أمان (CSRF Token) جديد دائماً ويحل مشكلة "النقرة الثانية"
//        [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
//        public async Task<IActionResult> Login(string returnUrl = null)
//        {
//            // 🚀 العزل الأمني: مسح أي كوكيز قديمة (بما فيها ActiveBranchId) فور فتح الصفحة لضمان بيئة نظيفة 100%
//            if (User.Identity.IsAuthenticated || Request.Cookies.Count > 0)
//            {
//                foreach (var cookie in Request.Cookies.Keys)
//                {
//                    Response.Cookies.Delete(cookie);
//                }
//                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
//            }

//            // تنظيف الرابط من أي مخلفات أخطاء سابقة
//            if (returnUrl != null && returnUrl.Contains("HandleError")) returnUrl = null;

//            ViewData["ReturnUrl"] = returnUrl;
//            return View();
//        }

//        // ==========================================
//        // 2. معالجة عملية تسجيل الدخول (POST)
//        // ==========================================
//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> Login(string username, string password, string returnUrl = null)
//        {
//            if (string.IsNullOrEmpty(username)) return View();

//            // 🚀 الإجراء الوقائي: مسح المسافات الفارغة (Trim) لحل مشاكل الإدخال
//            string cleanUsername = username.Trim();

//            // 1. التحقق من المستخدم في قاعدة البيانات
//            var user = await _context.Users
//                .FirstOrDefaultAsync(u => u.Username == cleanUsername && u.PasswordHash == password);

//            if (user == null || user.IsActive == false)
//            {
//                ViewBag.Error = "اسم المستخدم أو كلمة المرور غير صحيحة، أو الحساب محظور.";
//                return View();
//            }

//            // 2. جلب اسم الدور واسم الفرع بشكل آمن ومنفصل لضمان استقرار الربط
//            string roleName = "User";
//            if (user.RoleId > 0)
//            {
//                var role = await _context.Userroles.AsNoTracking().FirstOrDefaultAsync(r => r.RoleId == user.RoleId);
//                roleName = role?.RoleArabicName ?? role?.RoleName ?? "User";
//            }

//            string branchName = "فرع غير محدد";
//            if (user.DefaultBranchId > 0)
//            {
//                var branch = await _context.Branches.AsNoTracking().FirstOrDefaultAsync(b => b.BranchId == user.DefaultBranchId);
//                branchName = branch?.BranchName ?? "فرع غير محدد";
//            }

//            // 3. بناء الهوية البرمجية (Claims)
//            var claims = new List<Claim>
//            {
//                new Claim(ClaimTypes.Name, user.Username),
//                new Claim("UserID", user.UserId.ToString()),
//                new Claim("RoleID", user.RoleId.ToString()),
//                new Claim("RoleName", roleName),

//                // 🚀 تأسيس العزل: الفرع الافتراضي للموظف، ونقطة البداية للمدير
//                new Claim("BranchID", user.DefaultBranchId?.ToString() ?? "1"),
//                new Claim("BranchName", branchName)
//            };

//            // 4. 🚀 محرك الصلاحيات الفوري:
//            // جلب كافة الصلاحيات وزرعها في الكوكي لكي تظهر الأزرار في القائمة الجانبية فوراً
//            var permissions = await _context.Screenpermissions
//                .Include(p => p.Screen)
//                .Where(p => p.RoleId == user.RoleId)
//                .AsNoTracking()
//                .ToListAsync();

//            foreach (var p in permissions)
//            {
//                if (p.CanView) claims.Add(new Claim("Permission", $"{p.Screen.ScreenName}.View"));
//                if (p.CanAdd) claims.Add(new Claim("Permission", $"{p.Screen.ScreenName}.Add"));
//                if (p.CanEdit) claims.Add(new Claim("Permission", $"{p.Screen.ScreenName}.Edit"));
//                if (p.CanDelete) claims.Add(new Claim("Permission", $"{p.Screen.ScreenName}.Delete"));
//                if (p.CanPrint) claims.Add(new Claim("Permission", $"{p.Screen.ScreenName}.Print"));
//            }

//            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

//            var authProperties = new AuthenticationProperties
//            {
//                IsPersistent = true, // تذكرني
//                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(12) // صلاحية الجلسة
//            };

//            await HttpContext.SignInAsync(
//                CookieAuthenticationDefaults.AuthenticationScheme,
//                new ClaimsPrincipal(claimsIdentity),
//                authProperties);

//            // 🛡️ توثيق عملية الدخول في سجلات الرقابة (عبر الموتور المورث من BaseController)
//            // نمرر ActiveBranchId الافتراضي الذي تم تأسيسه للتو
//            await RecordLog("Login", "Account", $"المستخدم {user.Username} سجل دخوله بنجاح من {branchName}.");

//            // التوجيه الذكي
//            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) && !returnUrl.Contains("HandleError"))
//            {
//                return Redirect(returnUrl);
//            }

//            return RedirectToAction("Index", "Home");
//        }

//        // ==========================================
//        // 3. دالة تسجيل الخروج 
//        // ==========================================
//        [HttpGet]
//        public async Task<IActionResult> Logout()
//        {
//            if (User.Identity.IsAuthenticated)
//            {
//                await RecordLog("Logout", "Account", "قام المستخدم بتسجيل الخروج بنجاح.");
//            }

//            // 🚀 العزل الأمني: تدمير كوكي "الفرع النشط" لكي لا يرثه من يستخدم الجهاز لاحقاً
//            Response.Cookies.Delete("ActiveBranchId");

//            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
//            return RedirectToAction("Login", "Account");
//        }

//        // ==========================================
//        // 4. تغيير كلمة المرور
//        // ==========================================
//        [Authorize]
//        [HttpGet]
//        public IActionResult ChangePassword() => View();

//        [Authorize]
//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
//        {
//            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == User.Identity.Name);
//            if (user == null || user.PasswordHash != currentPassword)
//            {
//                ViewBag.Error = "كلمة المرور الحالية غير صحيحة!";
//                return View();
//            }

//            if (newPassword != confirmPassword)
//            {
//                ViewBag.Error = "كلمة المرور الجديدة غير متطابقة!";
//                return View();
//            }

//            user.PasswordHash = newPassword;
//            _context.Update(user);
//            await _context.SaveChangesAsync();

//            await RecordLog("Update", "Account", "تم تغيير كلمة المرور بنجاح.");
//            return RedirectToAction("Index", "Home");
//        }
//    }
//}
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmaSmartWeb.Models;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using System.Linq;
using System;

namespace PharmaSmartWeb.Controllers
{
    // 🚀 يرث من BaseController للوصول لمحرك العزل وخدمة RecordLog
    public class AccountController : BaseController
    {
        private readonly IPasswordHasher<Users> _passwordHasher;

        public AccountController(ApplicationDbContext context, IPasswordHasher<Users> passwordHasher) : base(context)
        {
            _passwordHasher = passwordHasher;
        }

        // ==========================================
        // 1. شاشة تسجيل الدخول (GET)
        // ==========================================
        [HttpGet]
        [AllowAnonymous]
        [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> Login(string returnUrl = null)
        {
            // مسح جلسة المستخدم الحالي فقط (وليس كل الكوكيز لتجنب مشكلة CSRF Token)
            if (User.Identity.IsAuthenticated)
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                // 🛑 الحل الحاسم لمشكلة الخطأ عند التبديل بين الحسابات:
                // يجب إعادة توجيه المستخدم لنفس الصفحة بعد تسجيل الخروج لتنظيف الـ Identity تماماً 
                // وتوليد Antiforgery Token جديد متوافق مع زائر غير مسجل (Anonymous)
                return RedirectToAction("Login", new { returnUrl });
            }

            // مسح كوكيز الفروع لضمان بيئة نظيفة للمستخدم الجديد
            Response.Cookies.Delete("ActiveBranchId");

            // تنظيف الرابط من أي مخلفات أخطاء سابقة
            if (returnUrl != null && returnUrl.Contains("HandleError")) returnUrl = null;

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // ==========================================
        // 2. معالجة عملية تسجيل الدخول (POST)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password, string returnUrl = null)
        {
            if (string.IsNullOrEmpty(username)) return View();

            // 🚀 الإجراء الوقائي: مسح المسافات الفارغة (Trim) لحل مشاكل الإدخال
            string cleanUsername = username.Trim();

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == cleanUsername);

            if (user == null || user.IsActive == false)
            {
                ViewBag.Error = "اسم المستخدم أو كلمة المرور غير صحيحة، أو الحساب محظور.";
                return View();
            }

            // 🚀 Migration Logic: plaintext → hash on first successful login; otherwise verify with IPasswordHasher.
            if (user.PasswordHash == password)
            {
                user.PasswordHash = _passwordHasher.HashPassword(user, password);
                _context.Users.Update(user);
                await _context.SaveChangesAsync();
            }
            else
            {
                var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
                if (result != PasswordVerificationResult.Success && result != PasswordVerificationResult.SuccessRehashNeeded)
                {
                    ViewBag.Error = "اسم المستخدم أو كلمة المرور غير صحيحة، أو الحساب محظور.";
                    return View();
                }
                if (result == PasswordVerificationResult.SuccessRehashNeeded)
                {
                    user.PasswordHash = _passwordHasher.HashPassword(user, password);
                    _context.Users.Update(user);
                    await _context.SaveChangesAsync();
                }
            }

            // 2. جلب اسم الدور واسم الفرع بشكل آمن ومنفصل لضمان استقرار الربط
            string roleName = "User";
            if (user.RoleId > 0)
            {
                var role = await _context.Userroles.AsNoTracking().FirstOrDefaultAsync(r => r.RoleId == user.RoleId);
                roleName = role?.RoleArabicName ?? role?.RoleName ?? "User";
            }

            string branchName = "فرع غير محدد";
            if (user.DefaultBranchId > 0)
            {
                var branch = await _context.Branches.AsNoTracking().FirstOrDefaultAsync(b => b.BranchId == user.DefaultBranchId);
                branchName = branch?.BranchName ?? "فرع غير محدد";
            }

            // 3. بناء الهوية البرمجية (Claims) - فقط البيانات الأساسية لتجنب تضخم الكوكي
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim("UserID", user.UserId.ToString()),
                new Claim("RoleID", user.RoleId.ToString()),
                new Claim(ClaimTypes.Role, roleName),
                
                // 🚀 تأسيس العزل: الفرع الافتراضي للموظف، ونقطة البداية للمدير
                new Claim("BranchID", user.DefaultBranchId?.ToString() ?? "1"),
                new Claim("BranchName", branchName)
            };

            // 🚀 إضافة الهوية الصريحة لمدير النظام لتفادي اختفائها في الواجهات (User.IsInRole)
            if (user.RoleId == 1)
            {
                claims.Add(new Claim(ClaimTypes.Role, "SuperAdmin"));
            }

            // تم إزالة حلقة (foreach) الخاصة بـ Permissions من هنا لتخفيف حجم الـ Cookie وحل خطأ ERR_HTTP2_PROTOCOL_ERROR
            // سيتولى كلاس (ClaimsTransformer) جلب الصلاحيات ديناميكياً في الذاكرة لاحقاً

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true, // تذكرني
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(12) // صلاحية الجلسة
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            // 🛡️ توثيق عملية الدخول في سجلات الرقابة (عبر الموتور المورث من BaseController)
            // نمرر ActiveBranchId الافتراضي الذي تم تأسيسه للتو
            await RecordLog("Login", "Account", $"المستخدم {user.Username} سجل دخوله بنجاح من {branchName}.");

            // التوجيه الذكي
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) && !returnUrl.Contains("HandleError"))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        // ==========================================
        // 3. دالة تسجيل الخروج 
        // ==========================================
        [HttpGet]
        [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> Logout()
        {
            if (User.Identity.IsAuthenticated)
            {
                await RecordLog("Logout", "Account", "قام المستخدم بتسجيل الخروج بنجاح.");
            }

            // 🚀 العزل الأمني: تدمير كوكي "الفرع النشط" لكي لا يرثه من يستخدم الجهاز لاحقاً
            Response.Cookies.Delete("ActiveBranchId");

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }

        // ==========================================
        // 4. تغيير كلمة المرور
        // ==========================================
        [Authorize]
        [HttpGet]
        public IActionResult ChangePassword() => View();

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == User.Identity.Name);
            if (user == null)
            {
                ViewBag.Error = "تعذر تحميل بيانات المستخدم.";
                return View();
            }

            bool currentValid =
                user.PasswordHash == currentPassword
                || _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, currentPassword) is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;

            if (!currentValid)
            {
                ViewBag.Error = "كلمة المرور الحالية غير صحيحة!";
                return View();
            }

            if (newPassword != confirmPassword)
            {
                ViewBag.Error = "كلمة المرور الجديدة غير متطابقة!";
                return View();
            }

            user.PasswordHash = _passwordHasher.HashPassword(user, newPassword);
            _context.Update(user);
            await _context.SaveChangesAsync();

            await RecordLog("Update", "Account", "تم تغيير كلمة المرور بنجاح.");
            return RedirectToAction("Index", "Home");
        }
    }
}