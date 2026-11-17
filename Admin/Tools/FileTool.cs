//using Microsoft.AspNetCore.Http;
//using Microsoft.Extensions.FileProviders;

//using System;
//using System.IO;

//namespace EfCoreSetUp1618.Tools
//{
//    public static class FileTool
//    {
//        public static string SaveFile(IFormFile file, string FolderName)
//        {
//            var path = Path.Combine("C:\\Users\\Tick\\Desktop\\Rezaproject\\R&Z\\BaseProjectFinally\\BaseProject-NET10" + @"\wwwroot" + @$"\{FolderName}\");

//            if (!Directory.Exists(path))
//            {
//                Directory.CreateDirectory(path);
//            }


//            string SaveName = Guid.NewGuid().ToString() + file.FileName.Trim().ToLower();

//            var Link = $"https://localhost:44380/{FolderName}/{SaveName}";


//            var filepath = new PhysicalFileProvider(Path.Combine("C:\\Users\\Tick\\Desktop\\Rezaproject\\R&Z\\BaseProjectFinally\\BaseProject-NET10", "wwwroot", FolderName)).Root + $@"\{SaveName}";
//            using (FileStream fs = File.Create(filepath))
//            {
//                file.CopyTo(fs);
//                fs.Flush();
//            }

//            return Link;
//        }

//    }
//}


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
            string webRootPath)
        {
            if (file == null || file.Length == 0)
                return null;

            string folderPath = Path.Combine(
                webRootPath,
                "images",
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
