using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace LauncherManagement
{
    public class DownloadHandler : FileDownloader
    {
        public static Action OnDownloadCompleted;
        public static Action<string, double, double> OnCurrentFileDownloading;
        public static Action<string, double, double> OnFullScanFileCheck;

        internal static async Task<List<DownloadableFile>> DownloadManifestAsync(string manifestUrl)
        {
            var contents = await Task.Run(() => DownloadAsync(manifestUrl).Result);

            return ManifestJsonHandler.GetFileList(System.Text.Encoding.UTF8.GetString(contents));
        }

        internal static async Task DownloadFilesFromListAsync(Dictionary<string, string> fileList, string downloadLocation)
        {
            double listLength = fileList.Count;

            double i = 1;
            // Key == name, Value == url
            foreach (KeyValuePair<string, string> file in fileList)
            {
                // Notify UI of filename
                OnCurrentFileDownloading?.Invoke(file.Key, i, listLength);

                var contents = await Task.Run(() => DownloadAsync(file.Value));

                // Create directory before writing to file if it doesn't exist
                new FileInfo(Path.Join(downloadLocation, file.Key)).Directory.Create();

                await File.WriteAllBytesAsync(Path.Join(downloadLocation, file.Key), contents);

                ++i;
            }

            OnDownloadCompleted?.Invoke();
        }
    }
}
