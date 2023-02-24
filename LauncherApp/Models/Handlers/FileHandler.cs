using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using LauncherApp.Models.Properties;

namespace LauncherApp.Models.Handlers;

public class FileHandler
{
    public static Action<string, double, double>? OnFullScanFileCheck { get; set; }
    public static Action<string>? OnInstallCheckFailed { get; set; }
    public static string? BaseGameLocation { get; set; }
    static readonly List<string> ClientChecksums = new()
    {
        "a487bcf7abe27ba9c02e3121ba44367e", // 30 FPS
        "50692684e090b200ea28681e7ae7da5b", // 60 FPS
        "2a55323f8774c43231331cb00014a011", // 144 FPS
        "38feda8e17042a5bc9edf7d9959bdbfe"  // 240 FPS
    };

    public async static Task<bool> CheckBaseInstallation(string location)
    {
        try
        {
            if (!Directory.Exists(location))
            {
                return false;
            }

            // Files that are required to exist
            List<string> filesToCheck = new()
            {
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
        }
        catch (Exception e)
        {
            await LogHandler.Log(LogType.ERROR, "| CheckBaseInstallation | " + e.Message.ToString());
            OnInstallCheckFailed?.Invoke(e.Message.ToString());
        }

        return false;
    }

    public static async Task<bool> UpdateIsAvailable()
    {
        ConfigFile? config = ConfigFile.GetConfig();

        string? gameLocation = config?.Servers?[config.ActiveServer].GameLocation;

        var versionFilePath = Path.Join(gameLocation, "version.json");

        // There is no version, force update
        if (!File.Exists(versionFilePath)) return true;

        var versionFile = JsonSerializer.Deserialize<VersionFile>(await File.ReadAllTextAsync(versionFilePath));
        var remoteVersionFile = await HttpHandler.DownloadVersionAsync();

#pragma warning disable IDE0059 // Unnecessary assignment of a value
        List<DownloadableFile> downloadableFiles = new();
#pragma warning restore IDE0059 // Unnecessary assignment of a value

        downloadableFiles = await HttpHandler.DownloadManifestAsync();

        var fileList = await GetBadFilesAsync(gameLocation ?? "", downloadableFiles);

        return (versionFile.Version != remoteVersionFile.Version) || (fileList.Count > 0);
    }

    internal static async Task<List<string>> GetBadFilesAsync(string downloadLocation, List<DownloadableFile> fileList, bool isFullScan = false)
    {
        List<string> newFileList = new();

        double listLength = fileList.Count;

        double i = 1;
        await Task.Run(() =>
        {
            foreach (DownloadableFile file in fileList)
            {
                if (isFullScan)
                {
                    try
                    {
                        if (File.Exists(Path.Join(downloadLocation, file.Name)))
                        {
                            OnFullScanFileCheck?.Invoke($"Checking File {file.Name}...", i, listLength);

                            string result = GetMd5Checksum(Path.Join(downloadLocation, file.Name));

                            // If checksum doesn't match, add to download list
                            if (result != file.Md5)
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

                            // Check MD5 sums for game client regardless of full scan or file size check
                            // This ensures the executable doesn't get re-downloaded when it is patched
                            // but stays the same size in bytes (FPS edits, for example)
                            if (file.Name is "SWGEmu.exe" or "SwgClient_r.exe")
                            {
                                // Calculate MD5 checksum
                                string result = GetMd5Checksum(Path.Join(downloadLocation, file.Name));

                                bool fileNeedsAdded = true;
                                ClientChecksums.ForEach(checksum =>
                                {
                                    // If MD5 checksum doesn't match the manifest, or the hardcoded patched sums, add to list
                                    if (result == file.Md5 || result == checksum)
                                    {
                                        fileNeedsAdded = false;
                                    }
                                });

                                if (fileNeedsAdded)
                                {
                                    newFileList.Add(file.Name);
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
                }
            }
        });

        return newFileList;
    }

    internal static string GetMd5Checksum(string filePath)
    {
        using MD5 md5 = MD5.Create();
        using FileStream stream = File.OpenRead(filePath);

        return BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
    }

    public static async Task CheckFilesAsync(bool isFullScan = false, string modName = "", bool isTreMod = false, bool isDirChange = false, string previousDir = "")
    {
        ConfigFile? config = ConfigFile.GetConfig();

        string? manifestFilePath = config!.Servers![config.ActiveServer].ManifestFilePath;

        List<DownloadableFile> downloadableFiles = new();

        long totalDownloadSize = 0;

        if (string.IsNullOrEmpty(modName) && manifestFilePath is not null)
        {
            downloadableFiles = await HttpHandler.DownloadManifestAsync();
        }
        else
        {
            if (manifestFilePath is not null)
            {
                downloadableFiles = await HttpHandler.DownloadManifestAsync();
            }

            if (isTreMod)
            {
                List<string> downloadableFileList = new();

                foreach (DownloadableFile file in downloadableFiles)
                {
                    Trace.WriteLine($"Added file: {file.Name}");
                    downloadableFileList.Add(file.Name);
                }

                config.Servers![config.ActiveServer].TreMods!.Add(new TreModProperties()
                {
                    ModName = modName,
                    FileList = downloadableFileList
                });
            }
        }

        List<string> fileList;
        if (isFullScan)
        {
            fileList = await Task.Run(() => GetBadFilesAsync(config.Servers![config.ActiveServer].GameLocation!, downloadableFiles, true));
        }
        else
        {
            fileList = await Task.Run(() => GetBadFilesAsync(config.Servers![config.ActiveServer].GameLocation!, downloadableFiles));

            if (isDirChange)
            {
                await Task.Run(() => AttemptCopyFilesFromListAsync(fileList, config.Servers![config.ActiveServer].GameLocation!, true, previousDir));
            }
            else
            {
                await Task.Run(() => AttemptCopyFilesFromListAsync(fileList, config.Servers![config.ActiveServer].GameLocation!));
            }

            fileList = await Task.Run(() => GetBadFilesAsync(config.Servers![config.ActiveServer].GameLocation!, downloadableFiles));
        }

        // Calculate total download size based on 
        // what files need to be downloaded
        fileList.ForEach(file =>
        {
            downloadableFiles.ForEach(downloadableFile =>
            {
                if (file == downloadableFile.Name)
                {
                    totalDownloadSize += downloadableFile.Size;
                }
            });
        });

        await HttpHandler.DownloadFilesFromListAsync(fileList, config.Servers![config.ActiveServer].GameLocation!, totalDownloadSize);
    }

    public static async Task AttemptCopyFilesFromListAsync(List<string> fileList, string copyLocation, bool isDirChange = false, string previousDir = "")
    {
        double listLength = fileList.Count;
        List<string> newFileList = new();

        double i = 1;
        // Key == name, Value == url
        foreach (string file in fileList)
        {
            // Notify UI of filename
            HttpHandler.OnCurrentFileDownloading?.Invoke("copy", file, i, listLength);

            // Create directory before writing to file if it doesn't exist
            if (copyLocation is not null && file is not null)
            {
                new FileInfo(Path.Join(copyLocation, file)).Directory!.Create();
            }

            if (isDirChange)
            {
                BaseGameLocation = previousDir;
            }

            if (copyLocation != BaseGameLocation)
            {
                // If file exists at source installation, copy it
                if (File.Exists(Path.Join(BaseGameLocation, file)))
                {
                    await CopyFileAsync(Path.Join(BaseGameLocation, file), Path.Join(copyLocation, file));
                }
                // If file doesn't exist in source location, add to new list to be returned
                else
                {
                    if (file is not null)
                    {
                        newFileList.Add(file);
                    }
                }
            }

            ++i;
        }
    }

    static async Task CopyFileAsync(string sourcePath, string destinationPath)
    {
        using Stream source = File.OpenRead(sourcePath);
        using Stream destination = File.Create(destinationPath);
        await source.CopyToAsync(destination);
    }

    public async static Task GenerateMissingFiles(ConfigFile? config)
    {
        List<AdditionalSettingProperties>? properties = config!.Servers![config.ActiveServer].AdditionalSettings;

        string path = Path.Join(config!.Servers![config.ActiveServer].GameLocation, "options.cfg");

        new FileInfo(path).Directory!.Create();

        if (!File.Exists(path))
        {
            string lastCategory = "";
            StringBuilder sb = new();
            if (properties is not null)
            {
                foreach (AdditionalSettingProperties property in properties)
                {
                    if (property.Category is not null && property.Category != lastCategory)
                    {
                        lastCategory = property.Category;
                        sb.AppendLine($"\n[{property.Category}]");
                        sb.AppendLine($"\t{property.Key}={property.Value}");
                    }
                    else
                    {
                        sb.AppendLine($"\t{property.Key}={property.Value}");
                    }
                }
            }

            try
            {
                await File.WriteAllTextAsync(path, sb.ToString());
            }
            catch (Exception e)
            {
                await LogHandler.Log(LogType.ERROR, "| GenerateMissingFiles |" + e.Message);
            }
        }
    }

    public static async Task<List<AdditionalSettingProperties>> ParseOptionsCfg(ConfigFile? config)
    {
        string[] lines = await File.ReadAllLinesAsync(Path.Join(config!.Servers![config.ActiveServer].GameLocation, "options.cfg"));

        List<AdditionalSettingProperties> properties = new();

        string currentCategory = "";

        foreach (string line in lines)
        {
            string key = "";
            string value = "";

            if (line.Contains('['))
            {
                currentCategory = line.Split('[')[1].Split(']')[0];
            }

            if (line.Contains('='))
            {
                key = line.Split('=')[0];
                value = line.Split('=')[1];

                AdditionalSettingProperties property = new()
                {
                    Category = currentCategory,
                    Key = key,
                    Value = value
                };

                properties.Add(property);
            }
        }

        return properties;
    }

    public static async Task SaveOptionsCfg(ConfigFile? config, List<AdditionalSettingProperties> properties)
    {
        string path = Path.Join(config!.Servers![config.ActiveServer].GameLocation, "options.cfg");

        StringBuilder sb = new();
        List<string> lines = new();
        string allText = "";
        string lastCategory = "";

        try
        {
            sb.AppendLine("# options.cfg - Please do not edit this auto-generated file.");

            foreach (AdditionalSettingProperties property in properties)
            {
                if (property.Category is not null && property.Category != lastCategory)
                {
                    lastCategory = property.Category;
                    lines.Add($"\n[{property.Category}]");
                    lines.Add($"\t{property.Key}={property.Value}");
                }
                else
                {
                    lines.Add($"\t{property.Key}={property.Value}");
                }
            }

            foreach (string line in lines)
            {
                allText += line;
            }

            foreach (string line in lines)
            {
                if (line.Contains("ClientGraphics"))
                {
                    if (!allText.Contains("useHardwareMouseCursor"))
                        sb.AppendLine(line + "\n\tuseHardwareMouseCursor=1");

                    if (allText.Contains("useSafeRenderer=1"))
                        sb.AppendLine(line + "\n\trasterMajor=5");
                    else if (allText.Contains("useSafeRenderer=0"))
                        sb.AppendLine(line + "\n\trasterMajor=7");
                    else
                        sb.AppendLine(line + "\n\trasterMajor=7");
                }

                if (line.Contains("SharedUtility"))
                {
                    if (!allText.Contains("cache="))
                        sb.AppendLine(line + "\n\tcache = \"misc/cache_large.iff\"");
                }

                if (line.Contains("ClientAudio"))
                {
                    sb.AppendLine(line + "\n\tsoundProvider = \"Windows Speaker Configuration\"");
                }

                sb.AppendLine(line);
            }

            if (!allText.Contains("ClientAudio"))
            {
                sb.AppendLine("\n[ClientAudio]");
                sb.AppendLine("\tsoundProvider = \"Windows Speaker Configuration\"");
            }

            if (!allText.Contains("SharedUtility"))
            {
                sb.AppendLine("\n[SharedUtility]");
                sb.AppendLine("\tcache = \"misc/cache_large.iff\"");
            }

            await File.WriteAllTextAsync(path, sb.ToString());
        }
        catch (Exception e)
        {
            await LogHandler.Log(LogType.ERROR, "| SaveOptionsCfg |" + e.Message);
        }
    }

    public static async Task SaveDeveloperOptionsCfg(ConfigFile? config)
    {
        using StreamWriter sw = new(Path.Join(config!.Servers![config.ActiveServer].GameLocation, "swgemu_login.cfg"));

        await sw.WriteAsync("[ClientGame]\n" +
            $"loginServerAddress0={config.Servers[config.ActiveServer].SWGLoginHost}\n" +
            $"loginServerPort0={config.Servers[config.ActiveServer].SWGLoginPort}\n" +
            $"freeChaseCameraMaximumZoom={config.Servers[config.ActiveServer].MaxZoom}\n");

        using StreamWriter sw2 = new(Path.Join(config!.Servers![config.ActiveServer].GameLocation, "launcher.cfg"));

        await sw2.WriteAsync("[SwgClient]\n" +
            "\tallowMultipleInstances=true\n\n" +
            "[ClientGame]\n" +
            $"\t0fd345d9={config.Servers[config.ActiveServer].Admin}\n\n" +
            "[ClientUserInterface]\n" +
            $"\tdebugExamine={config.Servers[config.ActiveServer].DebugExamine}");
    }
}
