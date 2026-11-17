using Entities.Setting;
using System.Threading.Tasks;

namespace Services
{
    public interface IStoreSettingsService
    {
        Task<StoreSettings> GetSettingsAsync();

        Task UpdateSettingsAsync(StoreSettings model);
    }
}