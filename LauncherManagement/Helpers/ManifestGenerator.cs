using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace LauncherManagement
{
    public static class ManifestGenerator
    {
        public static async Task GenerateManifestAsync(string generateFromFolder)
        {
            string[] files = Directory.GetFiles(generateFromFolder, "*.*", SearchOption.AllDirectories);
            List<DownloadableFile> listOfFiles = new List<DownloadableFile>();

            foreach (string file in files)
            {
                string splitFile = file.Split(generateFromFolder + "\\")[1].Replace("\\", "/");

                DownloadableFile dFile = new DownloadableFile();
                dFile.Name = splitFile;

                dFile.Size = new FileInfo(file).Length;

                using (var md5 = MD5.Create())
                {
                    using (var stream = File.OpenRead(file))
                    {
                        dFile.Md5 = await Task.Run(() => BitConverter.ToString(md5.ComputeHash(stream))
                            .Replace("-", "").ToLowerInvariant());
                    }
                }

                dFile.Url = "http://www.launchpad2.net/SWGEmu/" + splitFile;

                listOfFiles.Add(dFile);
            }

            string output = JsonConvert.SerializeObject(listOfFiles, Formatting.Indented);

            await File.WriteAllTextAsync(Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "manifest.json"), output);
        }
    }
}
