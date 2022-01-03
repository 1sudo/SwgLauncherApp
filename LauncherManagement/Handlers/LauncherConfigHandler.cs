using System.Text;

namespace LauncherManagement
{
    public class LauncherConfigHandler : DatabaseHandler
    {
        readonly string _defaultServerType = "SWG Legacy Test Server (Sudo)";
        readonly string _defaultApiUrl = "http://login.darknaught.com:5000";
        readonly string _defaultManifestFilePath = "manifest/required.json";
        readonly string _defaultManifestFileUrl = "http://login.darknaught.com:8080/files/";
        readonly string _defaultBackupManifestFileUrl = "http://login.darknaught.com:8080/files/";
        readonly string _defaultSWGLoginHost = "login.darknaught.com";
        readonly int _defaultSWGLoginPort = 44453;

        readonly string _defaultSecondaryServerType = "SWG Legacy Test Server (Lasko)";
        readonly string _defaultSecondaryApiUrl = "http://localhost:5000";
        readonly string _defaultSecondaryManifestFilePath = "manifest/required.json";
        readonly string _defaultSecondaryManifestFileUrl = "http://localhost:8787/files/";
        readonly string _defaultSecondaryBackupManifestFileUrl = "http://localhost:8080/files/";
        readonly string _defaultSecondarySWGLoginHost = "localhost";
        readonly int _defaultSecondarySWGLoginPort = 44453;

        public async Task<List<string>> GetServerTypes()
        {
            List<DatabaseProperties.LauncherConfig>? config = await ExecuteLauncherConfigAsync
                (
                    "SELECT ServerType " +
                    "FROM LauncherConfig;"
                );

            List<string> types = new();

            if (config is not null)
            {
                for (int i = 0; i < config.Count; i++)
                {
                    types.Add(config[i].ServerType ?? "");
                }
            }

            return types;
        }

        public async Task<List<string>> GetServerType()
        {
            List<DatabaseProperties.LauncherConfig>? config = await ExecuteLauncherConfigAsync
                (
                    "SELECT ServerType " +
                    "FROM LauncherConfig " +
                    $"WHERE Id = {ServerSelection.ActiveServer};"
                );

            List<string> types = new();

            if (config is not null)
            {
                for (int i = 0; i < config.Count; i++)
                {
                    types.Add(config[i].ServerType ?? "");
                }
            }

            return types;
        }

        public async Task<Dictionary<string, string>> GetLauncherSettings()
        {
            List<DatabaseProperties.LauncherConfig>? config = await ExecuteLauncherConfigAsync
                (
                    "SELECT " +
                    "ServerType, ApiUrl, ManifestFilePath, ManifestFileUrl, " +
                    "BackupManifestFileUrl, SWGLoginHost, SWGLoginPort " +
                    "FROM LauncherConfig " +
                    $"where Id = {ServerSelection.ActiveServer};"
                );

            return new Dictionary<string, string>()
            {
                { "ServerType",             config![0].ServerType ?? "" },
                { "ApiUrl",                 config[0].ApiUrl ?? "" },
                { "ManifestFilePath",       config[0].ManifestFilePath ?? "" },
                { "ManifestFileUrl",        config[0].ManifestFileUrl ?? "" },
                { "BackupManifestFileUrl",  config[0].BackupManifestFileUrl ?? "" },
                { "SWGLoginHost",           config[0].SWGLoginHost ?? "" },
                { "SWGLoginPort",           config[0].SWGLoginPort.ToString() }
            };
        }

        public async Task SetLauncherSettings(Dictionary<string, string> settings)
        {
            StringBuilder updateString = new();

            int i = 0;
            foreach (KeyValuePair<string, string> setting in settings)
            {
                settings.TryGetValue(setting.Key, out string? output);

                if (i < settings.Count - 1)
                {
                    updateString.Append(setting.Key + " = '" + output + "', ");
                }
                else
                {
                    updateString.Append(setting.Key + " = '" + output + "'");
                }

                i += 1;
            }

            await ExecuteLauncherConfigAsync
                (
                    $"UPDATE LauncherConfig SET {updateString} WHERE Id = {ServerSelection.ActiveServer};"
                );
        }

        public async Task InsertDefaultRows()
        {
            List<DatabaseProperties.LauncherConfig>? config = await ExecuteLauncherConfigAsync
                (
                    "SELECT * " +
                    "FROM LauncherConfig;"
                );

            if (config is not null && config.Count < 1)
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
    }
}
