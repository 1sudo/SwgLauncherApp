using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace LauncherManagement
{
    public class LauncherConfigHandler : DatabaseHandler
    {
        readonly string     _defaultServerType = "CUEmu Test Server";
        readonly string     _defaultApiUrl = "http://tc.darknaught.com:5000";
        readonly string     _defaultManifestFilePath = "manifest/required.json";
        readonly string     _defaultManifestFileUrl = "http://tc.darknaught.com:8787/files/";
        readonly string     _defaultBackupManifestFileUrl = "http://tc.darknaught.com:8080/files/";
        readonly string     _defaultSWGLoginHost = "tc.darknaught.com";
        readonly int        _defaultSWGLoginPort = 44453;

        readonly string     _defaultSecondaryServerType = "SWG Legacy Test Server";
        readonly string     _defaultSecondaryApiUrl = "http://tc.darknaught.com:5000";
        readonly string     _defaultSecondaryManifestFilePath = "manifest/required.json";
        readonly string     _defaultSecondaryManifestFileUrl = "http://tc.darknaught.com:8787/files/";
        readonly string     _defaultSecondaryBackupManifestFileUrl = "http://tc.darknaught.com:8080/files/";
        readonly string     _defaultSecondarySWGLoginHost = "swglegacy.ddns.net";
        readonly int        _defaultSecondarySWGLoginPort = 44453;

        public async Task<List<string>> GetServerTypes()
        {
            List<DatabaseProperties.LauncherConfig> config = await ExecuteLauncherConfigAsync
                (
                    "SELECT ServerType " +
                    "FROM LauncherConfig;"
                );

            List<string> types = new();

            for (int i = 0; i < config.Count; i++)
            {
                types.Add(config[i].ServerType);
            }

            return types;
        }

        public async Task<List<string>> GetServerType()
        {
            List<DatabaseProperties.LauncherConfig> config = await ExecuteLauncherConfigAsync
                (
                    "SELECT ServerType " +
                    "FROM LauncherConfig " +
                    $"WHERE Id = {ServerSelection.ActiveServer};"
                );

            List<string> types = new();

            for (int i = 0; i < config.Count; i++)
            {
                types.Add(config[i].ServerType);
            }

            return types;
        }

        public async Task<Dictionary<string, string>> GetLauncherSettings()
        {
            List<DatabaseProperties.LauncherConfig> config = await ExecuteLauncherConfigAsync
                (
                    "SELECT " +
                    "ServerType, ApiUrl, ManifestFilePath, ManifestFileUrl, " +
                    "BackupManifestFileUrl, SWGLoginHost, SWGLoginPort " +
                    "FROM LauncherConfig " +
                    $"where Id = {ServerSelection.ActiveServer};"
                );

            return new Dictionary<string, string>()
            {
                { "ServerType",             config[0].ServerType },
                { "ApiUrl",                 config[0].ApiUrl },
                { "ManifestFilePath",       config[0].ManifestFilePath },
                { "ManifestFileUrl",        config[0].ManifestFileUrl },
                { "BackupManifestFileUrl",  config[0].BackupManifestFileUrl },
                { "SWGLoginHost",           config[0].SWGLoginHost },
                { "SWGLoginPort",           config[0].SWGLoginPort.ToString() }
            };
        }

        public async Task InsertDefaultRows()
        {
            List<DatabaseProperties.LauncherConfig> config = await ExecuteLauncherConfigAsync
                (
                    "SELECT * " +
                    "FROM LauncherConfig;"
                );

            if (config.Count < 1)
            {
                await ExecuteLauncherConfigAsync
                    (
                        "INSERT into LauncherConfig " +
                        "(ServerType, ApiUrl, ManifestFilePath, " +
                        "ManifestFileUrl, BackupManifestFileUrl, SWGLoginHost, SWGLoginPort) " +
                        "VALUES " +
                        // First Row
                        $"('{_defaultServerType}', '{_defaultApiUrl}', '{_defaultManifestFilePath}', " +
                        $"'{_defaultManifestFileUrl}', '{_defaultBackupManifestFileUrl}', '{_defaultSWGLoginHost}', {_defaultSWGLoginPort}), " +
                        // Second Row
                        $"('{_defaultSecondaryServerType}', '{_defaultSecondaryApiUrl}', '{_defaultSecondaryManifestFilePath}', " +
                        $"'{_defaultSecondaryManifestFileUrl}', '{_defaultSecondaryBackupManifestFileUrl}', '{_defaultSecondarySWGLoginHost}', {_defaultSecondarySWGLoginPort});"
                    );
            }
        }

        public async Task<bool> WriteLoginConfig(bool cuClient)
        {
            SettingsHandler settingsHandler = new();
            string gameLocation = await settingsHandler.GetGameLocationAsync();
            Dictionary<string, string> settings = await GetLauncherSettings();
            Dictionary<string, string> gameSettings = await settingsHandler.GetGameOptionsControls();

            settings.TryGetValue("SWGLoginHost", out string host);
            settings.TryGetValue("SWGLoginPort", out string port);
            gameSettings.TryGetValue("MaxZoom", out string maxZoom);

            if (!string.IsNullOrEmpty(gameLocation))
            {
                string cfg = (cuClient) ? "login.cfg" : "swgemu_login.cfg";
                string filePath = Path.Join(gameLocation, cfg);

                new FileInfo(filePath).Directory.Create();

                try
                {
                    using FileStream fs = File.OpenWrite(filePath);

                    StringBuilder sb = new();

                    sb.Append("[ClientGame]\n");
                    sb.Append($"loginServerAddress0={host}\n");
                    sb.Append($"loginServerPort0={port}\n");
                    sb.Append($"freeChaseCameraMaximumZoom={maxZoom}");

                    fs.Write(Encoding.ASCII.GetBytes(sb.ToString()));
                    return true;
                }
                catch (Exception e)
                {
                    Trace.WriteLine(e.Message);
                }
            }

            return false;
        }
    }
}
