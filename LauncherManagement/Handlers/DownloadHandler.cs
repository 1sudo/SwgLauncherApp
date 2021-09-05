using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace LauncherManagement
{
    public class DownloadHandler
    {
        static readonly LauncherConfigHandler _configHandler = new();
        static Dictionary<string, string> _launcherSettings = new();
        static bool _primaryServerOffline = false;
        static readonly List<string> ClientChecksums = new() { "", "", "", "" };

        public static Action OnDownloadCompleted { get; set; }
        public static Action<string, string, double, double> OnCurrentFileDownloading { get; set; }
        public static Action<string, double, double> OnFullScanFileCheck { get; set; }
        public static Action<long, long, int> OnDownloadProgressUpdated { get; set; }
        public static Action<string> OnServerError { get; set; }
        public static Action<string> OnInstallCheckFailed { get; set; }
        public static string BaseGameLocation { get; set; }

        internal static async Task<List<DownloadableFile>> DownloadManifestAsync(string manifestFile)
        {
            var contents = await Task.Run(() => DownloadAsync(manifestFile).Result);

            return GetFileList(System.Text.Encoding.UTF8.GetString(contents));
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
            double listLength = fileList.Count;
            List<string> newFileList = new();

            double i = 1;
            // Key == name, Value == url
            foreach (string file in fileList)
            {
                // Notify UI of filename
                OnCurrentFileDownloading?.Invoke("copy", file, i, listLength);

                // Create directory before writing to file if it doesn't exist
                new FileInfo(Path.Join(copyLocation, file)).Directory.Create();

                if (copyLocation != BaseGameLocation)
                {
                    // If file exists at source installation, copy it
                    if (File.Exists(Path.Join(BaseGameLocation, file)))
                    {
                        await CopyFileAsync(Path.Join(BaseGameLocation, file), Path.Join(copyLocation, file));
                    }
                    // If file doesn't exist in source location, add to new list to be returned
                    else
                    {
                        newFileList.Add(file);
                    }
                }

                ++i;
            }
        }

        static async Task CopyFileAsync(string sourcePath, string destinationPath)
        {
            using Stream source = File.OpenRead(sourcePath);
            using Stream destination = File.Create(destinationPath);
            await source.CopyToAsync(destination);
        }

        internal static List<DownloadableFile> GetFileList(string listData)
        {
            List<DownloadableFile> fileList = new();

            // Parses a JSON array and iterates through items in the array
            foreach (var item in JArray.Parse(listData))
            {
                // Deserialize the JSON string, add it to a new 'DownloadableFile' object and add it to the file list
                DownloadableFile downloadableFile = JsonConvert.DeserializeObject<DownloadableFile>(item.ToString());

                fileList.Add(downloadableFile);
            }

            return fileList;
        }

        internal static string GetMd5Checksum(string filePath)
        {
            using var md5 = MD5.Create();
            using var stream = File.OpenRead(filePath);
            
            return BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();    
        }

        internal static async Task<List<string>> GetBadFilesAsync(string downloadLocation, List<DownloadableFile> fileList, bool isFullScan = false)
        {
            var newFileList = new List<string>();

            double listLength = fileList.Count;

            double i = 1;
            await Task.Run(() =>
            {
                foreach (var file in fileList)
                {
                    if (isFullScan)
                    {
                        try
                        {
                            if (File.Exists(Path.Join(downloadLocation, file.Name)))
                            {
                                OnFullScanFileCheck?.Invoke($"Checking File { file.Name }...", i, listLength);

                                string result = GetMd5Checksum(Path.Join(downloadLocation, file.Name));

                                // If checksum doesn't match, add to download list
                                if (result != file.Md5)
                                {
                                    newFileList.Add(file.Name);
                                }
                            }
                            // If file doesn't exist, add to download list
                            else
                            {
                                newFileList.Add(file.Name);
                            }
                        }
                        // Some other dumb shit happened, add file to list
                        catch
                        {
                            newFileList.Add(file.Name);
                        }

                        ++i;
                    }
                    else
                    {
                        try
                        {
                            if (File.Exists(Path.Join(downloadLocation, file.Name)))
                            {
                                // If file is wrong size, add to download list
                                if (new FileInfo(Path.Join(downloadLocation, file.Name)).Length != file.Size)
                                {
                                    newFileList.Add(file.Name);
                                }

                                // Check MD5 sums for game client regardless of full scan or file size check
                                // This ensures the executable doesn't get re-downloaded when it is patched on the fly (FPS edits, for example)
                                if (file.Name == "SWGEmu.exe" || file.Name == "SwgClient_r.exe")
                                {
                                    // Calculate MD5 checksum
                                    string result = GetMd5Checksum(Path.Join(downloadLocation, file.Name));

                                    bool fileAdded = false;
                                    ClientChecksums.ForEach(checksum =>
                                    {
                                        // Check to see if the file has already been added, prevent it from being downloaded multiple times
                                        if (!fileAdded)
                                        {
                                            // If MD5 checksum doesn't match the manifest, or the hardcoded patched sums, add to list
                                            if (result != file.Md5 || result != checksum)
                                            {
                                                newFileList.Add(file.Name);
                                                fileAdded = true;
                                            }
                                        }
                                    });
                                }
                            }
                            // If file doesn't exist, add to download list
                            else
                            {
                                newFileList.Add(file.Name);
                            }
                        }
                        // Some other dumb shit happened, add file to list
                        catch
                        {
                            newFileList.Add(file.Name);
                        }
                    }
                }
            });

            return newFileList;
        }

        public static bool CheckBaseInstallation(string location)
        {
            try
            {
                if (!Directory.Exists(location))
                {
                    return false;
                }

                // Files that are required to exist
                List<string> filesToCheck = new()
                {
                    "dpvs.dll",
                    "Mss32.dll",
                    "dbghelp.dll"
                };

                // Files in supposed SWG directory
                string[] files = Directory.GetFiles(location, "*.*", SearchOption.AllDirectories);

                int numRequiredFiles = 0;

                foreach (string fileToCheck in filesToCheck)
                {
                    foreach (string file in files)
                    {
                        if (fileToCheck == file.Split(location + "\\")[1].Trim())
                        {
                            numRequiredFiles++;
                        }
                    }
                }

                if (numRequiredFiles == 3)
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                OnInstallCheckFailed?.Invoke(ex.Message.ToString());
            }

            return false;
        }

        public static async Task CheckFilesAsync(string downloadLocation, bool isFullScan = false)
        {
            _launcherSettings = await _configHandler.GetLauncherSettings();

            _launcherSettings.TryGetValue("ManifestFilePath", out string manifestFilePath);
            Trace.WriteLine(manifestFilePath);
            var downloadableFiles = await DownloadManifestAsync(manifestFilePath);

            List<string> fileList;
            if (isFullScan)
            {
                fileList = await Task.Run(() => GetBadFilesAsync(downloadLocation, downloadableFiles, true));
            }
            else
            {
                fileList = await Task.Run(() => GetBadFilesAsync(downloadLocation, downloadableFiles));
                await Task.Run(() => AttemptCopyFilesFromListAsync(fileList, downloadLocation));
                fileList = await Task.Run(() => GetBadFilesAsync(downloadLocation, downloadableFiles));
            }

            await DownloadFilesFromListAsync(fileList, downloadLocation);
        }

        internal static async Task<byte[]> DownloadAsync(string file)
        {
            _launcherSettings = await _configHandler.GetLauncherSettings();

            byte[] data;

            using var client = new WebClient();

            Uri uri;
            if (_primaryServerOffline)
            {
                _launcherSettings.TryGetValue("BackupManifestFileUrl", out string backupManifestFileUrl);
                uri = new Uri(backupManifestFileUrl + file);
            }
            else
            {
                _launcherSettings.TryGetValue("ManifestFileUrl", out string manifestFileUrl);
                uri = new Uri(manifestFileUrl + file);
            }

            client.DownloadProgressChanged += OnDownloadProgressChanged;

            try
            {
                data = await client.DownloadDataTaskAsync(uri);
                return data;
            }
            // If the download server is unavailable, try the backup server
            catch
            {
                _primaryServerOffline = true;

                _launcherSettings.TryGetValue("BackupManifestFileUrl", out string backupManifestFileUrl);
                Uri uri2 = new(backupManifestFileUrl + file);

                client.DownloadProgressChanged += OnDownloadProgressChanged;

                try
                {
                    data = await client.DownloadDataTaskAsync(uri2);
                    return data;
                }
                // If the backup server is unavailable, error
                catch (Exception e)
                {
                    OnServerError?.Invoke(e.ToString());
                    return Array.Empty<byte>();
                }
            }
            
        }

        static void OnDownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            OnDownloadProgressUpdated?.Invoke(e.BytesReceived, e.TotalBytesToReceive, e.ProgressPercentage);
        }
    }
}
