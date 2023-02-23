using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using LauncherApp.Models.Properties;

namespace LauncherApp.Models.Handlers;

public class HttpHandler
{
    static bool _primaryServerOffline = false;
    public static Action? OnDownloadStarted { get; set; }
    public static Action? OnDownloadCompleted { get; set; }
    public static Action<string, string, double, double>? OnCurrentFileDownloading { get; set; }
    public static Action<long, long, int>? OnDownloadProgressUpdated { get; set; }
    public static Action<string>? OnServerError { get; set; }
    public static Action<string>? OnInstallCheckFailed { get; set; }
    public static Action? OnCannotReachWebserver { get; set; }
    
    

    internal static async Task<VersionFile> DownloadVersionAsync()
    {
        using var handler = new HttpClientHandler();
        handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

        using var client = new HttpClient(handler);
        client.DefaultRequestVersion = new Version(2, 0);

        client.Timeout = TimeSpan.FromSeconds(5);

        var config = ConfigFile.GetConfig();

        var manifestUrl = Path.Join(config?.Servers?[config.ActiveServer]?.ServiceUrl, "files");

        try
        {
            VersionFile versionFile = new();

            await Task.Run(async () =>
            {
                using var response = await client.GetAsync(new Uri(Path.Join(manifestUrl, "version.json")),
                    HttpCompletionOption.ResponseHeadersRead);

                await using var contentStream = await response.Content.ReadAsStreamAsync();

                versionFile = await JsonSerializer.DeserializeAsync<VersionFile>(contentStream);
            });

            return versionFile;
        }
        catch
        {
            OnCannotReachWebserver?.Invoke();
            return new VersionFile { Version = 1 };
        }
    }

    internal static async Task<List<DownloadableFile>> DownloadManifestAsync()
    {
        using var handler = new HttpClientHandler();
        handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

        using var client = new HttpClient(handler);
        client.DefaultRequestVersion = new Version(2, 0);

        client.Timeout = TimeSpan.FromSeconds(5);

        var config = ConfigFile.GetConfig();

        var manifestUrl = Path.Join(config?.Servers?[config.ActiveServer]?.ServiceUrl, "files");

        try
        {
            using var response = await client.GetAsync(new Uri(Path.Join(manifestUrl, "manifest/required.json")),
                HttpCompletionOption.ResponseHeadersRead);

            if (!response.IsSuccessStatusCode) return new List<DownloadableFile>();

            await using var contentStream = await response.Content.ReadAsStreamAsync();

            return await JsonSerializer.DeserializeAsync<List<DownloadableFile>>(contentStream) ?? new List<DownloadableFile>();
        }
        catch
        {
            OnCannotReachWebserver?.Invoke();
            return new List<DownloadableFile>();
        }
    }

    internal static async Task<List<string>> DownloadTreList()
    {
        var config = ConfigFile.GetConfig();

        var primaryUrl = Path.Join(config?.Servers?[config.ActiveServer]?.ServiceUrl, "files");
        var backupUrl = Path.Join(config?.Servers?[config.ActiveServer]?.BackupServiceUrl, "files");
        string? manifestFilePath = config!.Servers![config.ActiveServer].ManifestFilePath;
        string? address = _primaryServerOffline ? backupUrl ?? primaryUrl : primaryUrl ?? backupUrl;

        string? liveCfgAddress = manifestFilePath?.Split('/')?[0];
        liveCfgAddress = address + $"{liveCfgAddress}/livecfg.json" ?? "";

        using var handler = new HttpClientHandler();
        handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

        using var client = new HttpClient(handler);
        client.DefaultRequestVersion = new Version(2, 0);

        List<string>? treList = new();

        try
        {
            treList = JsonSerializer.Deserialize<List<string>>(await client.GetStringAsync(new Uri(liveCfgAddress)) ?? "");
        }
        catch (Exception e)
        {
            await LogHandler.Log(LogType.ERROR, "| DownloadTreList | " + e.Message.ToString());
        }

        return treList ?? new List<string>();
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

    internal static async Task DownloadFilesFromListAsync(List<string> fileList, string downloadLocation)
    {
        OnDownloadStarted?.Invoke();

        double listLength = fileList.Count;

        double i = 1;
        // Key == name, Value == url
        foreach (var file in fileList)
        {
            // Notify UI of filename
            OnCurrentFileDownloading?.Invoke("download", file, i, listLength);

            await DownloadFileAsync(downloadLocation, file);

            i++;
        }

        OnDownloadCompleted?.Invoke();
    }

    private static async Task DownloadFileAsync(string downloadLocation, string fileName)
    {
        try
        {
            var config = ConfigFile.GetConfig();

            var manifestFileUrl = Path.Join(config?.Servers?[config.ActiveServer]?.ServiceUrl, "files");

            var downloadUrl = Path.Join(manifestFileUrl, fileName);

            using var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

            using var client = new HttpClient(handler);
            client.DefaultRequestVersion = new Version(2, 0);

            using var response = await client.GetAsync(new Uri(downloadUrl),
                HttpCompletionOption.ResponseHeadersRead);

            long length = long.Parse(response.Content.Headers.First(h =>
                h.Key.Equals("Content-Length")).Value.First());

            if (response.IsSuccessStatusCode)
            {
                await using var contentStream = await response.Content.ReadAsStreamAsync();

                var dir = Path.GetDirectoryName(Path.Join(downloadLocation, fileName));

                if (dir is not null)
                {
                    Directory.CreateDirectory(dir);
                }

                await using var fileStream = new FileStream(Path.Join(downloadLocation, fileName),
                    FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 8192, useAsync: true);

                await DoStreamWriteAsync(contentStream, fileStream, length);
            }
        }
        catch { }
    }

    private static async Task DoStreamWriteAsync(Stream contentStream, Stream fileStream, long length)
    {
        var bytesReceived = 0L;
        var totalBytesToReceive = 0L;
        var buffer = new byte[8192];
        var endOfStream = false;

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
                    var progressPercentage = (int)Math.Round((double)bytesReceived / length * 1000, 0);
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
        var config = ConfigFile.GetConfig();

        string? manifestFilePath = config!.Servers![config.ActiveServer].ManifestFilePath;

        List<DownloadableFile> downloadableFiles = new();

        if (manifestFilePath is not null)
        {
            downloadableFiles = await DownloadManifestAsync();
        }

        string? gamePath = config!.Servers![config.ActiveServer].GameLocation;

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
}
