using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

            try
            {
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
            }
            catch (Exception e)
            {
                await LogHandler.Log(LogType.ERROR, "| GenerateManifestAsync |" + e.Message);
            }

            string output = "";

            try
            {
                output = JsonConvert.SerializeObject(listOfFiles, Formatting.Indented);
            }
            catch (Exception e)
            {
                await LogHandler.Log(LogType.ERROR, "| GenerateManifestAsync |" + e.Message);
            }

            try
            {
                await File.WriteAllTextAsync(Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "required.json"), output);
            }
            catch (Exception e)
            {
                await LogHandler.Log(LogType.ERROR, "| GenerateManifestAsync |" + e.Message);
            }
        }

        static async Task ParseLiveCfg(string file)
        {
            List<string> treFiles = new();

            try
            {
                string[] lines = await File.ReadAllLinesAsync(file);

                foreach (string line in lines)
                {
                    if (line.Contains("searchTree"))
                    {
                        string treFile = line.Split("=")[1];
                        treFiles.Add(treFile);
                    }
                }
            }
            catch (Exception e)
            {
                await LogHandler.Log(LogType.ERROR, "| ParseLiveCfg |" + e.Message);
            }

            string json = "";

            try
            {
                json = JsonConvert.SerializeObject(treFiles, Formatting.Indented);
            }
            catch (Exception e)
            {
                await LogHandler.Log(LogType.ERROR, "| ParseLiveCfg |" + e.Message);
            }

            try
            {
                await File.WriteAllTextAsync(Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "livecfg.json"), json);
            }
            catch (Exception e)
            {
                await LogHandler.Log(LogType.ERROR, "| ParseLiveCfg |" + e.Message);
            }
        }
    }
}
