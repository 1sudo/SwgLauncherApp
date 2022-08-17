using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using LauncherApp.Models.Properties;

namespace LauncherApp.Models.Handlers;

public class AppHandler
{
    static readonly bool _cuClient = false;

    public static async Task StartGameAsync(ConfigFile? config, string password = "",
        string username = "", string charactername = "", bool autoEnterZone = false)
    {
        // Run Async to prevent launcher from locking up when starting game and writing bytes
        await Task.Run(async () =>
        {
            try
            {
                bool configWritten = await WriteLoginConfigAsync(config);
                await WriteLauncherConfigAsync(config);
                await WriteLiveConfigAsync(config);

                string? gameLocation = config!.Servers![config.ActiveServer].GameLocation;

                if (configWritten)
                {
                    if (!_cuClient)
                    {
                        // Read SWGEmu.exe and seek to 0x1153
                        string exePath = Path.Join(gameLocation, "SWGEmu.exe");
                        FileStream rs = File.OpenRead(exePath);
                        rs.Seek(0x1153, SeekOrigin.Begin);

                        // Convert FPS integer (casted to float) to hex
                        string hexSelection = BitConverter.ToString(
                            BitConverter.GetBytes((float)config!.Servers![config.ActiveServer].Fps)).Replace("-", "");

                        // Read the next 3 bytes to ensure we're at the right position and 
                        // the binary hasn't been altered
                        if (rs.ReadByte() == 199 && rs.ReadByte() == 69 && rs.ReadByte() == 148)
                        {
                            string hex = "";

                            // Get hex of next 4 bytes at 0x1153 (float)
                            for (int i = 0; i <= 3; i++)
                            {
                                byte[] b = { (byte)rs.ReadByte() };
                                hex += BitConverter.ToString(b).Replace("-", "");
                            }

                            // Close Read to prepare for write
                            rs.Dispose();

                            // If the selected FPS already matches hex value in the 
                            // binary, don't write to it again (faster loading)
                            if (hex != hexSelection)
                            {
                                using FileStream ws = File.OpenWrite(exePath);
                                ws.Seek(0x1156, SeekOrigin.Begin);

                                // Create byte array of FPS value
                                byte[] bytes = BitConverter.GetBytes((float)config!.Servers![config.ActiveServer].Fps);

                                // Write FPS float at 0x1156
                                foreach (byte b in bytes)
                                {
                                    ws.WriteByte(b);
                                }
                            }
                        }

                        var startInfo = new ProcessStartInfo();

                        if (autoEnterZone)
                        {
                            startInfo.Arguments = $"-- -s ClientGame loginClientPassword={password} autoConnectToLoginServer=1 loginClientID={username} avatarName={charactername} autoConnectToGameServer=1 -s Station -s SwgClient allowMultipleInstances=true";
                        }
                        else
                        {
                            startInfo.Arguments = $"-- -s ClientGame loginClientPassword={password} autoConnectToLoginServer=1 loginClientID={username} autoConnectToGameServer=0 -s Station -s SwgClient allowMultipleInstances=true";
                        }

                        startInfo.EnvironmentVariables["SWGCLIENT_MEMORY_SIZE_MB"] = config!.Servers![config.ActiveServer].Ram.ToString();
                        startInfo.UseShellExecute = false;
                        startInfo.WorkingDirectory = gameLocation;
                        startInfo.FileName = Path.Join(gameLocation, "SWGEmu.exe");

                        Process.Start(startInfo);
                    }
                    else
                    {
                        string exePath = Path.Join(gameLocation, "SwgClient_r.exe");

                        ProcessStartInfo startInfo = new();
                        startInfo.EnvironmentVariables["SWGCLIENT_MEMORY_SIZE_MB"] = config!.Servers![config.ActiveServer].Ram.ToString();
                        startInfo.UseShellExecute = false;
                        startInfo.WorkingDirectory = gameLocation;
                        startInfo.FileName = Path.Join(gameLocation, "SwgClient_r.exe");

                        Process.Start(startInfo);
                    }
                }
                else
                {
                    await LogHandler.Log(LogType.ERROR, "Error writing to login.cfg!");
                }
            }
            catch (Exception e)
            {
                await LogHandler.Log(LogType.ERROR, "| StartGameAsync | " + e.Message.ToString());
            }
        });
    }

    public static async void WriteMissingConfigs(string gameLocation)
    {
        string[] configs =
        {
            "user.cfg",
            "launcher.cfg"
        };

        foreach (string config in configs)
        {
            string filePath = Path.Join(gameLocation, config);
            if (!File.Exists(filePath))
            {
                try
                {
                    StreamWriter sw = new(filePath);
                    sw.Write("");
                }
                catch (Exception e)
                {
                    await LogHandler.Log(LogType.ERROR, "| WriteMissingConfigs | " + e.Message.ToString());
                }
            }
        }
    }

