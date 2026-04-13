using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PharmaSmartWeb.Services
{
    /// <summary>
    /// نقطة بيانات مبيعات واحدة (تاريخ + كمية)
    /// </summary>
    public record SalesDataPoint(string Date, decimal Quantity);

    /// <summary>
    /// نتيجة التنبؤ بالطلب
    /// </summary>
    public record ForecastResult(
        decimal ForecastedDemand,
        decimal Accuracy,
        string Source // "GoogleVertexAI" | "Prophet" | "Average"
    );

    /// <summary>
    /// واجهة موحدة للتنبؤ بالطلب — تدعم Google Vertex AI أو Prophet محلياً
    /// </summary>
    public interface IForecastApiService
    {
        /// <summary>
        /// يُرجع الطلب المتوقع لشهر قادم ودقة النموذج لدواء بعينه
        /// </summary>
        /// <param name="drugName">اسم الدواء</param>
        /// <param name="salesHistory">تاريخ المبيعات اليومي (تاريخ + كمية)</param>
        /// <param name="ct">رمز الإلغاء</param>
        Task<ForecastResult> GetForecastAsync(
            string drugName,
            List<SalesDataPoint> salesHistory,
            CancellationToken ct = default);
    }
}
