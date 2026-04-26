using Microsoft.Extensions.Configuration;
using MySqlConnector;

namespace PharmaSmartWeb.Infrastructure
{
    /// <summary>
    /// يوحّد قراءة سلسلة الاتصال (DATABASE_URL أو appsettings) ويستخرج معاملات عميل MySQL CLI.
    /// </summary>
    public static class MySqlConnectionResolver
    {
        public static string ResolveConnectionString(IConfiguration configuration)
        {
            var fromEnv = Environment.GetEnvironmentVariable("DATABASE_URL");
            var fromConfig = configuration.GetConnectionString("DefaultConnection");
            string? connectionString = null;
            if (!string.IsNullOrWhiteSpace(fromEnv))
                connectionString = fromEnv.Trim();
            else if (!string.IsNullOrWhiteSpace(fromConfig))
                connectionString = fromConfig.Trim();

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException(
                    "لم يتم تهيئة الاتصال بقاعدة البيانات. عيّن المتغير البيئي DATABASE_URL أو ConnectionStrings:DefaultConnection في appsettings.json (أو appsettings.Development.json للتطوير).");
            }

            if (connectionString.StartsWith("mysql://", StringComparison.OrdinalIgnoreCase))
            {
                var uri = new Uri(connectionString);
                var userInfo = uri.UserInfo.Split(':');
                string user = Uri.UnescapeDataString(userInfo[0]);
                string pass = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";
                string dbName = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/'));
                int port = uri.Port > 0 ? uri.Port : 3306;
                connectionString = $"Server={uri.Host};Port={port};Database={dbName};Uid={user};Pwd={pass};SslMode=Required;CharSet=utf8mb4;";
            }

            return connectionString;
        }

        public static bool TryGetCliParameters(string connectionString, out string host, out int port, out string user, out string password, out string database)
        {
            host = "localhost";
            port = 3306;
            user = "";
            password = "";
            database = "";
            try
            {
                var builder = new MySqlConnectionStringBuilder(connectionString);
                host = string.IsNullOrWhiteSpace(builder.Server) ? "localhost" : builder.Server;
                port = builder.Port > 0 ? (int)builder.Port : 3306;
                user = builder.UserID ?? "";
                password = builder.Password ?? "";
                database = builder.Database ?? "";
                return !string.IsNullOrWhiteSpace(database) && !string.IsNullOrWhiteSpace(user);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// ملف إعدادات عميل MySQL آمن نسبياً (بدون كلمة المرور في سطر الأوامر).
        /// يجب حذف الملف بعد الاستخدام.
        /// </summary>
        public static string BuildClientOptionsFileContent(string host, int port, string user, string password)
        {
            static string IniEscape(string? value)
            {
                if (string.IsNullOrEmpty(value)) return "\"\"";
                var s = value.Replace("\\", "\\\\").Replace("\"", "\\\"");
                return $"\"{s}\"";
            }

            return
                "[client]\n" +
                $"host={IniEscape(host)}\n" +
                $"port={port}\n" +
                $"user={IniEscape(user)}\n" +
                $"password={IniEscape(password)}\n";
        }
    }
}
