using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace LauncherManagement
{
    public class FileHandler
    {
        readonly SettingsHandler _settings = new();
        readonly AdditionalSettingsHandler _additionalSettings = new();

        public async Task GenerateMissingFiles()
        {
            List<DatabaseProperties.AdditionalSettings> properties = await _additionalSettings.GetSettings();

            string path = Path.Join(await _settings.GetGameLocationAsync(), "options.cfg");

            new FileInfo(path).Directory.Create();

            if (!File.Exists(path))
            {
                string lastCategory = "";
                StringBuilder sb = new();
                foreach (DatabaseProperties.AdditionalSettings property in properties)
                {
                    if (property.Category != lastCategory)
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

        public async Task<List<GameSettingsProperty>> ParseOptionsCfg()
        {
            string[] lines = await File.ReadAllLinesAsync(Path.Join(
                await _settings.GetGameLocationAsync(), "options.cfg"));

            List<GameSettingsProperty> properties = new();

            string currentCategory = "";

            foreach (string line in lines)
            {
                string key = "";
                string value = "";

                if (line.Contains("["))
                {
                    currentCategory = line.Split("[")[1].Split("]")[0];
                }

                if (line.Contains("="))
                {
                    key = line.Split("=")[0];
                    value = line.Split("=")[1];

                    GameSettingsProperty property = new()
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

        public async Task SaveOptionsCfg(List<GameSettingsProperty> properties)
        {
            string path = Path.Join(await _settings.GetGameLocationAsync(), "options.cfg");

            StringBuilder sb = new();
            List<string> lines = new();
            string allText = "";
            string lastCategory = "";

            try
            {
                sb.AppendLine("# options.cfg - Please do not edit this auto-generated file.");

                foreach (GameSettingsProperty property in properties)
                {
                    if (property.Category != lastCategory)
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
    }
}
