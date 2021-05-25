using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace LauncherManagement
{
    public class LauncherConfigHandler
    {
        public static Action<string> OnJsonReadError;
        DatabaseHandler _db;

        public LauncherConfigHandler()
        {
            _db = new DatabaseHandler();
        }

        public async Task ToggleAutoLoginAsync(bool flag)
        {
            if (flag)
            {
                await _db.ExecuteLauncherConfig
                    (
                        "UPDATE LauncherConfig " +
                        "SET AutoLogin = 1 " +
                        "where Id = 1;"
                    );
            }
            else
            {
                await _db.ExecuteLauncherConfig
                    (
                        "UPDATE LauncherConfig " +
                        "SET Verified = 0 " +
                        "where Id = 1;"
                    );
            }
        }

        public async Task<bool> CheckAutoLoginEnabledAsync()
        {
            List<DatabaseProperties.LauncherConfig> config = await _db.ExecuteLauncherConfig
                (
                    "SELECT AutoLogin " +
                    "FROM LauncherConfig " +
                    "where Id = 1;"
                );

            return config[0].AutoLogin;
        }

        public async Task ConfigureLocationsAsync(string gamePath)
        {
            await _db.ExecuteLauncherConfig
                (
                    "UPDATE LauncherConfig SET " +
                    $"GameLocation = '{gamePath}' " +
                    "where Id = 1;"
                );
        }

        public async Task<string> GetServerNameAsync()
        {
            List<DatabaseProperties.LauncherConfig> config = await _db.ExecuteLauncherConfig($"SELECT ServerName FROM LauncherConfig where Id = 1;");

            return (config.Count > 0) ? config[0].ServerName : "";
        }

        public async Task<string> GetGameLocationAsync()
        {
            List<DatabaseProperties.LauncherConfig> config = await _db.ExecuteLauncherConfig($"SELECT GameLocation FROM LauncherConfig where Id = 1;");

            return (config.Count > 0) ? config[0].GameLocation : "";
        }

        public async Task SetVerifiedAsync()
        {
            await _db.ExecuteLauncherConfig
                (
                    "UPDATE LauncherConfig " +
                    "SET Verified = 1 " +
                    "where Id = 1;"
                );
        }

        public async Task<bool> GetVerifiedAsync()
        {
            List<DatabaseProperties.LauncherConfig> config = await _db.ExecuteLauncherConfig
                (
                    $"SELECT Verified " +
                    $"FROM LauncherConfig " +
                    $"where Id = 1;"
                );

            return config[0].Verified;
        }

        public async Task<Dictionary<string, string>> GetLauncherSettings()
        {
            List<DatabaseProperties.LauncherConfig> config = await _db.ExecuteLauncherConfig
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
            List<DatabaseProperties.LauncherConfig> config = await _db.ExecuteLauncherConfig("SELECT * FROM LauncherConfig;");

            if (config.Count < 1)
            {
                await _db.ExecuteLauncherConfig
                    (
                        "INSERT into LauncherConfig" +
                        "(GameLocation, AutoLogin, Verified, ServerName, ApiUrl, ManifestFilePath, " +
                        "ManifestFileUrl, BackupManifestFileUrl, SWGLoginHost, SWGLoginPort) " +
                        "VALUES " +
                        "('', 0, 0, 'SWGLegacy', 'http://localhost/', 'manifest/required.json', " +
                        "'http://localhost/files/', 'http://localhost:8080/files/', 'localhost', 44453);"
                    );
            }
        }
    }
}
