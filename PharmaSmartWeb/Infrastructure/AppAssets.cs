// ============================================================
//  🎨 AppAssets — السجل المركزي لجميع الأصول (CSS / JS / Fonts)
//  PharmaSmart ERP — Infrastructure Layer
//
//  🎯 الهدف:
//    - نقطة تحكم واحدة في جميع روابط المكتبات والخطوط.
//    - للتبديل من CDN إلى محلي: غيّر القيمة هنا فقط دون لمس أي View.
//    - جميع المسارات المحلية تبدأ من مجلد wwwroot.
//
//  📖 طريقة الاستخدام في Layout:
//    <link rel="stylesheet" href="@AppAssets.Fonts.Cairo">
//    <script src="@AppAssets.JS.ChartJs"></script>
// ============================================================

namespace PharmaSmartWeb.Infrastructure
{
    /// <summary>
    /// الثوابت المركزية لجميع روابط الأصول (CSS و JS و الخطوط).
    /// عدّل هذا الملف فقط للتبديل بين CDN والمسارات المحلية.
    /// </summary>
    public static class AppAssets
    {
        // ┌──────────────────────────────────────────────────────────────┐
        // │  🔤 الخطوط (Fonts) — محلية بالكامل                          │
        // │  Cairo + Manrope + Material Symbols موجودة في               │
        // │  wwwroot/css/local-fonts.css                                │
        // └──────────────────────────────────────────────────────────────┘
        public static class Fonts
        {
            /// <summary>
            /// ملف الخطوط المحلي — يشمل Cairo وManrope وMaterial Symbols.
            /// لا يحتاج إنترنت — الملفات في wwwroot/css/fonts/
            /// </summary>
            public const string Cairo = "/css/local-fonts.css";
        }

        // ┌──────────────────────────────────────────────────────────────┐
        // │  🎨 ملفات CSS                                                │
        // └──────────────────────────────────────────────────────────────┘
        public static class CSS
        {
            /// <summary>
            /// TailwindCSS — محلي في wwwroot/lib/tailwindcss.js
            /// </summary>
            public const string Tailwind = "/lib/tailwindcss.js";

            /// <summary>FontAwesome 6 — موجود محلياً في wwwroot/plugins/fontawesome-free</summary>
            public const string FontAwesome = "/plugins/fontawesome-free/css/all.min.css";

            /// <summary>Chart.js CSS — لا يحتاج CSS منفصل (مدمج في JS)</summary>
            // لا يوجد CSS لـ Chart.js

            /// <summary>SweetAlert2 — موجود محلياً</summary>
            public const string SweetAlert2 = "/plugins/sweetalert2/sweetalert2.min.css";

            /// <summary>Toastr للإشعارات — موجود محلياً</summary>
            public const string Toastr = "/plugins/toastr/toastr.min.css";

            /// <summary>Select2 لقوائم البحث — موجود محلياً</summary>
            public const string Select2 = "/plugins/select2/css/select2.min.css";

            /// <summary>DataTables لجداول البيانات المتقدمة — موجود محلياً</summary>
            public const string DataTables = "/plugins/datatables-bs4/css/dataTables.bootstrap4.min.css";

            /// <summary>ملف CSS المخصص للتطبيق</summary>
            public const string AppCustom = "/css/site.css";
        }

        // ┌──────────────────────────────────────────────────────────────┐
        // │  🔧 ملفات JavaScript                                         │
        // └──────────────────────────────────────────────────────────────┘
        public static class JS
        {
            /// <summary>jQuery — موجود محلياً في wwwroot/plugins/jquery</summary>
            public const string jQuery = "/plugins/jquery/jquery.min.js";

            /// <summary>
            /// Chart.js للرسوم البيانية — موجود محلياً في wwwroot/plugins/chart.js
            /// </summary>
            public const string ChartJs = "/plugins/chart.js/Chart.min.js";

