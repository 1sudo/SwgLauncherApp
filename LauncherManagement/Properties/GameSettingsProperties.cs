using System.Collections.Generic;

namespace LauncherManagement
{
    public class GameSettingsProperty
    {
        public string Category { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
    }

    public class GameSettingsProperties
    {
        public List<GameSettingsProperty> Property { get; set; }
    }
}
