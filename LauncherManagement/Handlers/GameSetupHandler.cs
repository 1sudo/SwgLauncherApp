using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace LauncherManagement
{
    public class GameSetupHandler : ValidationHandler
    {
        static LauncherConfigHandler _configHandler = new LauncherConfigHandler();
        static Dictionary<string, string> _launcherSettings = new Dictionary<string, string>();

        public static async Task CheckFilesAsync(string downloadLocation, bool isFullScan = false)
        {
            _launcherSettings = await _configHandler.GetLauncherSettings();

            _launcherSettings.TryGetValue("ManifestFilePath", out string manifestFilePath);
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

        public static bool ValidateBaseGame(string location)
        {
            return CheckBaseInstallation(location);
        }

        public static bool ValidateJsonFile(string file)
        {
            return ValidateJson(file);
        }
    }
}
