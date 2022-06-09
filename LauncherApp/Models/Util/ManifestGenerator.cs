using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using LauncherApp.Models.Properties;

namespace LauncherApp.Models.Util
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

                if (file.Contains("swgemu_live.cfg"))
                {
                    await ParseLiveCfg(file);
                }
                else
                {
                    listOfFiles.Add(dFile);
                }
            }

            JsonSerializerOptions options = new();
            options.WriteIndented = true;

            string output = JsonSerializer.Serialize(listOfFiles, options);

            await File.WriteAllTextAsync(Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "required.json"), output);
        }

        static async Task ParseLiveCfg(string file)
        {
            List<string> treFiles = new();

            string[] lines = await File.ReadAllLinesAsync(file);

            foreach (string line in lines)
            {
                if (line.Contains("searchTree"))
                {
                    string treFile = line.Split("=")[1];
                    treFiles.Add(treFile);
                }
            }

            JsonSerializerOptions options = new();
            options.WriteIndented = true;

            string json = JsonSerializer.Serialize(treFiles, options);

            await File.WriteAllTextAsync(Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "livecfg.json"), json);
        }
    }
}
