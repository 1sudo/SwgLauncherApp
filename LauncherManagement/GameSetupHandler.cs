using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace LauncherManagement
{
    public class GameSetupHandler : ValidationHandler
    {
        public static async Task CheckFiles(string downloadLocation, bool isFullScan = false)
        {
            var downloadableFiles = await DownloadManifest("http://localhost/required.json");

            Dictionary<string, string> fileList;
            if (isFullScan)
                fileList = await Task.Run(() => GetBadFiles(downloadLocation, downloadableFiles, true));
            else
                fileList = await Task.Run(() => GetBadFiles(downloadLocation, downloadableFiles));

            await DownloadFilesFromList(fileList, downloadLocation);
        }

        public static string GetDarknaughtPath()
        {
            JObject json = new JObject();
            try
            {
                json = JObject.Parse(File.ReadAllText(Path.Join(Directory.GetCurrentDirectory(), "config.json")));
            } 
            catch { }

            JToken location;

            if (json.TryGetValue("DarknaughtLocation", out location))
            {
                return location.ToString();
            }

            return string.Empty;
        }

        public static bool ValidateGamePath(string location)
        {
            return CheckValidInstallation(location);
        }

        public static bool ValidateConfig()
        {
            return CheckValidConfig();
        }
    }
}
