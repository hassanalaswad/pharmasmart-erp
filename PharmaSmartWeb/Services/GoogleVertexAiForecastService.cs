using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace PharmaSmartWeb.Services
{
    /// <summary>
    /// خدمة التنبؤ بالطلب باستخدام Google Vertex AI Forecasting
    /// تُستخدم عند توفر بيانات مبيعات خارجية (Excel)
    /// </summary>
    public class GoogleVertexAiForecastService : IForecastApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly ILogger<GoogleVertexAiForecastService> _logger;

        // قراءة الإعدادات من appsettings
        private string ProjectId     => _config["ForecastApi:GoogleVertexAI:ProjectId"]     ?? "";
        private string Location      => _config["ForecastApi:GoogleVertexAI:Location"]      ?? "us-central1";
        private string EndpointId    => _config["ForecastApi:GoogleVertexAI:EndpointId"]    ?? "";
        private string ApiKey        => _config["ForecastApi:GoogleVertexAI:ApiKey"]        ?? "";

        public GoogleVertexAiForecastService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<GoogleVertexAiForecastService> logger)
        {
            _httpClient = httpClient;
            _config     = configuration;
            _logger     = logger;
        }

        public async Task<ForecastResult> GetForecastAsync(
            string drugName,
            List<SalesDataPoint> salesHistory,
            CancellationToken ct = default)
        {
            // ─── التحقق من الإعدادات ─────────────────────────────────────
            if (string.IsNullOrWhiteSpace(ApiKey)     ||
                string.IsNullOrWhiteSpace(ProjectId)  ||
                string.IsNullOrWhiteSpace(EndpointId) ||
                salesHistory.Count < 2)
            {
                _logger.LogWarning("Google Vertex AI: إعدادات غير مكتملة أو بيانات غير كافية — تراجع للمتوسط.");
                return FallbackToAverage(salesHistory);
            }

            try
            {
                // ─── بناء الـ instances للـ Predict endpoint ─────────────────
                // تنسيق البيانات: time_series_identifier + timestamp + target
                var instances = new List<object>();
                foreach (var point in salesHistory)
                {
                    instances.Add(new
                    {
                        drug_name = drugName,
                        date      = point.Date,
                        quantity  = point.Quantity.ToString("F2")
                    });
                }

                var requestBody = new
                {
                    instances  = instances,
                    parameters = new
                    {
                        confidence_level       = 0.9,
                        forecast_horizon       = 30   // التنبؤ لـ 30 يوم قادم
                    }
                };

                string json = JsonSerializer.Serialize(requestBody);

                // ─── بناء URL ─────────────────────────────────────────────────
                string url = $"https://{Location}-aiplatform.googleapis.com/v1/" +
                             $"projects/{ProjectId}/locations/{Location}/" +
                             $"endpoints/{EndpointId}:predict?key={ApiKey}";

                using var content = new StringContent(json, Encoding.UTF8, "application/json");
                using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // ─── إرسال الطلب (مع timeout 15 ثانية) ─────────────────────
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(TimeSpan.FromSeconds(15));

                HttpResponseMessage response = await _httpClient.SendAsync(request, cts.Token);

                if (!response.IsSuccessStatusCode)
                {
                    string err = await response.Content.ReadAsStringAsync(ct);
                    _logger.LogWarning("Google Vertex AI أعاد خطأ {Code}: {Msg}", response.StatusCode, err);
                    return FallbackToAverage(salesHistory);
                }

                // ─── تحليل الاستجابة ─────────────────────────────────────────
                string responseJson = await response.Content.ReadAsStringAsync(ct);
                decimal forecast    = ParseForecastFromResponse(responseJson);
                decimal accuracy    = ParseAccuracyFromResponse(responseJson);

                _logger.LogInformation("Google Vertex AI نجح — دواء: {Drug}, توقع: {Forecast}", drugName, forecast);
                return new ForecastResult(forecast, accuracy, "Google Vertex AI");
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Google Vertex AI: انتهت مهلة الاتصال ({Drug})", drugName);
                return FallbackToAverage(salesHistory);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Google Vertex AI: خطأ غير متوقع ({Drug})", drugName);
                return FallbackToAverage(salesHistory);
            }
        }

        // ─── تحليل الاستجابة ─────────────────────────────────────────────────
        // بنية Vertex AI predictions يعتمد على schema النموذج المدرَّب
        // هنا نتعامل مع الأنماط الشائعة
        private static decimal ParseForecastFromResponse(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);

                // نمط 1: { "predictions": [{ "value": 123.4 }] }
                if (doc.RootElement.TryGetProperty("predictions", out var preds) &&
                    preds.ValueKind == JsonValueKind.Array)
                {
                    foreach (var pred in preds.EnumerateArray())
                    {
                        // نمط: { "value": number }
                        if (pred.TryGetProperty("value", out var val) && val.TryGetDecimal(out decimal d))
                            return Math.Max(0, d);

                        // نمط: { "forecast_periods": [{ "point_forecast": number }] }  
                        if (pred.TryGetProperty("forecast_periods", out var periods) &&
                            periods.ValueKind == JsonValueKind.Array)
                        {
                            decimal total = 0;
                            int count = 0;
                            foreach (var period in periods.EnumerateArray())
                            {
                                if (period.TryGetProperty("point_forecast", out var pf) && pf.TryGetDecimal(out decimal pfd))
                                { total += pfd; count++; }
                            }
                            if (count > 0) return Math.Max(0, total / count);
                        }

                        // نمط: رقم مباشر ضمن predictions
                        if (pred.ValueKind == JsonValueKind.Number && pred.TryGetDecimal(out decimal direct))
                            return Math.Max(0, direct);
                    }
                }

                // نمط 2: { "forecast": 123.4 }
                if (doc.RootElement.TryGetProperty("forecast", out var fEl) && fEl.TryGetDecimal(out decimal f))
                    return Math.Max(0, f);
            }
            catch { }

            return 0;
        }

        private static decimal ParseAccuracyFromResponse(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("predictions", out var preds) &&
                    preds.ValueKind == JsonValueKind.Array)
                {
                    foreach (var pred in preds.EnumerateArray())
                    {
                        if (pred.TryGetProperty("accuracy", out var acc)    && acc.TryGetDecimal(out decimal a)) return a;
                        if (pred.TryGetProperty("mape", out var mape)       && mape.TryGetDecimal(out decimal m)) return Math.Round(100 - m, 2);
                        if (pred.TryGetProperty("confidence", out var conf) && conf.TryGetDecimal(out decimal c)) return c;
                    }
                }
            }
            catch { }

            return 0; // 0 = غير متاح
        }

        // ─── Fallback: متوسط المبيعات التاريخية ─────────────────────────────
        private static ForecastResult FallbackToAverage(List<SalesDataPoint> history)
        {
            if (history.Count == 0)
                return new ForecastResult(0, 0, "Average (No Data)");

            decimal total = 0;
            foreach (var p in history) total += p.Quantity;
            decimal avg = Math.Round(total / history.Count, 0);

            return new ForecastResult(avg, 0, "Average (Fallback)");
        }
    }
}
