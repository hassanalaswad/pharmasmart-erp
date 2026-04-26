using System.Text;
using System.Text.Json;

namespace PharmaSmartWeb.Services
{
    public interface IWhatsAppService
    {
        Task<bool> SendMessageAsync(string mobileNumber, string message);
    }

    public class WhatsAppService : IWhatsAppService
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public WhatsAppService(IConfiguration configuration, HttpClient httpClient)
        {
            _configuration = configuration;
            _httpClient = httpClient;
        }

        /// <summary>
        /// إرسال رسالة واتساب عبر مزود خدمة (مثلاً UltraMsg أو Twilio)
        /// ملاحظة: تم تصميم هذه الدالة لتكون مرنة وقابلة للربط مع أي مزود API
        /// </summary>
        public async Task<bool> SendMessageAsync(string mobileNumber, string message)
        {
            try
            {
                // جلب الإعدادات من appsettings.json
                var instanceId = _configuration["WhatsApp:InstanceId"];
                var token = _configuration["WhatsApp:Token"];
                var apiUrl = _configuration["WhatsApp:ApiUrl"] ?? "https://api.ultramsg.com/";

                if (string.IsNullOrEmpty(instanceId) || string.IsNullOrEmpty(token))
                {
                    // في حالة عدم ضبط الإعدادات، نقوم بتسجيل التحذير فقط (لأغراض العرض التقديمي)
                    Console.WriteLine($"[WhatsApp Simulation] To: {mobileNumber}, Msg: {message}");
                    return true; 
                }

                var payload = new
                {
                    token = token,
                    to = mobileNumber.StartsWith("+") ? mobileNumber : "+" + mobileNumber,
                    body = message
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // رابط الإرسال (مثال لـ UltraMsg)
                var requestUrl = $"{apiUrl.TrimEnd('/')}/{instanceId}/messages/chat";

                var response = await _httpClient.PostAsync(requestUrl, content);
                
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                // تسجيل الخطأ في السجلات
                Console.WriteLine($"WhatsApp Error: {ex.Message}");
                return false;
            }
        }
    }
}
