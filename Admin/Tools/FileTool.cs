using Microsoft.AspNetCore.Http;
using System;
using System.IO;

namespace EfCoreSetUp1618.Tools
{
    public static class FileTool
    {
        public static string SaveFile(
            IFormFile file,
            string folderName,
            string sharedImagesPath)
        {
            if (file == null || file.Length == 0)
                return null;

            string folderPath = Path.Combine(
                sharedImagesPath,
                folderName
            );

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            string extension = Path.GetExtension(file.FileName);

            string saveName = Guid.NewGuid().ToString() + extension;

            string filePath = Path.Combine(folderPath, saveName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                file.CopyTo(stream);
            }

            return $"/images/{folderName}/{saveName}";
        }
    }
}
