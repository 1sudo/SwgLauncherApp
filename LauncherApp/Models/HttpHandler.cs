using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Security;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LibLauncherUtil.Properties;

namespace LauncherApp.Models;

public class HttpHandler
{
    public static double DownloadSpeed { get; private set; }
    public static bool IsDownloading { get; private set; }
    public static Action? OnDownloadStarted { get; set; }
    public static Action? OnDownloadCompleted { get; set; }
    public static Action<string, string, double, double>? OnCurrentFileDownloading { get; set; }
    public static Action<double>? OnDownloadProgressUpdated { get; set; }
    public static Action<double>? OnDownloadRateUpdated { get; set; }
    public static Action<string>? OnServerError { get; set; }
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
        catch (Exception e)
        {
            OnCannotReachWebserver?.Invoke();
            Logger.Instance.Log(e, ERROR);
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
        catch (Exception e)
        {
            OnCannotReachWebserver?.Invoke();
            Logger.Instance.Log(e, ERROR);
            return new List<DownloadableFile>();
        }
    }

    private static void NotifyDownloadSpeed()
    {
        while (IsDownloading)
        {
            Thread.Sleep(200);
            OnDownloadRateUpdated?.Invoke(DownloadSpeed);
        }
    }

    internal static async Task DownloadFilesFromListAsync(List<string> fileList, string downloadLocation, long totalDownloadSize)
    {
        var config = ConfigFile.GetCurrentServer();
        if (config is null) return;

        OnDownloadStarted?.Invoke();
        IsDownloading = true;

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        Task.Run(NotifyDownloadSpeed);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

        var tasks = new List<Task>();
        var semaphore = new SemaphoreSlim(config.DownloadConcurrency);
        var totalBytesDownloaded = 0L;
        var stopwatch = Stopwatch.StartNew();

        foreach (var file in fileList)
        {
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    await semaphore.WaitAsync();

                    var filePath = Path.Combine(downloadLocation, file);
                    var fileInfo = new FileInfo(filePath);

                    await DownloadFileAsync(downloadLocation, file, bytesDownloaded =>
                    {
                        Interlocked.Add(ref totalBytesDownloaded, bytesDownloaded);
                        var elapsedSeconds = stopwatch.Elapsed.TotalSeconds;

                        if (elapsedSeconds > 0)
                        {
                            var downloadRate = totalBytesDownloaded / elapsedSeconds;

                            OnDownloadProgressUpdated?.Invoke(totalBytesDownloaded / (double)totalDownloadSize * 1000);
                            DownloadSpeed = Math.Round(downloadRate / 125000, 2);
                        }
                    });
                }
                finally
                {
                    semaphore.Release();
                }
            }));
        }

        await Task.WhenAll(tasks);

        OnDownloadCompleted?.Invoke();
        IsDownloading = false;
    }

    private static async Task DownloadFileAsync(string downloadLocation, string fileName, Action<long> downloadProgressCallback)
    {
        try
        {
            var config = ConfigFile.GetConfig();
            var manifestFileUrl = Path.Join(config?.Servers?[config.ActiveServer]?.ServiceUrl, "files");
            var downloadUrl = Path.Join(manifestFileUrl, fileName);

            using var handler = new SocketsHttpHandler();

            handler.SslOptions = new SslClientAuthenticationOptions
            {
                RemoteCertificateValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
            };

            using var client = new HttpClient(handler);
            client.DefaultRequestVersion = new Version(2, 0);

            var request = new HttpRequestMessage(HttpMethod.Get, downloadUrl)
            {
                VersionPolicy = HttpVersionPolicy.RequestVersionOrHigher
            };

            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            if (response.IsSuccessStatusCode)
            {
                await using var contentStream = await response.Content.ReadAsStreamAsync();

                var dir = Path.GetDirectoryName(Path.Join(downloadLocation, fileName));

                if (dir is not null)
                {
                    Directory.CreateDirectory(dir);
                }

                await using var fileStream = new FileStream(Path.Join(downloadLocation, fileName),
                    FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true);

                await DoStreamWriteAsync(contentStream, fileStream, downloadProgressCallback);
            }
        }
        catch (Exception e)
        {
            Logger.Instance.Log(e, ERROR);
        }
    }

    private static async Task DoStreamWriteAsync(Stream contentStream, Stream fileStream, Action<long> downloadProgressCallback)
    {
        var stopwatch = Stopwatch.StartNew();
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

                var elapsedSeconds = stopwatch.Elapsed.TotalSeconds;

                if (elapsedSeconds > 0)
                {
                    downloadProgressCallback(read);
                }
            }
        }
    }
}
