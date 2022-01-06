using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Security.Cryptography;

namespace LauncherManagement
{
    public class DownloadHandler
    {
        static bool _primaryServerOffline = false;

        static readonly List<string> ClientChecksums = new() { 
            "a487bcf7abe27ba9c02e3121ba44367e", // 30 FPS
            "50692684e090b200ea28681e7ae7da5b", // 60 FPS
            "2a55323f8774c43231331cb00014a011", // 144 FPS
            "38feda8e17042a5bc9edf7d9959bdbfe"  // 240 FPS
        };

        public static Action? OnDownloadCompleted { get; set; }
        public static Action<string, string, double, double>? OnCurrentFileDownloading { get; set; }
        public static Action<string, double, double>? OnFullScanFileCheck { get; set; }
        public static Action<long, long, int>? OnDownloadProgressUpdated { get; set; }
        public static Action<string>? OnServerError { get; set; }
        public static Action<string>? OnInstallCheckFailed { get; set; }
        public static string? BaseGameLocation { get; set; }
        static ConfigFile? _config;

        internal static async Task<List<DownloadableFile>> DownloadManifestAsync(bool isTreMod = false, string treMod = "")
        {
            if (isTreMod)
            {
                return GetFileList(await Task.Run(() => DownloadAsync<string>(treMod, "", true, true)));
            }
            else
            {
                return GetFileList(await Task.Run(() => DownloadAsync<string>("", "", false, true)));
            }
        }

        internal static async Task<List<string>> DownloadTreList()
        {
            string? primaryUrl = _config!.Servers![_config.ActiveServer].ManifestFileUrl;
            string? backupUrl = _config!.Servers![_config.ActiveServer].BackupManifestFileUrl;
            string? manifestFilePath = _config!.Servers![_config.ActiveServer].ManifestFilePath;

            string address = "";

            if (backupUrl is not null && primaryUrl is not null)
            {
                 address = _primaryServerOffline ? backupUrl : primaryUrl;
            }

            string liveCfgAddress = "";

            if (manifestFilePath is not null)
            {
                liveCfgAddress = address + manifestFilePath.Split("/")[0] + $"/livecfg.json";
            }
            
            using HttpClient cl = new();

            string contents = "";
            List<string>? treList = new();

            try
            {
                contents = await cl.GetStringAsync(new Uri(liveCfgAddress));

                if (contents is not null)
                {
                    treList = JsonConvert.DeserializeObject<List<string>>(contents);
                }
            }
            catch (Exception e)
            {
                await LogHandler.Log(LogType.ERROR, "| DownloadTreList | " + e.Message.ToString());
            }

            if (treList is not null)
            {
                return treList;
            }
            else
            {
                return new List<string>();
            }
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

                await Task.Run(() => DownloadAsync<bool>(file, Path.Join(downloadLocation, file), isMod));

                i++;
            }

            OnDownloadCompleted?.Invoke();
        }

        public static async Task AttemptCopyFilesFromListAsync(List<string> fileList, string copyLocation, bool isDirChange = false, string previousDir = "")
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
                if (copyLocation is not null && file is not null)
                {
                    new FileInfo(Path.Join(copyLocation, file)).Directory!.Create();
                }

                if (isDirChange)
                {
                    BaseGameLocation = previousDir;
                }

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
                        if (file is not null)
                        {
                            newFileList.Add(file);
                        }
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
                                        if (result == file.Md5 || result == checksum)
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

        public async static Task<bool> CheckBaseInstallation(string location)
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
            catch (Exception e)
            {
                await LogHandler.Log(LogType.ERROR, "| CheckBaseInstallation | " + e.Message.ToString());
                OnInstallCheckFailed?.Invoke(e.Message.ToString());
            }

