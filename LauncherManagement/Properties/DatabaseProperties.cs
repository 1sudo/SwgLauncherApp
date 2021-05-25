using SQLite;

namespace LauncherManagement
{
    public static class DatabaseProperties
    {
        public class Accounts
        {
            [PrimaryKey, AutoIncrement]
            public int Id { get; set; }
            public string Username { get; set; }
            public string Password { get; set; }
        }

        public class Characters
        {
            [PrimaryKey, AutoIncrement]
            public int Id { get; set; }
            public string Character { get; set; }
        }

        public class LauncherConfig
        {
            [PrimaryKey, AutoIncrement]
            public int Id { get; set; }
            public string GameLocation { get; set; }
            public bool AutoLogin { get; set; }
            public bool Verified { get; set; }
            public string ServerName { get; set; }
            public string ApiUrl { get; set; }
            public string ManifestFilePath { get; set; }
            public string ManifestFileUrl { get; set; }
            public string BackupManifestFileUrl { get; set; }
            public string SWGLoginHost { get; set; }
            public int SWGLoginPort { get; set; }
        }

        public class GameSettings
        {
            [PrimaryKey, AutoIncrement]
            public int Id { get; set; }
            public int Fps { get; set; }
            public int Ram { get; set; }
            public int MaxZoom { get; set; }
        }
    }
}
