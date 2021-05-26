using System;
using System.Threading.Tasks;
using System.Net;
using System.Collections.Generic;

namespace LauncherManagement
{
    public abstract class FileDownloader
    {
        public static Action<long, long, int> OnDownloadProgressUpdated;
        public static Action<string> OnServerError;
        static LauncherConfigHandler _configHandler = new LauncherConfigHandler();
        static Dictionary<string, string> _launcherSettings = new Dictionary<string, string>();

        internal static async Task<byte[]> DownloadAsync(string file)
        {
            _launcherSettings = await _configHandler.GetLauncherSettings();

            byte[] data;

            using (var client = new WebClient())
            {
                Uri uri;
                if (ServerProperties.PrimaryServerOffline)
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
                    ServerProperties.PrimaryServerOffline = true;

                    _launcherSettings.TryGetValue("BackupManifestFileUrl", out string backupManifestFileUrl);
                    Uri uri2 = new Uri(backupManifestFileUrl + file);

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
                        return new byte[0];
                    }
                }
            }
        }

        static void OnDownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            OnDownloadProgressUpdated?.Invoke(e.BytesReceived, e.TotalBytesToReceive, e.ProgressPercentage);
        }
    }
}
