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
            List<DownloadableFile> listOfFiles = new();

            foreach (string file in files)
            {
                string splitFile = file.Split(generateFromFolder + "\\")[1].Replace("\\", "/");

                DownloadableFile dFile = new();
                dFile.Name = splitFile;

                dFile.Size = new FileInfo(file).Length;

                using (MD5 md5 = MD5.Create())
                {
                    using FileStream stream = File.OpenRead(file);
                    
                    dFile.Md5 = await Task.Run(() => BitConverter.ToString(md5.ComputeHash(stream))
                        .Replace("-", "").ToLowerInvariant());
                }

                listOfFiles.Add(dFile);
            }

            string output = JsonConvert.SerializeObject(listOfFiles, Formatting.Indented);

            await File.WriteAllTextAsync(Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "required.json"), output);
        }
    }
}
