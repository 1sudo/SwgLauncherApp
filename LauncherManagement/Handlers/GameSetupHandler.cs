using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace LauncherManagement
{
    public class GameSetupHandler : ValidationHandler
    {
        public static async Task CheckFilesAsync(string downloadLocation, bool isFullScan = false)
        {
            var downloadableFiles = await DownloadManifestAsync(ServerProperties.ManifestFilePath);

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

        public static string GetGamePath()
        {
            JObject json = new JObject();
            try
            {
                json = JObject.Parse(File.ReadAllText(Path.Join(Directory.GetCurrentDirectory(), "config.json")));
            } 
            catch { }

            JToken location;
            try
            {
                if (json.TryGetValue("GameLocation", out location))
                {
                    return location.ToString();
                }
            }
            catch { }

            return string.Empty;
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
