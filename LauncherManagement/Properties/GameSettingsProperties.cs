using System.Collections.Generic;

namespace LauncherManagement
{
    public class GameSettingsProperty
    {
        public bool Required { get; set; }
        public string Category { get; set; }
        public string Name { get; set; }
        public string Setting { get; set; }
        public string Value { get; set; }
    }

    public class GameSettingsProperties
    {
        public List<GameSettingsProperty> Property { get; set; }
    }
}
