using System.Collections.Generic;
using System.Threading.Tasks;

namespace LauncherManagement
{
    public class LauncherConfigHandler : DatabaseHandler
    {
        string _defaultGameLocation = "";
        int defaultAutoLogin = 0;
        int defaultVerified = 0;
        string defaultServername = "SWGLegacy";
        string defaultApiUrl = "http://tc.darknaught.com:5000";
        string defaultManifestFilePath = "manifest/required.json";
        string defaultManifestFileUrl = "http://tc.darknaught.com:8787/files/";
        string defaultBackupManifestFileUrl = "http://localhost:8080/files/";
        string defaultSWGLoginHost = "localhost";
        int defaultSWGLoginPort = 44453;

        public async Task ToggleAutoLoginAsync(bool flag)
        {
            if (flag)
            {
                await ExecuteLauncherConfigAsync
                    (
                        "UPDATE LauncherConfig " +
                        "SET AutoLogin = 1 " +
                        "where Id = 1;"
                    );
            }
            else
            {
                await ExecuteLauncherConfigAsync
                    (
                        "UPDATE LauncherConfig " +
                        "SET AutoLogin = 0 " +
                        "where Id = 1;"
                    );
            }
        }

        public async Task<bool> CheckAutoLoginEnabledAsync()
        {
            List<DatabaseProperties.LauncherConfig> config = await ExecuteLauncherConfigAsync
                (
                    "SELECT AutoLogin " +
                    "FROM LauncherConfig " +
                    "where Id = 1;"
                );

            return config[0].AutoLogin;
        }

        public async Task ConfigureLocationsAsync(string gamePath)
        {
            await ExecuteLauncherConfigAsync
                (
                    "UPDATE LauncherConfig SET " +
                    $"GameLocation = '{gamePath}' " +
                    "where Id = 1;"
                );
        }

        public async Task<string> GetServerNameAsync()
        {
            List<DatabaseProperties.LauncherConfig> config = await ExecuteLauncherConfigAsync($"SELECT ServerName FROM LauncherConfig where Id = 1;");

            return (config.Count > 0) ? config[0].ServerName : "";
        }

        public async Task<string> GetGameLocationAsync()
        {
            List<DatabaseProperties.LauncherConfig> config = await ExecuteLauncherConfigAsync($"SELECT GameLocation FROM LauncherConfig where Id = 1;");

            return (config.Count > 0) ? config[0].GameLocation : "";
        }

        public async Task SetVerifiedAsync()
        {
            await ExecuteLauncherConfigAsync
                (
                    "UPDATE LauncherConfig " +
                    "SET Verified = 1 " +
                    "where Id = 1;"
                );
        }

        public async Task<bool> GetVerifiedAsync()
        {
            List<DatabaseProperties.LauncherConfig> config = await ExecuteLauncherConfigAsync
                (
                    $"SELECT Verified " +
                    $"FROM LauncherConfig " +
                    $"where Id = 1;"
                );

            return config[0].Verified;
        }

        public async Task<Dictionary<string, string>> GetLauncherSettings()
        {
            List<DatabaseProperties.LauncherConfig> config = await ExecuteLauncherConfigAsync
                (
                    "SELECT " +
                    "ServerName, ApiUrl, ManifestFilePath, ManifestFileUrl, " +
                    "BackupManifestFileUrl, SWGLoginHost, SWGLoginPort " +
                    "FROM LauncherConfig " +
                    "where Id = 1;"
                );

            return new Dictionary<string, string>()
            {
                { "ServerName",             config[0].ServerName },
                { "ApiUrl",                 config[0].ApiUrl },
                { "ManifestFilePath",       config[0].ManifestFilePath },
                { "ManifestFileUrl",        config[0].ManifestFileUrl },
                { "BackupManifestFileUrl",  config[0].BackupManifestFileUrl },
                { "SWGLoginHost",           config[0].SWGLoginHost },
                { "SWGLoginPort",           config[0].SWGLoginPort.ToString() }
            };
        }

        public async Task InsertDefaultRow()
        {
            List<DatabaseProperties.LauncherConfig> config = await ExecuteLauncherConfigAsync("SELECT * FROM LauncherConfig;");

            if (config.Count < 1)
            {
                await ExecuteLauncherConfigAsync
                    (
                        "INSERT into LauncherConfig" +
                        "(GameLocation, AutoLogin, Verified, ServerName, ApiUrl, ManifestFilePath, " +
                        "ManifestFileUrl, BackupManifestFileUrl, SWGLoginHost, SWGLoginPort) " +
                        "VALUES " +
                        $"('{_defaultGameLocation}', {defaultAutoLogin}, {defaultVerified}, '{defaultServername}', '{defaultApiUrl}', '{defaultManifestFilePath}', " +
                        $"'{defaultManifestFileUrl}', '{defaultBackupManifestFileUrl}', '{defaultSWGLoginHost}', {defaultSWGLoginPort});"
                    );
            }
        }
    }
}
