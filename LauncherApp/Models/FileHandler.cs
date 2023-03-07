using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using LibLauncherUtil.Properties;

namespace LauncherApp.Models;

public class FileHandler
{
    public static Action<string, int, int>? OnFullScanFileCheck { get; set; }
    public static Action? OnFullScanStarted { get; set; }
    public static Action? OnFullScanCompleted { get; set; }
    public static Action? UpdateCheckComplete { get; set; }
    public static Action<string>? OnInstallCheckFailed { get; set; }
    public static string? BaseGameLocation { get; set; }
    static readonly List<string> ClientChecksums = new()
    {
        "a487bcf7abe27ba9c02e3121ba44367e", // 30 FPS
        "50692684e090b200ea28681e7ae7da5b", // 60 FPS
        "2a55323f8774c43231331cb00014a011", // 144 FPS
        "38feda8e17042a5bc9edf7d9959bdbfe"  // 240 FPS
    };

    public static bool CheckBaseInstallation(string location)
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
            Logger.Instance.Log(e, ERROR);
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

        List<DownloadableFile> downloadableFiles;

        downloadableFiles = await HttpHandler.DownloadManifestAsync();

        var fileList = await GetBadFilesAsync(gameLocation ?? "", downloadableFiles);

        UpdateCheckComplete?.Invoke();

        return versionFile.Version != remoteVersionFile.Version || fileList.Count > 0;
    }

    internal static async Task<List<string>> GetBadFilesAsync(string downloadLocation, List<DownloadableFile> fileList, bool isFullScan = false)
    {
        List<string> newFileList = new();

        int listLength = fileList.Count;

        int i = 1;
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
                    catch (Exception e)
                    {
                        newFileList.Add(file.Name);
                        Logger.Instance.Log(e, ERROR);
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
                    catch (Exception e)
                    {
                        newFileList.Add(file.Name);
                        Logger.Instance.Log(e, ERROR);
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

    public static async Task CheckFilesAsync(bool isFullScan = false)
    {
        OnFullScanStarted?.Invoke();

        var config = ConfigFile.GetCurrentServer();

        if (config is null) return;

        long totalDownloadSize = 0;
        List<DownloadableFile> downloadableFiles = new();

        downloadableFiles = await HttpHandler.DownloadManifestAsync();

        List<string> fileList;
        if (isFullScan)
        {
            fileList = await GetBadFilesAsync(config.GameLocation!, downloadableFiles, true);
            OnFullScanCompleted?.Invoke();
        }
        else
        {
            fileList = await GetBadFilesAsync(config.GameLocation!, downloadableFiles);
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

        await HttpHandler.DownloadFilesFromListAsync(fileList, config.GameLocation!, totalDownloadSize);
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
                Logger.Instance.Log(e, ERROR);
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
        StringBuilder sb = new();

        List<string> propertyHeadings = new()
        {
            "ClientGraphics",
            "Direct3d9",
            "ClientGame",
            "ClientUserInterface",
            "ClientAudio",
            "ClientSkeletalAnimation",
            "ClientTextureRenderer",
            "SharedUtility",
            "ClientObject/DetailAppearanceTemplate",
            "SharedFile",
            "ClientTerrain",
            "ClientUserInterface"
        };

        sb.AppendLine("# options.cfg - Please do not edit this auto-generated file.");

        propertyHeadings.ForEach(heading =>
        {
            if (properties.Where(property => property.Category == heading).Any() || heading == "SharedUtility" || heading == "ClientAudio")
            {
                sb.AppendLine($"\n[{heading}]");
            }

            foreach (var property in properties)
            {
                if (property.Category is not null && property.Category == heading)
                {
                    sb.AppendLine($"\t{property.Key}={property.Value}");
                }
            }

            if (heading == "ClientGraphics")
            {
                if (!properties.Any(line => line.Key == "useHardwareMouseCursor"))
                {
                    sb.AppendLine("\tuseHardwareMouseCursor=1");
                }

                if (properties.Any(line => line.Key == "useSafeRenderer" && line.Value == "1"))
                {
                    sb.AppendLine("\trasterMajor=5");
                }
                else if (properties.Any(line => line.Key == "useSafeRenderer" && line.Value == "0"))
                {
                    sb.AppendLine("\trasterMajor=7");
                }
                else
                {
                    sb.AppendLine("\trasterMajor=7");
                }
            }

            if (heading == "SharedUtility")
            {
                if (!properties.Any(line => line.Key == "cache"))
                {
                    sb.AppendLine("\tcache = \"misc/cache_large.iff\"");
                }
            }

            if (heading == "ClientAudio")
            {
                sb.AppendLine("\tsoundProvider = \"Windows Speaker Configuration\"");
            }
        });

        try
        {
            await File.WriteAllTextAsync(Path.Join(config!.Servers![config.ActiveServer].GameLocation, "options.cfg"), sb.ToString());
        }
        catch (Exception e)
        {
            Logger.Instance.Log(e, ERROR);
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
