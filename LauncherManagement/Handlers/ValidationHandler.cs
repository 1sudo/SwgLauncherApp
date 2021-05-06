using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

namespace LauncherManagement
{
    public class ValidationHandler : DownloadHandler
    {
        internal static async Task<List<string>> GetBadFilesAsync(string downloadLocation, List<DownloadableFile> fileList, bool isFullScan = false)
        {
            var newFileList = new List<string>();

            double listLength = fileList.Count;

            double i = 1;
            await Task.Run(() =>
            {
                foreach (var file in fileList)
                {
                    if (isFullScan)
                    {
                        try
                        {
                            if (File.Exists(Path.Join(downloadLocation, file.Name)))
                            {
                                OnFullScanFileCheck?.Invoke($"Checking File { file.Name }...", i, listLength);

                                using (var md5 = MD5.Create())
                                {
                                    using (var stream = File.OpenRead(Path.Join(downloadLocation, file.Name)))
                                    {
                                        var result =  System.BitConverter.ToString(md5.ComputeHash(stream))
                                            .Replace("-", "").ToLowerInvariant();

                                        // If checksum doesn't match, add to download list
                                        if (result != file.Md5)
                                        {
                                            newFileList.Add(file.Name);
                                        }
                                    }
                                }
                            }
                            // If file doesn't exist, add to download list
                            else
                            {
                                newFileList.Add(file.Name);
                            }
                        }
                        // Some other dumb shit happened, add file to list
                        catch
                        {
                            newFileList.Add(file.Name);
                        }

                        ++i;
                    }
                    else
                    {
                        try
                        {
                            if (File.Exists(Path.Join(downloadLocation, file.Name)))
                            {
                                // If file is wrong size, add to download list
                                if (new FileInfo(Path.Join(downloadLocation, file.Name)).Length != file.Size)
                                {
                                    newFileList.Add(file.Name);
                                }
                            }
                            // If file doesn't exist, add to download list
                            else
                            {
                                newFileList.Add(file.Name);
                            }
                        }
                        // Some other dumb shit happened, add file to list
                        catch
                        {
                            newFileList.Add(file.Name);
                        }
                    }
                }
            });

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

        internal static bool ValidateJson(string file)
        {
            List<string> keys = new List<string>();

            Dictionary<string, List<string>> JsonFileKeys = new Dictionary<string, List<string>>()
            {
                { "config.json", new List<string>()
                    {
                        "SWGLocation", "ServerLocation", "AutoLogin"
                    } 
                },
                { "account.json", new List<string>()
                    {
                        "Username", "Password"
                    }
                },
                { "character.json", new List<string>()
                    {
                        "Character"
                    }
                },
            };

            foreach (KeyValuePair<string, List<string>> kvp in JsonFileKeys)
            {
                if (kvp.Key == file)
                {
                    keys = kvp.Value;
                }
            }

            JObject json;

            try
            {
                json = JObject.Parse(File.ReadAllText(Path.Join(Directory.GetCurrentDirectory(), file)));
            }
            catch
            {
                return false;
            }

            int keysContained = 0;

            foreach (string key in keys)
            {
                if (json.ContainsKey(key))
                {
                    keysContained++;
                }
            }

            if (keysContained == keys.Count)
            {
                return true;
            }

            return false;
        }
    }
}
