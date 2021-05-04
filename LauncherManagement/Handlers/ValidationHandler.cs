using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

namespace LauncherManagement
{
    public class ValidationHandler : DownloadHandler
    {
        internal static async Task<Dictionary<string, string>> GetBadFilesAsync(string downloadLocation, List<DownloadableFile> fileList, bool isFullScan = false)
        {
            var newFileList = new Dictionary<string, string>();

            double listLength = fileList.Count;

            double i = 1;
            foreach (var file in fileList)
            {
                if (isFullScan)
                {
                    try
                    {
                        OnFullScanFileCheck?.Invoke($"Checking File { file.Name }...", i, listLength);

                        using (var md5 = MD5.Create())
                        {
                            using (var stream = File.OpenRead(Path.Join(downloadLocation, file.Name)))
                            {
                                var result = await Task.Run(() => System.BitConverter.ToString(md5.ComputeHash(stream))
                                    .Replace("-", "").ToLowerInvariant());

                                // If checksum doesn't match, add to download list
                                if (result != file.Md5)
                                {
                                    newFileList.Add(file.Name, file.Url);
                                }
                            }
                        }
                    }
                    // If file doesn't exist, add to download list
                    catch
                    {
                        newFileList.Add(file.Name, file.Url);
                    }
                    ++i;
                }
                else
                {
                    try
                    {
                        // If file is wrong size, add to download list
                        if (new FileInfo(Path.Join(downloadLocation, file.Name)).Length != file.Size)
                        {
                            newFileList.Add(file.Name, file.Url);
                        }
                    }
                    // If file doesn't exist, add to download list
                    catch
                    {
                        newFileList.Add(file.Name, file.Url);
                    }
                }
            }

            return newFileList;
        }

        internal static bool CheckValidInstallation(string location)
        {
            if (!Directory.Exists(location))
            {
                return false;
            }

            // Files that are required to exist
            List<string> filesToCheck = new List<string> {
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

            return false;
        }

        internal static bool CheckValidConfig()
        {
            JObject json = new JObject();

            string schemaJson = @"{
              'SWGLocation': 'location',
              'ServerLocation': 'location',
              'AutoLogin': false
            }";

            JSchema schema = JSchema.Parse(schemaJson);

            try
            {
                json = JObject.Parse(File.ReadAllText(Path.Join(Directory.GetCurrentDirectory(), "config.json")));
            }
            catch
            {
                return false;
            }
            
            bool validSchema = json.IsValid(schema);

            int keysContained = 0;

            if (json.ContainsKey("SWGLocation"))
            {
                keysContained++;
            }

            if (json.ContainsKey("ServerLocation"))
            {
                keysContained++;
            }

            if (json.ContainsKey("AutoLogin"))
            {
                keysContained++;
            }

            if (validSchema && keysContained >= 3)
            {
                return true;
            }

            return false;
        }
    }
}
