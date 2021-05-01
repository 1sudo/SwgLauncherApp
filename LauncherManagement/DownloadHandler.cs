﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace LauncherManagement
{
    public class DownloadHandler : FileDownloader
    {
        public static Action OnDownloadCompleted;
        public static Action<string> OnCurrentFileDownloading;
        public static Action<string> OnFullScanFileCheck;

        internal static async Task<List<DownloadableFile>> DownloadManifest(string manifestUrl)
        {
            var contents = await Task.Run(() => Download(manifestUrl).Result);

            return JsonHandler.GetFileList(System.Text.Encoding.UTF8.GetString(contents));
        }

        internal static async Task DownloadFilesFromList(Dictionary<string, string> fileList, string downloadLocation)
        {
            // Key == name, Value == url
            foreach (KeyValuePair<string, string> file in fileList)
            {
                // Notify UI of filename
                OnCurrentFileDownloading?.Invoke(file.Key);

                var contents = await Task.Run(() => Download(file.Value));

                // Create directory before writing to file if it doesn't exist
                new FileInfo(Path.Join(downloadLocation, file.Key)).Directory.Create();

                await File.WriteAllBytesAsync(Path.Join(downloadLocation, file.Key), contents); 
            }

            OnDownloadCompleted?.Invoke();
        }
    }
}
