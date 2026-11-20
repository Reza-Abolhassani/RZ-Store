using System;
using System.Security.Cryptography;
using System.Text;

namespace Common.Security
{
    public static class OtpHasher
    {
        public static string Hash(string code, string secretKey)
        {
            if (string.IsNullOrWhiteSpace(secretKey))
                throw new InvalidOperationException("OTP hash key is not configured.");

            using var hmac = new HMACSHA256(
                Convert.FromBase64String(secretKey));

            var hash = hmac.ComputeHash(
                Encoding.UTF8.GetBytes(code));

            return Convert.ToBase64String(hash);
        }
    }
}