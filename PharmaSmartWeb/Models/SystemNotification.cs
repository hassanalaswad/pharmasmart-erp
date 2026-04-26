using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PharmaSmartWeb.Models
{
    /// <summary>
    /// إشعارات النظام المخزّنة في قاعدة البيانات.
    /// تُنشأ تلقائياً بواسطة NotificationEngine عند اكتشاف حدث.
    /// تُحذف فوراً بعد قراءتها من مركز الإشعارات.
    /// </summary>
    [Table("systemnotifications")]
    public class SystemNotification
    {
        [Key]
        public int Id { get; set; }

        /// <summary>نوع الإشعار: inventory | admin | expiry | shortage</summary>
        [StringLength(50)]
        public string Category { get; set; } = "inventory";

        /// <summary>مستوى الخطورة: critical | warning | info</summary>
        [StringLength(20)]
        public string Severity { get; set; } = "info";

        /// <summary>عنوان الإشعار</summary>
        [StringLength(300)]
        public string Title { get; set; } = string.Empty;

        /// <summary>نص الإشعار التفصيلي</summary>
        [StringLength(1000)]
        public string Body { get; set; } = string.Empty;

        /// <summary>أيقونة Material Symbols</summary>
        [StringLength(100)]
        public string Icon { get; set; } = "notifications";

        [StringLength(100)]
        public string IconColor { get; set; } = "text-blue-600";

        [StringLength(200)]
        public string BgColor { get; set; } = "bg-blue-50 border-blue-200";

        [StringLength(100)]
        public string BadgeColor { get; set; } = "bg-blue-500";

        /// <summary>رابط الإجراء المقترح</summary>
        [StringLength(300)]
        public string ActionUrl { get; set; } = "#";

        [StringLength(100)]
        public string ActionText { get; set; } = "—";

        /// <summary>هل تمت قراءته؟</summary>
        public bool IsRead { get; set; } = false;

        /// <summary>تاريخ الإنشاء</summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>فرع الإشعار (0 = عام)</summary>
        public int BranchId { get; set; } = 0;
    }
}
