using Common.Utilities;

using Microsoft.Extensions.Options;

using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Common.Sms
{
    public interface ISmsSender
    {
        Task<OtpSendResult> SendOtpAsync(string to, CancellationToken cancellationToken = default);
        Task<string> SendSharedAsync(string to, string[] args, int bodyId, CancellationToken cancellationToken = default);
    }

    public class OtpSendResult
    {
        public string Code { get; set; }
        public string Status { get; set; }
    }

    public class MeliPayamakSmsSender : ISmsSender
    {
        private readonly HttpClient httpClient;
        private readonly SmsSettings settings;

        public MeliPayamakSmsSender(HttpClient httpClient, IOptionsSnapshot<SiteSettings> options)
        {
            this.httpClient = httpClient;
            settings = options.Value.SmsSettings ?? new SmsSettings();
        }

        public async Task<OtpSendResult> SendOtpAsync(string to, CancellationToken cancellationToken = default)
        {
            var path = BuildPath(settings.OtpPath);
            var response = await httpClient.PostAsJsonAsync(path, new { to }, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var payload = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return JsonSerializer.Deserialize<OtpSendResult>(payload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        public async Task<string> SendSharedAsync(string to, string[] args, int bodyId, CancellationToken cancellationToken = default)
        {
            var path = BuildPath(settings.SharedPath);
            var response = await httpClient.PostAsJsonAsync(path, new { bodyId, to, args }, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        }

        private string BuildPath(string pathTemplate)
        {
            return (pathTemplate ?? string.Empty).Replace("{token}", settings.ApiToken ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        }
    }

    public static class MeliPayamak
    {
        public static string HashOtp(string code)
        {
            return SecurityHelper.GetSha256Hash(code ?? string.Empty);
        }
    }
}
