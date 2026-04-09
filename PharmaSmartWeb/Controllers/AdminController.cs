using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmaSmartWeb.Models;
using System.Threading.Tasks;
using PharmaSmartWeb.Filters;
using System.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace PharmaSmartWeb.Controllers
{
    /// <summary>
    /// متحكم الإدارة: مسؤول عن عرض سجلات النظام والعمليات الرقابية العليا
    /// </summary>
    [Authorize]
    public class AdminController : BaseController
    {
        // نعتمد على BaseController لتمرير سياق قاعدة البيانات
        public AdminController(ApplicationDbContext context) : base(context)
        {
        }

        // ==========================================
        // 🚀 شاشة الإعدادات العامة للمؤسسة (GET)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            if (!IsSuperAdmin)
                return RedirectToAction("AccessDenied", "Home");

            // جلب الإعدادات (Id=1 دائماً لأنه Singleton)، أو إنشاء واحد جديد إذا لم يوجد
            var settings = await _context.CompanySettings.FirstOrDefaultAsync(s => s.Id == 1);
            if (settings == null)
            {
                settings = new CompanySettings { Id = 1, CompanyName = "PharmaSmart ERP" };
            }

            return View(settings);
        }

        // ==========================================
        // 🚀 شاشة الإعدادات العامة للمؤسسة (POST)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(CompanySettings model, Microsoft.AspNetCore.Http.IFormFile logoFile)
        {
            if (!IsSuperAdmin)
                return RedirectToAction("AccessDenied", "Home");

            if (ModelState.IsValid)
            {
                try
                {
                    var existingSettings = await _context.CompanySettings.FirstOrDefaultAsync(s => s.Id == 1);
                    
                    // معالجة رفع الشعار
                    string uniqueFileName = existingSettings?.CompanyLogoPath;
                    if (logoFile != null && logoFile.Length > 0)
                    {
                        string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "company");
                        if (!Directory.Exists(uploadsFolder))
                            Directory.CreateDirectory(uploadsFolder);

                        uniqueFileName = "logo_" + DateTime.Now.ToString("yyyyMMddHHmmss") + Path.GetExtension(logoFile.FileName);
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await logoFile.CopyToAsync(fileStream);
                        }
                    }

                    if (existingSettings != null)
                    {
                        existingSettings.CompanyName = model.CompanyName;
                        if (logoFile != null)
                        {
                            existingSettings.CompanyLogoPath = uniqueFileName;
                        }
                        _context.Update(existingSettings);
                        await RecordLog("UpdateSettings", "Admin", $"تم تحديث إعدادات المؤسسة: {model.CompanyName}");
                    }
                    else
                    {
                        model.Id = 1;
                        model.CompanyLogoPath = uniqueFileName;
                        _context.CompanySettings.Add(model);
                        await RecordLog("CreateSettings", "Admin", $"تم تهيئة إعدادات المؤسسة الافتراضية: {model.CompanyName}");
                    }

                    await _context.SaveChangesAsync();
                    TempData["Success"] = "تم حفظ إعدادات المؤسسة بنجاح.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    TempData["Error"] = $"حدث خطأ أثناء حفظ الإعدادات: {ex.Message}";
                }
            }

            return View(model);
        }

        // ==========================================
        // 🚀 عرض سجلات الرقابة (System Logs)
        // ==========================================
        /// <summary>
        /// جلب كافة الحركات المسجلة في النظام وعرضها للمدير العام
        /// </summary>
        [HttpGet]
        [HasPermission("SystemLogs", "View")] // التحقق من امتلاك صلاحية عرض السجلات في قاعدة البيانات
        public async Task<IActionResult> SystemLogs()
        {
            // جلب السجلات من قاعدة البيانات مع بيانات المستخدم المرتبط بكل حركة
            // Include(l => l.User): لضمان عدم ظهور اسم المستخدم فارغاً في الجدول
            // OrderByDescending: لعرض أحدث العمليات (الحذف، التعديل، الدخول) في الأعلى
            var logs = await _context.Systemlogs
                .Include(l => l.User)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();

            return View(logs);
        }

        // ==========================================
        // 💾 شاشة النسخ الاحتياطي (GET)
        // ==========================================
        [HttpGet]
        public IActionResult Backup()
        {
            if (!IsSuperAdmin)
                return RedirectToAction("AccessDenied", "Home");

            return View();
        }

        // ==========================================
        // 💾 تنفيذ النسخ الاحتياطي وتنزيل الملف (POST)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Backup(string confirm)
        {
            if (!IsSuperAdmin)
                return RedirectToAction("AccessDenied", "Home");

            // مسار mysqldump - يدعم XAMPP وWAMP والـ PATH العام
            string[] possiblePaths = new[]
            {
                @"C:\xampp\mysql\bin\mysqldump.exe",
                @"C:\wamp64\bin\mysql\mysql8.0.31\bin\mysqldump.exe",
                @"C:\wamp\bin\mysql\mysql5.7.36\bin\mysqldump.exe",
                "mysqldump"  // إذا كان في PATH
            };

            string dumpPath = "mysqldump";
            foreach (var p in possiblePaths)
            {
                if (p == "mysqldump" || System.IO.File.Exists(p))
                {
                    dumpPath = p;
                    break;
                }
            }

            string dbName = "dblast";
            string dbUser = "root";
            string dbHost = "localhost";
            string fileName = $"PharmaSmart_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.sql";
            string tempFile = Path.Combine(Path.GetTempPath(), fileName);

            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = dumpPath,
                        // --no-create-info = بيانات فقط بدون CREATE TABLE
                        Arguments = $"--host={dbHost} --user={dbUser} --no-tablespaces --single-transaction --no-create-info --skip-triggers {dbName}",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();
                process.WaitForExit();

                if (process.ExitCode != 0 || string.IsNullOrWhiteSpace(output))
                {
                    TempData["BackupError"] = $"فشل في إنشاء النسخة الاحتياطية. تأكد من تثبيت MySQL وإمكانية الوصول. التفاصيل: {error}";
                    return RedirectToAction(nameof(Backup));
                }

                // إضافة تعليق توضيحي في رأس الملف
                var header = new StringBuilder();
                header.AppendLine($"-- ====================================================");
                header.AppendLine($"-- PharmaSmart ERP - نسخة احتياطية (بيانات فقط - Data Only)");
                header.AppendLine($"-- تاريخ الإنشاء: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                header.AppendLine($"-- قاعدة البيانات: {dbName}");
                header.AppendLine($"-- ====================================================");
                header.AppendLine();

                byte[] fileBytes = Encoding.UTF8.GetBytes(header + output);

                await RecordLog("Backup", "Admin", $"تم إنشاء نسخة احتياطية (بيانات فقط) من قاعدة البيانات ({dbName}) بنجاح. حجم: {fileBytes.Length / 1024} KB");

                return File(fileBytes, "application/octet-stream", fileName);
            }
            catch (Exception ex)
            {
                TempData["BackupError"] = $"خطأ تقني: {ex.Message}. تأكد من أن mysqldump مثبت على الجهاز.";
                return RedirectToAction(nameof(Backup));
            }
        }

        // ==========================================
        // 🔄 شاشة الاسترجاع (GET)
        // ==========================================
        [HttpGet]
        public IActionResult Restore()
        {
            if (!IsSuperAdmin)
                return RedirectToAction("AccessDenied", "Home");
            return View("Backup"); // نعيد استخدام نفس الشاشة
        }

        // ==========================================
        // 🔄 تنفيذ الاسترجاع من ملف SQL (POST)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(52428800)] // 50 MB max
        public async Task<IActionResult> Restore(Microsoft.AspNetCore.Http.IFormFile sqlFile)
        {
            if (!IsSuperAdmin)
                return RedirectToAction("AccessDenied", "Home");

            if (sqlFile == null || sqlFile.Length == 0)
            {
                TempData["RestoreError"] = "الرجاء اختيار ملف SQL صالح قبل المتابعة.";
                return RedirectToAction(nameof(Backup));
            }

            if (!sqlFile.FileName.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
            {
                TempData["RestoreError"] = "نوع الملف غير صحيح. يُقبل ملفات .sql فقط.";
                return RedirectToAction(nameof(Backup));
            }

            try
            {
                string sqlContent;
                using (var reader = new StreamReader(sqlFile.OpenReadStream(), Encoding.UTF8))
                {
                    sqlContent = await reader.ReadToEndAsync();
                }

                if (string.IsNullOrWhiteSpace(sqlContent))
                {
                    TempData["RestoreError"] = "الملف فارغ أو تالف.";
                    return RedirectToAction(nameof(Backup));
                }

                // تنفيذ SQL مباشرة عبر Entity Framework
                // تقسيم الأوامر على حسب ; للتنفيذ التتابعي
                var statements = sqlContent
                    .Split(new[] { ";\n", ";\r\n" }, StringSplitOptions.RemoveEmptyEntries);

                int executed = 0;
                foreach (var stmt in statements)
                {
                    var trimmed = stmt.Trim();
                    if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("--"))
                        continue;

                    try
                    {
                        _context.Database.ExecuteSqlRaw(trimmed);
                        executed++;
                    }
                    catch { /* تجاهل الأخطاء الفردية وإكمال الباقي */ }
                }

                await RecordLog("Restore", "Admin", $"تم استرجاع البيانات من الملف ({sqlFile.FileName}). عدد الأوامر المنفذة: {executed}");
                TempData["RestoreSuccess"] = $"✅ تم استرجاع البيانات بنجاح! عدد الأوامر المنفذة: {executed}";
                return RedirectToAction(nameof(Backup));
            }
            catch (Exception ex)
            {
                TempData["RestoreError"] = $"خطأ أثناء الاسترجاع: {ex.Message}";
                return RedirectToAction(nameof(Backup));
            }
        }

        // ملاحظة: يمكنك إضافة أكواد إضافية هنا لتنظيف السجلات القديمة أو تصديرها
    }
}

