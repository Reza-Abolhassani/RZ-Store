namespace Common
{
    public class SiteSettings
    {
        public string ElmahPath { get; set; }
        public JwtSettings JwtSettings { get; set; }
        public IdentitySettings IdentitySettings { get; set; }
        public GoogleSettings GoogleSettings { get; set; }
        public CorsSettings CorsSettings { get; set; }
        public DatabaseSettings DatabaseSettings { get; set; }
        public DataSeedSettings DataSeedSettings { get; set; }
        public SmsSettings SmsSettings { get; set; }
    }

    public class CorsSettings
    {
        public string[] AllowedOrigins { get; set; } = System.Array.Empty<string>();
    }

    public class DatabaseSettings
    {
        public bool AutoMigrate { get; set; }
    }

    public class DataSeedSettings
    {
        public bool SeedDefaultRoles { get; set; } = true;
        public bool SeedDemoUsers { get; set; }
        public string AdminUserName { get; set; }
        public string AdminEmail { get; set; }
        public string AdminPassword { get; set; }
        public string ClientUserName { get; set; }
        public string ClientEmail { get; set; }
        public string ClientPassword { get; set; }
    }

    public class SmsSettings
    {
        public string BaseUrl { get; set; } = "https://console.melipayamak.com";
        public string ApiToken { get; set; }
        public string OtpPath { get; set; } = "api/send/otp/{token}";
        public string SharedPath { get; set; } = "api/send/shared/{token}";
    }

    public class IdentitySettings
    {
        public bool PasswordRequireDigit { get; set; }
        public int PasswordRequiredLength { get; set; }
        public bool PasswordRequireNonAlphanumeric { get; set; }
        public bool PasswordRequireUppercase { get; set; }
        public bool PasswordRequireLowercase { get; set; }
        public bool RequireUniqueEmail { get; set; }
    }
    public class GoogleSettings
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
    }
    public class JwtSettings
    {
        public string SecretKey { get; set; }
        public string EncryptKey { get; set; }
        public string Issuer { get; set; }
        public string Audience { get; set; }
        public int NotBeforeMinutes { get; set; }
        public int ExpirationMinutes { get; set; }
        public bool RequireHttpsMetadata { get; set; } = true;
    }
}
