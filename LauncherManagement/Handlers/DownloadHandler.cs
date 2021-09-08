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
        static readonly List<string> ClientChecksums = new() { "582f8324c2b280b6b1de1fd00180729d", "", "", "" };

        public static Action OnDownloadCompleted { get; set; }
        public static Action<string, string, double, double> OnCurrentFileDownloading { get; set; }
        public static Action<string, double, double> OnFullScanFileCheck { get; set; }
        public static Action<long, long, int> OnDownloadProgressUpdated { get; set; }
        public static Action<string> OnServerError { get; set; }
        public static Action<string> OnInstallCheckFailed { get; set; }
        public static string BaseGameLocation { get; set; }

        internal static async Task<List<DownloadableFile>> DownloadManifestAsync(string manifestFile)
        {
            byte[] contents = await Task.Run(() => DownloadAsync(manifestFile).Result);

            return GetFileList(System.Text.Encoding.UTF8.GetString(contents));
        }

        internal static async Task<List<string>> DownloadTreList()
        {
            Dictionary<string, string> launcherSettings = await _configHandler.GetLauncherSettings();

            launcherSettings.TryGetValue("ManifestFileUrl", out string primaryUrl);
            launcherSettings.TryGetValue("BackupManifestFileUrl", out string backupUrl);
            launcherSettings.TryGetValue("ManifestFilePath", out string manifestFilePath);

            string address = _primaryServerOffline ? backupUrl : primaryUrl;

            string liveCfgAddress = address + manifestFilePath.Split("/")[0] + $"/livecfg.json";

            using WebClient client = new();

            string contents = client.DownloadString(new Uri(liveCfgAddress));

            return JsonConvert.DeserializeObject<List<string>>(contents);
        }

        internal static async Task DownloadFilesFromListAsync(List<string> fileList, string downloadLocation, bool isMod = false)
        {
            double listLength = fileList.Count;

            double i = 1;
            // Key == name, Value == url
            foreach (string file in fileList)
            {
                // Notify UI of filename
                OnCurrentFileDownloading?.Invoke("download", file, i, listLength);

                byte[] contents = await Task.Run(() => DownloadAsync(file, isMod));

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
            foreach (JToken item in JArray.Parse(listData))
            {
                // Deserialize the JSON string, add it to a new 'DownloadableFile' object and add it to the file list
                DownloadableFile downloadableFile = JsonConvert.DeserializeObject<DownloadableFile>(item.ToString());

                fileList.Add(downloadableFile);
            }

            return fileList;
        }

        internal static string GetMd5Checksum(string filePath)
        {
            using MD5 md5 = MD5.Create();
            using FileStream stream = File.OpenRead(filePath);
            
            return BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();    
        }

        internal static async Task<List<string>> GetBadFilesAsync(string downloadLocation, List<DownloadableFile> fileList, bool isFullScan = false)
        {
            List<string> newFileList = new();

            double listLength = fileList.Count;

            double i = 1;
            await Task.Run(() =>
            {
                foreach (DownloadableFile file in fileList)
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
                                if (file.Name is "SWGEmu.exe" or "SwgClient_r.exe")
                                {
                                    // Calculate MD5 checksum
                                    string result = GetMd5Checksum(Path.Join(downloadLocation, file.Name));

                                    bool fileNeedsAdded = true;
                                    ClientChecksums.ForEach(checksum =>
                                    {
                                        // If MD5 checksum doesn't match the manifest, or the hardcoded patched sums, add to list
                                        if (result != file.Md5 || result != checksum)
                                        {
                                            fileNeedsAdded = false;
                                        }
                                    });

                                    if (fileNeedsAdded)
                                    {
                                        newFileList.Add(file.Name);
                                    }
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

        public static async Task CheckFilesAsync(string downloadLocation, bool isFullScan = false, string modName = "", bool isTreMod = false)
        {
            _launcherSettings = await _configHandler.GetLauncherSettings();
            _launcherSettings.TryGetValue("ManifestFilePath", out string manifestFilePath);

            List<DownloadableFile> downloadableFiles = new();

            if (string.IsNullOrEmpty(modName))
            {
                downloadableFiles = await DownloadManifestAsync(manifestFilePath);
            }
            else
            {
                downloadableFiles = await DownloadManifestAsync(manifestFilePath.Split("/")[0] + $"/{modName}.json");

                if (isTreMod)
                {
                    TreModHandler treModHandler = new();
                    await treModHandler.EnableMod(modName, downloadableFiles);
                }
            }

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

            await DownloadFilesFromListAsync(fileList, downloadLocation, !string.IsNullOrEmpty(modName));
        }

        internal static async Task<byte[]> DownloadAsync(string file, bool isMod = false)
        {
            _launcherSettings = await _configHandler.GetLauncherSettings();

            byte[] data;

            using WebClient client = new();

            Uri uri;
            if (_primaryServerOffline)
            {
                _launcherSettings.TryGetValue("BackupManifestFileUrl", out string backupManifestFileUrl);
                if (isMod)
                {
                    string fileUrl = backupManifestFileUrl + "mods/" + file;
                    uri = new Uri(fileUrl);
                }
                else
                {
                    uri = new Uri(backupManifestFileUrl + file);
                }
            }
            else
            {
                _launcherSettings.TryGetValue("ManifestFileUrl", out string manifestFileUrl);
                if (isMod)
                {
                    string fileUrl = manifestFileUrl + "mods/" + file;
                    uri = new Uri(fileUrl);
                }
                else
                {
                    uri = new Uri(manifestFileUrl + file);
                }
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

        static void CheckSpecialCircumstances(string modName, string gamePath)
        {
            try
            {
                if (modName == "reshade")
                {
                    Directory.Delete(Path.Join(gamePath, "reshade-shaders"), true);
                    File.Delete(Path.Join(gamePath, "d3d9.log"));
                }
            }
            catch { }
        }

        public async static Task DeleteNonTreMod(string modName)
        {
            Dictionary<string, string> launcherSettings = await _configHandler.GetLauncherSettings();

            launcherSettings.TryGetValue("ManifestFilePath", out string manifestFilePath);

            List<DownloadableFile> downloadableFiles = downloadableFiles = await DownloadManifestAsync(manifestFilePath.Split("/")[0] + $"/{modName}.json");

            SettingsHandler settingsHandler = new();

            string gamePath = await settingsHandler.GetGameLocationAsync();

            try
            {
                foreach (DownloadableFile file in downloadableFiles)
                {
                    string filePath = Path.Join(gamePath, file.Name).Replace("\\", "/");

                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                }
            
                foreach (DownloadableFile file in downloadableFiles)
                {
                    string filePath = Path.Join(gamePath, file.Name).Replace("\\", "/");

                    string dir = Path.GetDirectoryName(filePath);

                    if (Directory.Exists(dir))
                    {
                        if (Directory.GetFiles(dir).Length == 0 && Directory.GetDirectories(dir).Length == 0)
                        {
                            Directory.Delete(dir);
                        }
                    }
                }
            }
            catch { }

            CheckSpecialCircumstances(modName, gamePath);
        }

        static void OnDownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            OnDownloadProgressUpdated?.Invoke(e.BytesReceived, e.TotalBytesToReceive, e.ProgressPercentage);
        }
    }
}
