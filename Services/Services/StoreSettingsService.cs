using Common;
using Data.Repositories;
using Entities.Setting;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Services
{
    public class StoreSettingsService : IStoreSettingsService, IScopedDependency
    {
        private readonly IRepository<StoreSettings> _repository;

        public StoreSettingsService(
            IRepository<StoreSettings> repository)
        {
            _repository = repository;
        }

        public async Task<StoreSettings> GetSettingsAsync()
        {
            return await _repository.TableNoTracking
                .FirstOrDefaultAsync();
        }

        public async Task UpdateSettingsAsync(StoreSettings model)
        {
            var settings = await _repository.Table
                .FirstOrDefaultAsync();

            if (settings == null)
            {
                await _repository.AddAsync(
                    model,
                    CancellationToken.None);
            }
            else
            {
                settings.SiteName = model.SiteName;
                settings.Logo = model.Logo;
                settings.PhoneNumber = model.PhoneNumber;
                settings.Email = model.Email;
                settings.Address = model.Address;
                settings.Description = model.Description;
                settings.Instagram = model.Instagram;
                settings.Telegram = model.Telegram;

                await _repository.UpdateAsync(
                    settings,
                    CancellationToken.None);
            }
        }
    }
}