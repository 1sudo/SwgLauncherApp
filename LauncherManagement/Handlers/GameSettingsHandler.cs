using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace LauncherManagement
{
    public class GameSettingsHandler
    {
        public async Task SaveSettings(List<GameSettingsProperty> properties)
        {
            await File.WriteAllTextAsync(Path.Join(Directory.GetCurrentDirectory(), "settings.json"), 
                JsonConvert.SerializeObject(properties, Formatting.Indented));
        }
    }
}
