using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using LauncherApp.Models.Properties;

namespace LauncherApp.Models.Handlers
{
    public class FileHandler
    {
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
}
