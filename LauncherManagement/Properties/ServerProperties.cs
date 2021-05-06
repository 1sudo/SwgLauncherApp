namespace LauncherManagement
{
    public static class ServerProperties
    {
        public static string ServerName { get; set; }
        public static string ApiUrl { get; set; }
        public static string ManifestFilePath { get; set; }
        public static string ManifestFileUrl { get; set; }
        public static string BackupManifestFileUrl { get; set; }
        public static bool PrimaryServerOffline { get; set; }
    }
}