    public static async Task<bool> WriteConfigAsync(ConfigFile? config, string file, string text)
    {
        string? gameLocation = config!.Servers![config.ActiveServer].GameLocation;

        if (!string.IsNullOrEmpty(gameLocation))
        {
            string cfg = "";

            switch (file)
            {
                case "login": cfg = _cuClient ? "login.cfg" : "swgemu_login.cfg"; break;
                case "live": cfg = _cuClient ? "live.cfg" : "swgemu_live.cfg"; break;
                case "launcher": cfg = "launcher.cfg"; break;
                default:
                    break;
            }

            string filePath = Path.Join(gameLocation, cfg);

            try
            {
                new FileInfo(filePath).Directory!.Create();
            }
            catch (Exception e)
            {
                await LogHandler.Log(LogType.ERROR, "| WriteConfigAsync | " + e.Message.ToString());
            }

            try
            {
                using StreamWriter sw = new(filePath);
                await sw.WriteAsync(text);

                return true;
            }
            catch (Exception e)
            {
                await LogHandler.Log(LogType.ERROR, "| WriteConfigAsync | " + e.Message.ToString());
            }
        }

        return false;
    }

    static async Task WriteLauncherConfigAsync(ConfigFile? config)
    {
        bool admin = config!.Servers![config.ActiveServer].Admin;
        bool debugExamine = config.Servers[config.ActiveServer].DebugExamine;

        string cfgText = $"[SwgClient]\n" +
            "\tallowMultipleInstances=true\n\n" +
            "[ClientGame]\n" +
            $"\t0fd345d9={admin.ToString().ToLower()}\n\n" +
            "[ClientUserInterface]\n" +
            $"\tdebugExamine={debugExamine.ToString().ToLower()}";

        await WriteConfigAsync(config, "launcher", cfgText);
    }

    static async Task WriteLiveConfigAsync(ConfigFile? config)
    {
        List<string> treList = await DownloadHandler.DownloadTreList();

        StringBuilder sb = new();

        string header = "[SharedFile]\n" +
            "\tmaxSearchPriority=99\n";

        sb.Append(header);

        List<TreModProperties>? modList = config!.Servers![config.ActiveServer].TreMods;

        int count = treList.Count;
        int modFileCount = 0;

        foreach (TreModProperties mod in modList!)
        {
            modFileCount += mod.FileList!.Count;
        }

        modFileCount += count;

        foreach (TreModProperties mod in modList)
        {
            mod.FileList!.Reverse();

            foreach (string treFile in mod.FileList)
            {
                sb.Append($"\tsearchTree_00_{modFileCount}={treFile}\n");
                modFileCount -= 1;
            }
        }

        foreach (string treFile in treList)
        {
            sb.Append($"\tsearchTree_00_{count}={treFile}\n");
            count -= 1;
        }

        string footer = "\n[SharedNetwork]\n" +
            "\tnetworkHandlerDispatchThrottle=true\n\n" +
            "[ClientUserInterface]\n" +
            "\tmessageOfTheDayTable=live_motd\n\n" +
            "[SwgClientUserInterface/SwgCuiService]\n" +
            "\tknownIssuesArticle=10424\n\n" +
            "[Station]\n" +
            "\tsubscriptionFeatures=1\n" +
            "\tgameFeatures=65535\n";

        sb.Append(footer);

        await WriteConfigAsync(config, "live", sb.ToString());
    }

    static async Task<bool> WriteLoginConfigAsync(ConfigFile? config)
    {
        string? host = config!.Servers![config.ActiveServer].SWGLoginHost;
        int port = config.Servers[config.ActiveServer].SWGLoginPort;
        int maxZoom = config.Servers[config.ActiveServer].MaxZoom;

        string cfgText = $"[ClientGame]\n" +
            $"loginServerAddress0={host}\n" +
            $"loginServerPort0={port}\n" +
            $"freeChaseCameraMaximumZoom={maxZoom}";

        return await WriteConfigAsync(config, "login", cfgText);
    }

    public async static void StartGameConfig(string serverPath)
    {
        try
        {
            ProcessStartInfo startInfo = new();

            if (!_cuClient)
            {
                startInfo.UseShellExecute = true;
                startInfo.WorkingDirectory = serverPath;
                startInfo.FileName = Path.Join(serverPath, "SWGEmu_Setup.exe");
            }
            else
            {
                startInfo.UseShellExecute = true;
                startInfo.WorkingDirectory = serverPath;
                startInfo.FileName = Path.Join(serverPath, "SwgClientSetup_r.exe");
            }

            Process.Start(startInfo);
        }
        catch (Exception e)
        {
            await LogHandler.Log(LogType.ERROR, "| StartGameConfig | " + e.Message.ToString());
        }
    }

    public static async void OpenDefaultBrowser(string url)
    {
        Process myProcess = new();

        try
        {
            myProcess.StartInfo.UseShellExecute = true;
            myProcess.StartInfo.FileName = url;
            myProcess.Start();
        }
        catch (Exception e)
        {
            await LogHandler.Log(LogType.ERROR, "| OpenDefaultBrowser | " + e.Message.ToString());
        }
    }
}