            /// <summary>SweetAlert2 — موجود محلياً</summary>
            public const string SweetAlert2 = "/plugins/sweetalert2/sweetalert2.all.min.js";

            /// <summary>Toastr للإشعارات البوب — موجود محلياً</summary>
            public const string Toastr = "/plugins/toastr/toastr.min.js";

            /// <summary>Select2 لقوائم البحث مع التصفية — موجود محلياً</summary>
            public const string Select2 = "/plugins/select2/js/select2.min.js";

            /// <summary>DataTables JS — موجود محلياً</summary>
            public const string DataTables = "/plugins/datatables/jquery.dataTables.min.js";

            /// <summary>Bootstrap Bundle (يشمل Popper) — موجود محلياً</summary>
            public const string Bootstrap = "/plugins/bootstrap/js/bootstrap.bundle.min.js";

            /// <summary>InputMask لتنسيق الحقول — موجود محلياً</summary>
            public const string InputMask = "/plugins/inputmask/jquery.inputmask.min.js";

            /// <summary>Moment.js لمعالجة التواريخ — موجود محلياً</summary>
            public const string MomentJs = "/plugins/moment/moment.min.js";

            /// <summary>ملف JavaScript المخصص للتطبيق</summary>
            public const string AppCustom = "/js/site.js";
        }

        // ┌──────────────────────────────────────────────────────────────┐
        // │  🖼️ الصور والأيقونات                                         │
        // └──────────────────────────────────────────────────────────────┘
        public static class Images
        {
            /// <summary>شعار التطبيق الافتراضي</summary>
            public const string Logo    = "/dist/img/AdminLTELogo.png";
            /// <summary>صورة المستخدم الافتراضية</summary>
            public const string Avatar  = "/dist/img/avatar.png";
            /// <summary>أيقونة Favicon</summary>
            public const string Favicon = "/favicon.ico";
        }

        // ┌──────────────────────────────────────────────────────────────┐
        // │  🗂️ Bundles جاهزة (مجموعات للاستخدام السريع)               │
        // └──────────────────────────────────────────────────────────────┘

        /// <summary>
        /// الـ Bundle الأساسي المطلوب في كل صفحة (Layout).
        /// يحتوي على المكتبات الضرورية بالترتيب الصحيح.
        /// </summary>
        public static class CoreBundle
        {
            public static readonly string[] Styles = new[]
            {
                Fonts.Cairo,
                CSS.FontAwesome,
                CSS.Tailwind,
                CSS.AppCustom
            };

            public static readonly string[] Scripts = new[]
            {
                JS.jQuery,
                JS.Bootstrap,
                JS.AppCustom
            };
        }

        /// <summary>
        /// Bundle الصفحات التي تحتوي على جداول بيانات (Index pages)
        /// </summary>
        public static class TableBundle
        {
            public static readonly string[] Styles  = new[] { CSS.DataTables };
            public static readonly string[] Scripts = new[] { JS.DataTables };
        }

        /// <summary>
        /// Bundle الصفحات التي تحتوي على رسوم بيانية (Dashboard, Reports)
        /// </summary>
        public static class ChartBundle
        {
            public static readonly string[] Scripts = new[] { JS.MomentJs, JS.ChartJs };
        }

        /// <summary>
        /// Bundle نماذج الإدخال المتقدمة (Create, Edit forms)
        /// </summary>
        public static class FormBundle
        {
            public static readonly string[] Styles  = new[] { CSS.Select2 };
            public static readonly string[] Scripts = new[] { JS.Select2, JS.InputMask };
        }

        /// <summary>
        /// Bundle الإشعارات والتنبيهات
        /// </summary>
        public static class AlertBundle
        {
            public static readonly string[] Styles  = new[] { CSS.SweetAlert2, CSS.Toastr };
            public static readonly string[] Scripts = new[] { JS.SweetAlert2, JS.Toastr };
        }
    }
}
