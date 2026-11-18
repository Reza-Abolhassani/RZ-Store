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

            // مسیر پوشه تصاویر
            string folderPath = Path.Combine(
                sharedImagesPath,
                folderName
            );

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            // نام جدید تصویر
            string extension = Path.GetExtension(file.FileName);

            string saveName = Guid.NewGuid().ToString() + extension;

            string filePath = Path.Combine(folderPath, saveName);

            // ذخیره تصویر
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                file.CopyTo(stream);
            }

            // آدرس تصویر برای ذخیره در دیتابیس
            return $"/images/{folderName}/{saveName}";
        }
    }
}