            return false;
        }

        public static async Task CheckFilesAsync(ConfigFile config, bool isFullScan = false, string modName = "", bool isTreMod = false, bool isDirChange = false, string previousDir = "")
        {
            _config = config;

            string? manifestFilePath = _config!.Servers![_config.ActiveServer].ManifestFilePath;

            List<DownloadableFile> downloadableFiles = new();

            if (string.IsNullOrEmpty(modName) && manifestFilePath is not null)
            {
                downloadableFiles = await DownloadManifestAsync();
            }
            else
            {
                if (manifestFilePath is not null)
                {
                    downloadableFiles = await DownloadManifestAsync(true, modName);
                }

                if (isTreMod)
                {
                    TreModHandler treModHandler = new();
                    await treModHandler.EnableMod(modName, downloadableFiles);
                }
            }

            List<string> fileList;
            if (isFullScan)
            {
                fileList = await Task.Run(() => GetBadFilesAsync(_config.Servers![_config.ActiveServer].GameLocation!, downloadableFiles, true));
            }
            else
            {
                fileList = await Task.Run(() => GetBadFilesAsync(_config.Servers![_config.ActiveServer].GameLocation!, downloadableFiles));

                if (isDirChange)
                {
                    await Task.Run(() => AttemptCopyFilesFromListAsync(fileList, _config.Servers![_config.ActiveServer].GameLocation!, true, previousDir));
                }
                else
                {
                    await Task.Run(() => AttemptCopyFilesFromListAsync(fileList, _config.Servers![_config.ActiveServer].GameLocation!));
                }
                
                fileList = await Task.Run(() => GetBadFilesAsync(_config.Servers![_config.ActiveServer].GameLocation!, downloadableFiles));
            }

            await DownloadFilesFromListAsync(fileList, _config.Servers![_config.ActiveServer].GameLocation!, !string.IsNullOrEmpty(modName));
        }

        internal static async Task<T> DownloadAsync<T>(string file, string downloadLocation, bool isMod = false, bool isManifest = false)
        {
            string? manifestFileUrl = _config!.Servers![_config.ActiveServer].ManifestFileUrl;
            string? manifestFilePath = _config!.Servers![_config.ActiveServer].ManifestFilePath;
            string? backupManifestFileUrl = _config!.Servers![_config.ActiveServer].BackupManifestFileUrl;

            using HttpClient client = new();

            Uri uri;

            if (isManifest)
            {
                try
                {
                    if (isMod)
                    {
                        manifestFilePath = manifestFilePath!.Split("/")[0] + $"/{file}.json";
                    }

                    uri = new Uri(manifestFileUrl + manifestFilePath);

                    return (T)Convert.ChangeType(await client.GetStringAsync(uri), typeof(T));
                }
                catch
                {
                    try
                    {
                        uri = new Uri(backupManifestFileUrl + manifestFilePath);

                        return (T)Convert.ChangeType(await client.GetStringAsync(uri), typeof(T));
                    }
                    catch (Exception e)
                    {
                        await LogHandler.Log(LogType.CRITICAL, "| DownloadAsync | All download servers unavailable!");
                        OnServerError?.Invoke(e.ToString());
                        return (T)Convert.ChangeType(false, typeof(T));
                    }
                }
            }

            if (_primaryServerOffline)
            {
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

            try
            {
                using HttpResponseMessage response = client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead).Result;
                long length = int.Parse(response.Content.Headers.First(h => h.Key.Equals("Content-Length")).Value.First());

                response.EnsureSuccessStatusCode();

                using Stream contentStream = await response.Content.ReadAsStreamAsync();
                using Stream fileStream = new FileStream(downloadLocation, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

                await DoStreamWriteAsync(contentStream, fileStream, length);

                return (T)Convert.ChangeType(true, typeof(T));
            }
            // If the download server is unavailable, try the backup server
            catch
            {
                await LogHandler.Log(LogType.INFO, "| DownloadAsync | Primary download server unavailable. Downloading from backup server.");

                _primaryServerOffline = true;

                Uri uri2 = new(backupManifestFileUrl + file);

                try
                {
                    using HttpResponseMessage response = client.GetAsync(uri2, HttpCompletionOption.ResponseHeadersRead).Result;
                    long length = int.Parse(response.Content.Headers.First(h => h.Key.Equals("Content-Length")).Value.First());

                    response.EnsureSuccessStatusCode();

                    using Stream contentStream = await response.Content.ReadAsStreamAsync();
                    using Stream fileStream = new FileStream(downloadLocation, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

                    await DoStreamWriteAsync(contentStream, fileStream, length);

                    return (T)Convert.ChangeType(true, typeof(T));
                }
                // If the backup server is unavailable, error
                catch (Exception e)
                {
                    await LogHandler.Log(LogType.CRITICAL, "| DownloadAsync | All download servers unavailable!");
                    OnServerError?.Invoke(e.ToString());
                    return (T)Convert.ChangeType(false, typeof(T));
                }
            }
        }

        internal static async Task DoStreamWriteAsync(Stream contentStream, Stream fileStream, long length)
        {
            long bytesReceived = 0L;
            long totalBytesToReceive = 0L;
            byte[] buffer = new byte[8192];
            bool endOfStream = false;

            while (!endOfStream)
            {
                var read = await contentStream.ReadAsync(buffer);
                if (read == 0)
                {
                    endOfStream = true;
                }
                else
                {
                    await fileStream.WriteAsync(buffer.AsMemory(0, read));

                    bytesReceived += read;
                    totalBytesToReceive += 1;

                    if (totalBytesToReceive % 100 == 0)
                    {
                        int progressPercentage = (int)Math.Round((double)bytesReceived / length * 1000, 0);
                        OnDownloadProgressUpdated?.Invoke(bytesReceived, totalBytesToReceive, progressPercentage);
                    }
                }
            }
        }

        static async Task CheckSpecialCircumstances(string modName, string gamePath)
        {
            try
            {
                if (modName == "reshade")
                {
                    Directory.Delete(Path.Join(gamePath, "reshade-shaders"), true);
                    File.Delete(Path.Join(gamePath, "d3d9.log"));
                }
            }
            catch (Exception e)
            {
                await LogHandler.Log(LogType.ERROR, "| CheckSpecialCircumstances |" + e.Message);
            }
        }

        public async static Task DeleteNonTreMod(string modName)
        {
            string? manifestFilePath = _config!.Servers![_config.ActiveServer].ManifestFilePath;

            List<DownloadableFile> downloadableFiles = new();

            if (manifestFilePath is not null)
            {
                downloadableFiles = await DownloadManifestAsync(true, modName);
            }

            string? gamePath = _config!.Servers![_config.ActiveServer].GameLocation;

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
                    string? dir = "";

                    dir = Path.GetDirectoryName(filePath);

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

            await CheckSpecialCircumstances(modName, gamePath!);
        }

        static void OnDownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            OnDownloadProgressUpdated?.Invoke(e.BytesReceived, e.TotalBytesToReceive, e.ProgressPercentage);
        }
    }
}
