using System;
using System.Threading.Tasks;
using System.Net;

namespace LauncherManagement
{
    public abstract class FileDownloader
    {
        public static Action<long, long, int> OnDownloadProgressUpdated;
        public static Action<string> OnServerError;

        internal static async Task<byte[]> DownloadAsync(string url)
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
                    OnServerError?.Invoke(e.ToString());
                    return new byte[0];
                }
            }
        }

        static void OnDownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            OnDownloadProgressUpdated?.Invoke(e.BytesReceived, e.TotalBytesToReceive, e.ProgressPercentage);
        }
    }
}
