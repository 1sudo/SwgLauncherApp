using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace LauncherManagement
{
    public class FileHandler
    {
        readonly SettingsHandler _settings = new();

        public async Task GenerateMissingFiles()
        {
            string path = Path.Join(await _settings.GetGameLocationAsync(), "options.cfg");
            if (!File.Exists(path))
            {
                string defaultConfiguration = 
                    "[ClientGraphics]\n" +
                    "\tscreenWidth=1920\n" +
                    "\tscreenHeight=1080\n" +
                    "\tallowTearing=1\n" +
                    "\n" +
                    "[ClientGame]\n" +
                    "\tskipIntro=1\n" +
                    "\tpreloadWorldSnapshot=0\n" +
                    "\n" +
                    "[ClientSkeletalAnimation]\n" +
                    "\tlodManagerEnable=0\n" +
                    "\n" +
                    "[SharedUtility]\n" +
                    "\tdisableFileCaching=1";

                await File.WriteAllTextAsync(path, defaultConfiguration);
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
