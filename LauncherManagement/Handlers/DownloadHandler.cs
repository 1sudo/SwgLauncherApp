using Newtonsoft.Json;
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
        public static Action<string, string, double, double> OnCurrentFileDownloading;
        public static Action<string, double, double> OnFullScanFileCheck;

        internal static async Task<List<DownloadableFile>> DownloadManifestAsync(string manifestUrl)
        {
            var contents = await Task.Run(() => DownloadAsync(manifestUrl).Result);

            return JsonManifestHandler.GetFileList(System.Text.Encoding.UTF8.GetString(contents));
        }

        internal static async Task DownloadFilesFromListAsync(Dictionary<string, string> fileList, string downloadLocation)
        {
            double listLength = fileList.Count;

            double i = 1;
            // Key == name, Value == url
            foreach (KeyValuePair<string, string> file in fileList)
            {
                // Notify UI of filename
                OnCurrentFileDownloading?.Invoke("download", file.Key, i, listLength);

                var contents = await Task.Run(() => DownloadAsync(file.Value));

                // Create directory before writing to file if it doesn't exist
                new FileInfo(Path.Join(downloadLocation, file.Key)).Directory.Create();

                await File.WriteAllBytesAsync(Path.Join(downloadLocation, file.Key), contents);

                ++i;
            }

            OnDownloadCompleted?.Invoke();
        }

        internal static async Task AttemptCopyFilesFromListAsync(Dictionary<string, string> fileList, string copyLocation)
        {
            double listLength = fileList.Count;
            Dictionary<string, string> newFileList = new Dictionary<string, string>();

            // Get source installation properties
            string configLocation = Path.Join(Directory.GetCurrentDirectory(), "config.json");
            using StreamReader sr = File.OpenText(configLocation);
            ConfigProperties configProperties = JsonConvert.DeserializeObject<ConfigProperties>(sr.ReadToEnd());

            double i = 1;
            // Key == name, Value == url
            foreach (KeyValuePair<string, string> file in fileList)
            {
                // Notify UI of filename
                OnCurrentFileDownloading?.Invoke("copy", file.Key, i, listLength);

                // Create directory before writing to file if it doesn't exist
                new FileInfo(Path.Join(copyLocation, file.Key)).Directory.Create();

                // If file exists at source installation, copy it
                if (File.Exists(Path.Join(configProperties.SWGLocation, file.Key)))
                {
                    await CopyFileAsync(Path.Join(configProperties.SWGLocation, file.Key), Path.Join(copyLocation, file.Key));
                }
                // If file doesn't exist in source location, add to new list to be returned
                else
                {
                    newFileList.Add(file.Key, file.Value);
                }
                
                ++i;
            }
        }

        public static async Task CopyFileAsync(string sourcePath, string destinationPath)
        {
            using (Stream source = File.OpenRead(sourcePath))
            {
                using (Stream destination = File.Create(destinationPath))
                {
                    await source.CopyToAsync(destination);
                }
            }
        }
    }
}
