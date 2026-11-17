using System.Security.Cryptography;

namespace Common.Utilities
{
    public static class OtpGenerator
    {
        public static string GenerateRandomToken(int length)
        {
            const string chars = "0123456789";
            var token = new char[length];

            for (var i = 0; i < token.Length; i++)
            {
                token[i] = chars[RandomNumberGenerator.GetInt32(chars.Length)];
            }

            return new string(token);
        }

      
    }
}
