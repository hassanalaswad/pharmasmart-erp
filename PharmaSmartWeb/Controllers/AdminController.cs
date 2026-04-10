using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PharmaSmartWeb.Models;
using System.Threading.Tasks;
using PharmaSmartWeb.Filters;
using PharmaSmartWeb.Infrastructure;
using System.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using MySqlConnector;

namespace PharmaSmartWeb.Controllers
{
    /// <summary>
    /// متحكم الإدارة: مسؤول عن عرض سجلات النظام والعمليات الرقابية العليا
    /// </summary>
    [Authorize]
    public class AdminController : BaseController
    {
        private readonly IConfiguration _configuration;

        public AdminController(ApplicationDbContext context, IConfiguration configuration) : base(context)
        {
            _configuration = configuration;
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

            string connectionString;
            try
            {
                connectionString = MySqlConnectionResolver.ResolveConnectionString(_configuration);
            }
            catch (InvalidOperationException ex)
            {
                TempData["BackupError"] = ex.Message;
                return RedirectToAction(nameof(Backup));
            }

            if (!MySqlConnectionResolver.TryGetCliParameters(connectionString, out var dbHost, out var dbPort, out var dbUser, out var dbPassword, out var dbName))
            {
                TempData["BackupError"] = "تعذر قراءة اسم قاعدة البيانات أو المستخدم من سلسلة الاتصال.";
                return RedirectToAction(nameof(Backup));
            }

            string dumpPath = ResolveMySqlToolPath("mysqldump");
            string fileName = $"PharmaSmart_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.sql";
            string cnfPath = Path.Combine(Path.GetTempPath(), $"ps_dump_{Guid.NewGuid():N}.cnf");

            try
            {
                await System.IO.File.WriteAllTextAsync(cnfPath, MySqlConnectionResolver.BuildClientOptionsFileContent(dbHost, dbPort, dbUser, dbPassword), Encoding.UTF8);

                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = dumpPath,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                var psi = process.StartInfo;
                psi.ArgumentList.Add($"--defaults-extra-file={cnfPath}");
                var csb = new MySqlConnectionStringBuilder(connectionString);
                if (csb.SslMode is MySqlSslMode.Required or MySqlSslMode.VerifyCA or MySqlSslMode.VerifyFull)
                    psi.ArgumentList.Add("--ssl-mode=REQUIRED");

                psi.ArgumentList.Add("--no-tablespaces");
                psi.ArgumentList.Add("--single-transaction");
                psi.ArgumentList.Add("--no-create-info");
                psi.ArgumentList.Add("--skip-triggers");
                psi.ArgumentList.Add(dbName);

                process.Start();
                var stdoutTask = process.StandardOutput.ReadToEndAsync();
                var stderrTask = process.StandardError.ReadToEndAsync();
                var exitTask = process.WaitForExitAsync();
                var backupTimeout = TimeSpan.FromMinutes(10);
                var finished = await Task.WhenAny(exitTask, Task.Delay(backupTimeout));
                if (finished != exitTask)
                {
                    try { process.Kill(entireProcessTree: true); } catch { }
                    TempData["BackupError"] = $"انتهت مهلة النسخ الاحتياطي ({(int)backupTimeout.TotalMinutes} دقيقة). إذا كانت قاعدة البيانات كبيرة جداً، نفّذ النسخ يدوياً من الخادم.";
                    return RedirectToAction(nameof(Backup));
                }

                string output = await stdoutTask;
                string error = await stderrTask;

                if (process.ExitCode != 0 || string.IsNullOrWhiteSpace(output))
                {
                    TempData["BackupError"] = $"فشل في إنشاء النسخة الاحتياطية. تأكد من تثبيت MySQL وإمكانية الوصول. التفاصيل: {error}";
                    return RedirectToAction(nameof(Backup));
                }

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
                TempData["BackupError"] = $"خطأ تقني: {ex.Message}. تأكد من أن mysqldump مثبت وفي PATH أو مسار XAMPP/WAMP.";
                return RedirectToAction(nameof(Backup));
            }
            finally
            {
                try { if (System.IO.File.Exists(cnfPath)) System.IO.File.Delete(cnfPath); } catch { }
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

            string connectionString;
            try
            {
                connectionString = MySqlConnectionResolver.ResolveConnectionString(_configuration);
            }
            catch (InvalidOperationException ex)
            {
                TempData["RestoreError"] = ex.Message;
                return RedirectToAction(nameof(Backup));
            }

            if (!MySqlConnectionResolver.TryGetCliParameters(connectionString, out var dbHost, out var dbPort, out var dbUser, out var dbPassword, out var dbName))
            {
                TempData["RestoreError"] = "تعذر قراءة معاملات الاتصال لقاعدة البيانات.";
                return RedirectToAction(nameof(Backup));
            }

            string mysqlPath = ResolveMySqlToolPath("mysql");
            string sqlTempPath = Path.Combine(Path.GetTempPath(), $"ps_restore_{Guid.NewGuid():N}.sql");
            string cnfPath = Path.Combine(Path.GetTempPath(), $"ps_restore_cnf_{Guid.NewGuid():N}.cnf");

            try
            {
                await using (var fs = System.IO.File.Create(sqlTempPath))
                {
                    await sqlFile.CopyToAsync(fs);
                }

                var fileInfo = new FileInfo(sqlTempPath);
                if (fileInfo.Length == 0)
                {
                    TempData["RestoreError"] = "الملف فارغ أو تالف.";
                    return RedirectToAction(nameof(Backup));
                }

                await System.IO.File.WriteAllTextAsync(cnfPath, MySqlConnectionResolver.BuildClientOptionsFileContent(dbHost, dbPort, dbUser, dbPassword), Encoding.UTF8);

                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = mysqlPath,
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                var psi = process.StartInfo;
                psi.ArgumentList.Add($"--defaults-extra-file={cnfPath}");
                var csb = new MySqlConnectionStringBuilder(connectionString);
                if (csb.SslMode is MySqlSslMode.Required or MySqlSslMode.VerifyCA or MySqlSslMode.VerifyFull)
                    psi.ArgumentList.Add("--ssl-mode=REQUIRED");
                psi.ArgumentList.Add("--default-character-set=utf8mb4");
                psi.ArgumentList.Add(dbName);

                process.Start();

                await using (var sqlStream = System.IO.File.OpenRead(sqlTempPath))
                await sqlStream.CopyToAsync(process.StandardInput.BaseStream).ConfigureAwait(false);
                process.StandardInput.Close();

                var stderrTask = process.StandardError.ReadToEndAsync();
                _ = process.StandardOutput.ReadToEndAsync();

                var exitTask = process.WaitForExitAsync();
                var restoreTimeout = TimeSpan.FromMinutes(30);
                if (await Task.WhenAny(exitTask, Task.Delay(restoreTimeout)).ConfigureAwait(false) != exitTask)
                {
                    try { process.Kill(entireProcessTree: true); } catch { }
                    TempData["RestoreError"] = $"انتهت مهلة الاستعادة ({(int)restoreTimeout.TotalMinutes} دقيقة). جرّب تقسيم الملف أو الاستعادة من سطر الأوامر.";
                    return RedirectToAction(nameof(Backup));
                }

                string err = await stderrTask.ConfigureAwait(false);
                if (process.ExitCode != 0)
                {
                    TempData["RestoreError"] = string.IsNullOrWhiteSpace(err)
                        ? "فشل تنفيذ mysql (رمز خروج غير صفري). تحقق من صلاحيات المستخدم وصحة الملف."
                        : $"فشل الاستعادة: {err}";
                    return RedirectToAction(nameof(Backup));
                }

                await RecordLog("Restore", "Admin", $"تم استرجاع البيانات عبر mysql CLI من الملف ({sqlFile.FileName}).");
                TempData["RestoreSuccess"] = "✅ تم استرجاع البيانات بنجاح عبر عميل MySQL الرسمي.";
                return RedirectToAction(nameof(Backup));
            }
            catch (Exception ex)
            {
                TempData["RestoreError"] = $"خطأ أثناء الاسترجاع: {ex.Message}";
                return RedirectToAction(nameof(Backup));
            }
            finally
            {
                try { if (System.IO.File.Exists(sqlTempPath)) System.IO.File.Delete(sqlTempPath); } catch { }
                try { if (System.IO.File.Exists(cnfPath)) System.IO.File.Delete(cnfPath); } catch { }
            }
        }

        /// <summary>مسارات شائعة لـ XAMPP/WAMP ثم الاعتماد على PATH.</summary>
        private static string ResolveMySqlToolPath(string toolName)
        {
            bool isDump = toolName.Equals("mysqldump", StringComparison.OrdinalIgnoreCase);
            string[] candidates = isDump
                ? new[]
                {
                    @"C:\xampp\mysql\bin\mysqldump.exe",
                    @"C:\wamp64\bin\mysql\mysql8.0.31\bin\mysqldump.exe",
                    @"C:\wamp\bin\mysql\mysql5.7.36\bin\mysqldump.exe",
                    "mysqldump"
                }
                : new[]
                {
                    @"C:\xampp\mysql\bin\mysql.exe",
                    @"C:\wamp64\bin\mysql\mysql8.0.31\bin\mysql.exe",
                    @"C:\wamp\bin\mysql\mysql5.7.36\bin\mysql.exe",
                    "mysql"
                };

            foreach (var p in candidates)
            {
                if (p == "mysqldump" || p == "mysql")
                    return p;
                if (System.IO.File.Exists(p))
                    return p;
            }

            return isDump ? "mysqldump" : "mysql";
        }
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
