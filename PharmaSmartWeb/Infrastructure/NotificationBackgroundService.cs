using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PharmaSmartWeb.Services;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PharmaSmartWeb.Infrastructure
{
    /// <summary>
    /// خدمة خلفية تعمل دورياً (كل ساعة) لتشغيل محرك الإشعارات وفحص النواقص والصلاحية تلقائياً.
    /// تضمن وصول تنبيهات الواتساب حتى لو لم يقم أحد بفتح المتصفح.
    /// </summary>
    public class NotificationBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<NotificationBackgroundService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1); // فحص كل ساعة

        public NotificationBackgroundService(IServiceProvider serviceProvider, ILogger<NotificationBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Notification Background Service started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var notificationEngine = scope.ServiceProvider.GetRequiredService<NotificationEngine>();
                        
                        _logger.LogInformation("Running periodic notification check...");
                        
                        // فحص كافة الفروع (ID = 0) أو يمكنك تخصيصها لكل فرع إذا أردت
                        await notificationEngine.GenerateAndSaveNotificationsAsync(0);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred in NotificationBackgroundService.");
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }

            _logger.LogInformation("Notification Background Service stopped.");
        }
    }
}
