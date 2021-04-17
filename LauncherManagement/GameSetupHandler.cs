using System.Collections.Generic;
using System.Threading.Tasks;

namespace LauncherManagement
{
    public class GameSetupHandler : ValidationHandler
    {
        public static async Task CheckFiles(bool isFullScan = false)
        {
            var downloadableFiles = await DownloadManifest("http://localhost/required.json");

            Dictionary<string, string> fileList;
            if (isFullScan)
                fileList = await Task.Run(() => GetBadFiles(downloadableFiles, true));
            else
                fileList = await Task.Run(() => GetBadFiles(downloadableFiles));

            await DownloadFilesFromList(fileList);
        }

        public static (bool flag, string reason) ValidateGamePath(string location)
        {
            return CheckValidInstallation(location);
        }
    }
}
