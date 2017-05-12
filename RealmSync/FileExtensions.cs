using System;
using System.IO;
using System.Threading.Tasks;
using PCLStorage;

namespace PCLStorage
{
    public class FileExtensions2
    {
        public static async Task<string> CopyFileToUserLocation(string path)
        {
            var sourceFile = await PCLStorage.FileSystem.Current.GetFileFromPathAsync(path);
            var newFileName = Guid.NewGuid() + Path.GetExtension(path);
            var newFile = await PCLStorage.FileSystem.Current.LocalStorage.CreateFileAsync(newFileName,
                CreationCollisionOption.ReplaceExisting);

            using (var writeStream = await newFile.OpenAsync(FileAccess.ReadAndWrite))
            {
                using (var sourceStream = await sourceFile.OpenAsync(FileAccess.Read))
                {
                    sourceStream.CopyTo(writeStream);
                    writeStream.Flush();
                }
            }

            return newFile.Path;
        }
    }
}