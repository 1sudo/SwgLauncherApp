using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using LauncherApp.Models.Properties;

namespace LauncherApp.Models.Util;

public static class ManifestGenerator
{
    public static async Task GenerateManifestAsync(string generateFromFolder)
    {
        var files = Directory.GetFiles(generateFromFolder, "*.*", SearchOption.AllDirectories);
        List<DownloadableFile> listOfFiles = new();

        foreach (var file in files)
        {
            var splitFile = file.Split(generateFromFolder + "\\")[1].Replace("\\", "/");

            DownloadableFile dFile = new();
            dFile.Name = splitFile;

            dFile.Size = new FileInfo(file).Length;

            using var md5 = MD5.Create();
            
            await using var stream = File.OpenRead(file);

            dFile.Md5 = await Task.Run(() => BitConverter.ToString(md5.ComputeHash(stream))
                .Replace("-", "").ToLowerInvariant());

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

        var output = JsonSerializer.Serialize(listOfFiles, options);

        await File.WriteAllTextAsync(Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "required.json"), output);
    }

    private static async Task ParseLiveCfg(string file)
    {
        var lines = await File.ReadAllLinesAsync(file);

        var treFiles = (from line in lines where line.Contains("searchTree") select line.Split("=")[1]).ToList();

        var json = JsonSerializer.Serialize(treFiles, new JsonSerializerOptions { WriteIndented = true });

        await File.WriteAllTextAsync(Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "livecfg.json"), json);
    }

}
