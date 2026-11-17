using Data.Repositories;
using Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Services.DataInitializer
{
    public class ProvinceDataInitializer : IDataInitializer
    {
        private readonly IRepository<Province> provinceRepo;
        private readonly IRepository<City> cityRepo;

        public ProvinceDataInitializer(
            IRepository<Province> provinceRepo,
            IRepository<City> cityRepo)
        {
            this.provinceRepo = provinceRepo;
            this.cityRepo = cityRepo;
        }

        public async Task InitializeDataAsync(
            CancellationToken cancellationToken = default)
        {
            // مرحله اول: ثبت استان‌ها در صورت خالی بودن جدول

            if (!await provinceRepo.TableNoTracking.AnyAsync(cancellationToken))
            {
                var provinces = new List<Province>
                {
                    new Province { Id = Guid.NewGuid(), Name = "آذربایجان شرقی" },
                    new Province { Id = Guid.NewGuid(), Name = "آذربایجان غربی" },
                    new Province { Id = Guid.NewGuid(), Name = "اردبیل" },
                    new Province { Id = Guid.NewGuid(), Name = "اصفهان" },
                    new Province { Id = Guid.NewGuid(), Name = "البرز" },
                    new Province { Id = Guid.NewGuid(), Name = "ایلام" },
                    new Province { Id = Guid.NewGuid(), Name = "بوشهر" },
                    new Province { Id = Guid.NewGuid(), Name = "تهران" },
                    new Province { Id = Guid.NewGuid(), Name = "چهارمحال و بختیاری" },
                    new Province { Id = Guid.NewGuid(), Name = "خراسان جنوبی" },
                    new Province { Id = Guid.NewGuid(), Name = "خراسان رضوی" },
                    new Province { Id = Guid.NewGuid(), Name = "خراسان شمالی" },
                    new Province { Id = Guid.NewGuid(), Name = "خوزستان" },
                    new Province { Id = Guid.NewGuid(), Name = "زنجان" },
                    new Province { Id = Guid.NewGuid(), Name = "سمنان" },
                    new Province { Id = Guid.NewGuid(), Name = "سیستان و بلوچستان" },
                    new Province { Id = Guid.NewGuid(), Name = "فارس" },
                    new Province { Id = Guid.NewGuid(), Name = "قزوین" },
                    new Province { Id = Guid.NewGuid(), Name = "قم" },
                    new Province { Id = Guid.NewGuid(), Name = "کردستان" },
                    new Province { Id = Guid.NewGuid(), Name = "کرمان" },
                    new Province { Id = Guid.NewGuid(), Name = "کرمانشاه" },
                    new Province { Id = Guid.NewGuid(), Name = "کهگیلویه و بویراحمد" },
                    new Province { Id = Guid.NewGuid(), Name = "گلستان" },
                    new Province { Id = Guid.NewGuid(), Name = "گیلان" },
                    new Province { Id = Guid.NewGuid(), Name = "لرستان" },
                    new Province { Id = Guid.NewGuid(), Name = "مازندران" },
                    new Province { Id = Guid.NewGuid(), Name = "مرکزی" },
                    new Province { Id = Guid.NewGuid(), Name = "هرمزگان" },
                    new Province { Id = Guid.NewGuid(), Name = "همدان" },
                    new Province { Id = Guid.NewGuid(), Name = "یزد" }
                };

                await provinceRepo.AddRangeAsync(
                    provinces,
                    cancellationToken);
            }

            // مرحله دوم: خواندن فایل‌های JSON

            var dataFolder = Path.Combine(
                AppContext.BaseDirectory,
                "DataInitializer",
                "Data");

            var provincePath = Path.Combine(
                dataFolder,
                "provinces.json");

            var cityPath = Path.Combine(
                dataFolder,
                "cities-filtered.json");

            if (!File.Exists(provincePath) || !File.Exists(cityPath))
            {
                throw new FileNotFoundException(
                    "فایل‌های provinces.json یا cities-filtered.json پیدا نشدند.");
            }

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var provinceJson = await File.ReadAllTextAsync(
                provincePath,
                cancellationToken);

            var cityJson = await File.ReadAllTextAsync(
                cityPath,
                cancellationToken);

            var sourceProvinces =
                JsonSerializer.Deserialize<List<ProvinceJson>>(
                    provinceJson, jsonOptions);

            var sourceCities =
                JsonSerializer.Deserialize<List<CityJson>>(
                    cityJson, jsonOptions);

            if (sourceProvinces == null || sourceCities == null)
            {
                throw new InvalidOperationException(
                    "اطلاعات فایل‌های JSON قابل خواندن نیستند.");
            }

            // مرحله سوم: دریافت استان‌های موجود در دیتابیس

            var databaseProvinces =
                await provinceRepo.TableNoTracking.ToListAsync(
                    cancellationToken);

            var provinceIdByName = databaseProvinces
                .ToDictionary(
                    p => p.Name.Trim(),
                    p => p.Id);

            // اتصال شناسه استان‌های فایل JSON به شناسه دیتابیس

            var provinceMapping = new Dictionary<int, Guid>();

            foreach (var province in sourceProvinces)
            {
                if (!provinceIdByName.TryGetValue(
                    province.Name.Trim(), out var databaseId))
                {
                    throw new InvalidOperationException(
                        $"استان {province.Name} در دیتابیس پیدا نشد.");
                }

                provinceMapping[province.Id] = databaseId;
            }

            // مرحله چهارم: جلوگیری از ثبت شهرهای تکراری

            var existingCities =
                await cityRepo.TableNoTracking.ToListAsync(
                    cancellationToken);

            var existingCityKeys = new HashSet<string>(
                existingCities.Select(
                    c => $"{c.ProvinceId}_{c.Name.Trim()}"));

            var newCities = new List<City>();

            foreach (var city in sourceCities)
            {
                if (!provinceMapping.TryGetValue(
                    city.ProvinceId, out var databaseProvinceId))
                {
                    continue;
                }

                var cityKey =
                    $"{databaseProvinceId}_{city.Name.Trim()}";

                if (existingCityKeys.Add(cityKey))
                {
                    newCities.Add(new City
                    {
                        Id = Guid.NewGuid(),
                        Name = city.Name.Trim(),
                        ProvinceId = databaseProvinceId
                    });
                }
            }

            // مرحله پنجم: ثبت شهرها در دیتابیس

            if (newCities.Count > 0)
            {
                await cityRepo.AddRangeAsync(
                    newCities,
                    cancellationToken);
            }
        }

        // مدل اطلاعات استان در فایل JSON

        private class ProvinceJson
        {
            [JsonPropertyName("id")]
            public int Id { get; set; }

            [JsonPropertyName("name")]
            public string Name { get; set; }
        }

        // مدل اطلاعات شهر در فایل JSON

        private class CityJson
        {
            [JsonPropertyName("id")]
            public long Id { get; set; }

            [JsonPropertyName("name")]
            public string Name { get; set; }

            [JsonPropertyName("province_id")]
            public int ProvinceId { get; set; }
        }
    }
}