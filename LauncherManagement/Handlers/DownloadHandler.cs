using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace LauncherManagement
{
    public class DownloadHandler : FileDownloader
    {
        public static Action OnDownloadCompleted;
        public static Action<string, string, double, double> OnCurrentFileDownloading;
        public static Action<string, double, double> OnFullScanFileCheck;

        internal static async Task<List<DownloadableFile>> DownloadManifestAsync(string manifestFile)
        {
            var contents = await Task.Run(() => DownloadAsync(manifestFile).Result);

            return JsonManifestHandler.GetFileList(System.Text.Encoding.UTF8.GetString(contents));
        }

        internal static async Task DownloadFilesFromListAsync(List<string> fileList, string downloadLocation)
        {
            double listLength = fileList.Count;

            double i = 1;
            // Key == name, Value == url
            foreach (string file in fileList)
            {
                // Notify UI of filename
                OnCurrentFileDownloading?.Invoke("download", file, i, listLength);

                var contents = await Task.Run(() => DownloadAsync(file));

                // Create directory before writing to file if it doesn't exist
                new FileInfo(Path.Join(downloadLocation, file)).Directory.Create();

                await File.WriteAllBytesAsync(Path.Join(downloadLocation, file), contents);

                ++i;
            }

            OnDownloadCompleted?.Invoke();
        }

        internal static async Task AttemptCopyFilesFromListAsync(List<string> fileList, string copyLocation)
        {
            /* Needs refactored to not use config file for path
             * ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
             * 
            double listLength = fileList.Count;
            List<string> newFileList = new List<string>();

            // Get source installation properties
            string configLocation = Path.Join(Directory.GetCurrentDirectory(), "config.json");
            using StreamReader sr = File.OpenText(configLocation);
            ConfigProperties configProperties = JsonConvert.DeserializeObject<ConfigProperties>(sr.ReadToEnd());

            double i = 1;
            // Key == name, Value == url
            foreach (string file in fileList)
            {
                // Notify UI of filename
                OnCurrentFileDownloading?.Invoke("copy", file, i, listLength);

                // Create directory before writing to file if it doesn't exist
                new FileInfo(Path.Join(copyLocation, file)).Directory.Create();

                // If file exists at source installation, copy it
                if (File.Exists(Path.Join(configProperties.SWGLocation, file)))
                {
                    await CopyFileAsync(Path.Join(configProperties.SWGLocation, file), Path.Join(copyLocation, file));
                }
                // If file doesn't exist in source location, add to new list to be returned
                else
                {
                    newFileList.Add(file);
                }
                
                ++i;
            }
            */
        }

        static async Task CopyFileAsync(string sourcePath, string destinationPath)
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