/* =============================================================================================
📑 الكتالوج والدليل الفني للكنترولر (AdminController)
=============================================================================================
الوظيفة العامة: 
هذا الكنترولر يمثل "غرفة المراقبة المركزية" (Control Room) في نظام PharmaSmart ERP. 
مسؤوليته الأساسية هي عرض "سجلات الرقابة" (Audit Trails) لتتبع كل ما يحدث في النظام 
من عمليات (إضافة، تعديل، حذف، تسجيل دخول وخروج) لضمان الشفافية والمساءلة.

ملاحظة معمارية بخصوص العزل (Branch Isolation): 
سجل الرقابة (System Logs) يعتبر من "البيانات السيادية" (Global Data) التي تهم المدير العام 
ومسؤولي النظام لمراقبة أداء كافة الفروع. لذلك، لا يتم تطبيق عزل الفروع `ActiveBranchId` 
هنا كفلتر إجباري، بل يتم الاعتماد على "الختم النصي" الذي تم دمجه آلياً داخل حقل التفاصيل (Details) 
في BaseController لمعرفة الفرع الذي تمت فيه كل عملية.

محتويات الكنترولر والدوال (Methods):

1. [HttpGet] SystemLogs()
   - الوظيفة: جلب جميع حركات النظام من جدول `systemlogs` مرتبة من الأحدث إلى الأقدم.
   - المنطق: يتم استخدام `Include(l => l.User)` لجلب كائن المستخدم المرتبط بكل حركة، 
     مما يسمح للواجهة بعرض اسم الموظف الذي قام بالعملية بدلاً من مجرد عرض معرفه (ID).
   - العزل: الشاشة تعرض كافة الحركات على مستوى النظام بالكامل (System-wide View) 
     نظراً لطبيعتها الرقابية وتصنيفها كشاشة (الإدارة العليا).
=============================================================================================
*/
