using System.Security.Cryptography;
using System.Text.Json;
using LibLauncherUtil.Properties;

namespace LibLauncherUtil.Util;

public static class ManifestGenerator
{
    public static async Task GenerateManifestAsync(string generateFromFolder)
    {
        var versionFilePath = Path.Join(generateFromFolder, "version.json");

        // Set to 0 for increment to 1
        // This will ensure version is set to 1 if version file does not exist
        var currentVersion = 0;

        if (File.Exists(versionFilePath))
        {
            var versionFile = JsonSerializer.Deserialize<VersionFile>(await File.ReadAllTextAsync(versionFilePath));
            currentVersion = versionFile.Version;
        }

        await File.WriteAllTextAsync(Path.Join(generateFromFolder, "version.json"), JsonSerializer.Serialize(new VersionFile
        {
            Version = currentVersion + 1
        },
        new JsonSerializerOptions
        {
            WriteIndented = true
        }));

        var files = Directory.GetFiles(generateFromFolder, "*.*", SearchOption.AllDirectories);
        List<DownloadableFile> listOfFiles = new();

        foreach (var file in files)
        {
            var splitFile = file.Split(generateFromFolder + "\\")[1].Replace("\\", "/");

            DownloadableFile dFile = new()
            {
                Name = splitFile,
                Size = new FileInfo(file).Length
            };

            using (var md5 = MD5.Create())
            {
                await using var stream = File.OpenRead(file);

                dFile.Md5 = await Task.Run(() => BitConverter.ToString(md5.ComputeHash(stream))
                    .Replace("-", "").ToLowerInvariant());
            }

            listOfFiles.Add(dFile);
        }

        var saveDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

        await File.WriteAllTextAsync(Path.Join(saveDirectory, "required.json"), JsonSerializer.Serialize(listOfFiles, new JsonSerializerOptions
        {
            WriteIndented = true
        }));
    }

    private static async Task ParseLiveCfg(string file)
    {
        var lines = await File.ReadAllLinesAsync(file);

        var treFiles = (from line in lines where line.Contains("searchTree") select line.Split("=")[1]).ToList();

        var json = JsonSerializer.Serialize(treFiles, new JsonSerializerOptions { WriteIndented = true });

        await File.WriteAllTextAsync(Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "livecfg.json"), json);
    }

}
