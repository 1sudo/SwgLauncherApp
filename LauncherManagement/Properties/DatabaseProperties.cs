using SQLite;

namespace LauncherManagement
{
    public static class DatabaseProperties
    {
        public class Accounts
        {
            [PrimaryKey, AutoIncrement]
            public int Id { get; set; }
            public string? Username { get; set; }
            public string? Password { get; set; }
        }

        public class Characters
        {
            [PrimaryKey, AutoIncrement]
            public int Id { get; set; }
            public string? Character { get; set; }
        }

        public class ActiveServer
        {
            [PrimaryKey]
            public int Id { get; set; }
        }

        public class LauncherConfig
        {
            [PrimaryKey, AutoIncrement]
            public int Id { get; set; }
            public string? ServerType { get; set; }
            public string? ApiUrl { get; set; }
            public string? ManifestFilePath { get; set; }
            public string? ManifestFileUrl { get; set; }
            public string? BackupManifestFileUrl { get; set; }
            public string? SWGLoginHost { get; set; }
            public int SWGLoginPort { get; set; }
        }

        public class Settings
        {
            [PrimaryKey, AutoIncrement]
            public int Id { get; set; }
            public string? GameLocation { get; set; }
            public string? ServerName { get; set; }
            public bool AutoLogin { get; set; }
            public bool Verified { get; set; }
            public int Fps { get; set; }
            public int Ram { get; set; }
            public int MaxZoom { get; set; }
            public int Admin { get; set; }
            public int DebugExamine { get; set; }
            public int Reshade { get; set; }
            public int HDTextures { get; set; }
        }

        public class AdditionalSettings
        {
            [PrimaryKey, AutoIncrement]
            public int Id { get; set; }
            public string? Category { get; set; }
            public string? Key { get; set; }
            public string? Value { get; set; }
        }

        public class TreMods
        {
            [PrimaryKey, AutoIncrement]
            public int Id { get; set; }
            public string? ModName { get; set; }
            public string? FileList { get; set; }
        }
    }
}
