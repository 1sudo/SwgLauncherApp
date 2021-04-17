using System;
using System.Threading.Tasks;
using System.Net;

namespace LauncherManagement
{
    public abstract class FileDownloader
    {
        public static Action<long, long, int> onDownloadProgressUpdated;
        public static Action<string> onServerError;

        internal static async Task<byte[]> Download(string url)
        {
            byte[] data;

            using (var client = new WebClient())
            {
                Uri uri = new Uri(url);

                client.DownloadProgressChanged += OnDownloadProgressChanged;
                
                try
                {
                    data = await client.DownloadDataTaskAsync(uri);
                    return data;
                }
                // If the download server is unavailable
                catch (Exception e)
                {
                    onServerError?.Invoke(e.ToString());
                    return new byte[0];
                }
            }
        }

        private static void OnDownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            onDownloadProgressUpdated?.Invoke(e.BytesReceived, e.TotalBytesToReceive, e.ProgressPercentage);
        }
    }
}
