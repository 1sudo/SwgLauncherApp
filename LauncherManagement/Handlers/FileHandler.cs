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
            List<DatabaseProperties.AdditionalSettings> settings = await _additionalSettings.GetSettings();

            string path = Path.Join(await _settings.GetGameLocationAsync(), "options.cfg");

            if (!File.Exists(path))
            {
                Trace.WriteLine("in the if not exists loop");
                string lastCategory = "";
                StringBuilder sb = new();
                foreach (DatabaseProperties.AdditionalSettings setting in settings)
                {
                    Trace.WriteLine(setting.Category);
                    if (setting.Category != lastCategory)
                    {
                        Trace.WriteLine("In the category doesn't exist if");
                        lastCategory = setting.Category;
                        sb.AppendLine($"\n[{setting.Category}]");
                        sb.AppendLine($"\t{setting.Key}={setting.Value}");
                    }
                    else
                    {
                        Trace.WriteLine("Else");
                        sb.AppendLine($"\t{setting.Key}={setting.Value}");
                    }
                }

                Trace.WriteLine("Write!!");
                await File.WriteAllTextAsync(path, sb.ToString());
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
    }
}
