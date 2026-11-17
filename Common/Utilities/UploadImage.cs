using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;

using System;
using System.IO;
using System.Threading.Tasks;

namespace Common.Utilities
{
    public static class UploadImage
    {
        public async static Task<string> SaveImage(this IFormFile file, string CategoryName, string domain = "med", string OldUrl = "")
        {
            var path = Path.Combine(Directory.GetCurrentDirectory() + @"\wwwroot" + @$"\{CategoryName}\");
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            if (OldUrl.HasValue())
            {
                if (File.Exists(Directory.GetCurrentDirectory() + OldUrl))
                {
                    File.Delete(Directory.GetCurrentDirectory() + OldUrl);
                }
            }
            string SaveName = Guid.NewGuid().ToString().ToLower() + file.FileName.Trim().ToLower();
            SaveName = SaveName.Trim().ToLower();
            var SavedUrl = $"https://{domain}.goljaam.com/{CategoryName}/{SaveName}";
            var filepath = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", CategoryName)).Root + $@"{SaveName}";

            using (FileStream fs = File.Create(filepath))
            {
                file.CopyTo(fs);
                fs.Flush();
            }

            return SavedUrl;
        }


    }
}
