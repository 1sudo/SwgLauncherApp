using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace LauncherManagement
{
    public class ValidationHandler : DownloadHandler
    {
        internal static async Task<Dictionary<string, string>> GetBadFiles(List<DownloadableFile> fileList, bool isFullScan = false)
        {
            var newFileList = new Dictionary<string, string>();

            foreach (var file in fileList)
            {
                if (isFullScan)
                {
                    try
                    {
                        using (var md5 = MD5.Create())
                        {
                            using (var stream = File.OpenRead(file.name))
                            {
                                var result = await Task.Run(() => System.BitConverter.ToString(md5.ComputeHash(stream))
                                    .Replace("-", "").ToLowerInvariant());

                                // If checksum doesn't match, add to download list
                                if (result != file.md5)
                                {
                                    newFileList.Add(file.name, file.url);
                                }
                            }
                        }
                    }
                    // If file doesn't exist, add to download list
                    catch
                    {
                        newFileList.Add(file.name, file.url);
                    }
                }
                else
                {
                    try
                    {
                        // If file is wrong size, add to download list
                        if (new FileInfo(file.name).Length != file.size)
                        {
                            newFileList.Add(file.name, file.url);
                        }
                    }
                    // If file doesn't exist, add to download list
                    catch
                    {
                        newFileList.Add(file.name, file.url);
                    }
                }
            }

            return newFileList;
        }

        internal static (bool flag, string reason) CheckValidInstallation(string location)
        {
            if (!Directory.Exists(location))
            {
                return (false, "Missing SWG directory! Please select a valid SWG installation location!");
            }

            // Files that are required to exist
            List<string> filesToCheck = new List<string> {
                "dpvs.dll",
                "Mss32.dll",
                "dbghelp.dll"
            };

            // Files in supposed SWG directory
            string[] files = Directory.GetFiles(location);

            int numRequiredFiles = 0;

            foreach (string fileToCheck in filesToCheck)
            {
                foreach (string file in files)
                {
                    if (fileToCheck == file.Split("\\")[1].Trim())
                    {
                        numRequiredFiles++;
                    }
                }
            }

            if (numRequiredFiles == 3)
            {
                return (true, "");
            }

            return (false, "Invalid SWG installation directory! Please select a directory with valid SWG game files!");
        }
    }
}
