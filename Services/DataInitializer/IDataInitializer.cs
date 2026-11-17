using Common;

using System.Threading;
using System.Threading.Tasks;

namespace Services.DataInitializer
{
    public interface IDataInitializer : IScopedDependency
    {
        Task InitializeDataAsync(CancellationToken cancellationToken = default);
    }
}
