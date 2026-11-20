using Common;
using Services.Models;

using System.Threading;
using System.Threading.Tasks;

namespace Services
{
    public interface IOtpService : IScopedDependency
    {
        Task<OtpResult> SendOtpAsync(
            string phoneNumber,
            string requestIp,
            CancellationToken cancellationToken = default);

        Task<OtpResult> VerifyOtpAsync(
            string phoneNumber,
            string code,
            CancellationToken cancellationToken = default);
    }
}