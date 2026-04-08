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
    public class AccountController : BaseController
    {
        private readonly IPasswordHasher<Users> _passwordHasher;

        public AccountController(ApplicationDbContext context, IPasswordHasher<Users> passwordHasher)
            : base(context)
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
            if (User.Identity.IsAuthenticated)
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction("Login", new { returnUrl });
            }

            Response.Cookies.Delete("ActiveBranchId");

            if (returnUrl != null && returnUrl.Contains("HandleError")) returnUrl = null;
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // ==========================================
        // 2. معالجة تسجيل الدخول (POST)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password, string returnUrl = null)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "يرجى إدخال اسم المستخدم وكلمة المرور.";
                return View();
            }

            string cleanUsername = username.Trim();

            // ✅ الخطوة 1: جلب المستخدم بالاسم فقط — لا تُقارَن كلمة المرور في الاستعلام
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == cleanUsername);

            if (user == null || user.IsActive == false)
            {
                ViewBag.Error = "اسم المستخدم أو كلمة المرور غير صحيحة، أو الحساب محظور.";
                return View();
            }

            // ✅ الخطوة 2: التحقق من كلمة المرور بأمان
            bool passwordValid = false;
            var verifyResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash ?? "", password);

            if (verifyResult == PasswordVerificationResult.Success ||
                verifyResult == PasswordVerificationResult.SuccessRehashNeeded)
            {
                passwordValid = true;
                // إذا احتاج الهاش لتحديث (Rehash)، يتم تلقائياً
                if (verifyResult == PasswordVerificationResult.SuccessRehashNeeded)
                {
                    user.PasswordHash = _passwordHasher.HashPassword(user, password);
                    _context.Update(user);
                    await _context.SaveChangesAsync();
                }
            }
            else
            {
                // ✅ مسار الهجرة: إذا كانت كلمة المرور لا تزال نصاً صريحاً (الحسابات القديمة)،
                // نتحقق منها مرة واحدة فقط ثم نُشفِّرها فوراً للمستقبل.
                if (!string.IsNullOrEmpty(user.PasswordHash) && user.PasswordHash == password)
                {
                    user.PasswordHash = _passwordHasher.HashPassword(user, password);
                    _context.Update(user);
                    await _context.SaveChangesAsync();
                    passwordValid = true;
                }
            }

            if (!passwordValid)
            {
                ViewBag.Error = "اسم المستخدم أو كلمة المرور غير صحيحة، أو الحساب محظور.";
                return View();
            }

            // جلب اسم الدور
            string roleName = "User";
            if (user.RoleId > 0)
            {
                var role = await _context.Userroles.AsNoTracking()
                    .FirstOrDefaultAsync(r => r.RoleId == user.RoleId);
                roleName = role?.RoleArabicName ?? role?.RoleName ?? "User";
            }

            // جلب اسم الفرع
            string branchName = "فرع غير محدد";
            if (user.DefaultBranchId > 0)
            {
                var branch = await _context.Branches.AsNoTracking()
                    .FirstOrDefaultAsync(b => b.BranchId == user.DefaultBranchId);
                branchName = branch?.BranchName ?? "فرع غير محدد";
            }

            // بناء Claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim("UserID", user.UserId.ToString()),
                new Claim("RoleID", user.RoleId.ToString()),
                new Claim(ClaimTypes.Role, roleName),
                new Claim("BranchID", user.DefaultBranchId?.ToString() ?? "1"),
                new Claim("BranchName", branchName)
            };

            if (user.RoleId == 1)
                claims.Add(new Claim(ClaimTypes.Role, "SuperAdmin"));
            }

            var claimsIdentity = new ClaimsIdentity(
                claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(12)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            await RecordLog("Login", "Account",
                $"المستخدم {user.Username} سجل دخوله بنجاح من {branchName}.");

            if (!string.IsNullOrEmpty(returnUrl) &&
                Url.IsLocalUrl(returnUrl) &&
                !returnUrl.Contains("HandleError"))
            {
                return Redirect(returnUrl);

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
                await RecordLog("Logout", "Account", "قام المستخدم بتسجيل الخروج بنجاح.");

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
        public async Task<IActionResult> ChangePassword(
            string currentPassword, string newPassword, string confirmPassword)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == User.Identity.Name);

            if (user == null)
            {
                ViewBag.Error = "لم يتم العثور على المستخدم.";
                return View();
            }

            // ✅ التحقق من كلمة المرور الحالية باستخدام الهاش
            bool currentPasswordValid = false;
            var verifyResult = _passwordHasher.VerifyHashedPassword(
                user, user.PasswordHash ?? "", currentPassword);

            if (verifyResult == PasswordVerificationResult.Success ||
                verifyResult == PasswordVerificationResult.SuccessRehashNeeded)
            {
                currentPasswordValid = true;
            }
            else
            {
                // مسار الهجرة للحسابات القديمة التي لم تسجّل دخولاً بعد
                if (!string.IsNullOrEmpty(user.PasswordHash) && user.PasswordHash == currentPassword)
                {
                    currentPasswordValid = true;
                }
            }

            if (!currentPasswordValid)
            {
                ViewBag.Error = "كلمة المرور الحالية غير صحيحة!";
                return View();
            }

            if (newPassword != confirmPassword)
            {
                ViewBag.Error = "كلمة المرور الجديدة غير متطابقة!";
                return View();
            }

            // ✅ حفظ كلمة المرور الجديدة مُشفَّرة (Hashed)
            user.PasswordHash = _passwordHasher.HashPassword(user, newPassword);
            _context.Update(user);
            await _context.SaveChangesAsync();

            await RecordLog("Update", "Account", "تم تغيير كلمة المرور بنجاح.");
            return RedirectToAction("Index", "Home");
        }
    }
}
