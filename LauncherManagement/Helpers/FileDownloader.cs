using System;
using System.Threading.Tasks;
using System.Net;

namespace LauncherManagement
{
    public abstract class FileDownloader
    {
        public static Action<long, long, int> OnDownloadProgressUpdated;
        public static Action<string> OnServerError;

        internal static async Task<byte[]> DownloadAsync(string file)
        {
            byte[] data;

            using (var client = new WebClient())
            {
                Uri uri;
                if (ServerProperties.PrimaryServerOffline)
                {
                    uri = new Uri(ServerProperties.BackupManifestFileUrl + file);
                }
                else
                {
                    uri = new Uri(ServerProperties.ManifestFileUrl + file);
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

                    Uri uri2 = new Uri(ServerProperties.BackupManifestFileUrl + file);

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
