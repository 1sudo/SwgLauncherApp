using System.Text.Json.Serialization;
using System.Text.Json;

namespace LauncherManagement
{
    public class ConfigFile
    {
        [JsonPropertyName("activeServer")]
        public int ActiveServer { get; set; }

        [JsonPropertyName("servers")]
        public Dictionary<int, AccountProperties>? Servers { get; set; }

        public async static Task GenerateNewConfig()
        {
            if (File.Exists("config.json"))
                return;

            ConfigFile config = new();

            config.ActiveServer = 0;

            List<AdditionalSettingProperties> additionalSettings = new()
            {
                new AdditionalSettingProperties() { Category = "ClientGraphics", Key = "screenWidth", Value = "1024" },
                new AdditionalSettingProperties() { Category = "ClientGraphics", Key = "screenHeight", Value = "768" },
                new AdditionalSettingProperties() { Category = "ClientGraphics", Key = "allowTearing", Value = "1" },
                new AdditionalSettingProperties() { Category = "ClientGraphics", Key = "useHardwareMouseCursor", Value = "1" },
                new AdditionalSettingProperties() { Category = "Direct3d9", Key = "fullscreenRefreshRate", Value = "60" },
                new AdditionalSettingProperties() { Category = "ClientGame", Key = "skipIntro", Value = "1" },
                new AdditionalSettingProperties() { Category = "ClientGame", Key = "preloadWorldSnapshot", Value = "0" },
                new AdditionalSettingProperties() { Category = "ClientSkeletalAnimation", Key = "lodManagerEnable", Value = "0" },
                new AdditionalSettingProperties() { Category = "SharedUtility", Key = "disableFileCaching", Value = "1" }
            };

            AccountProperties accountProperties = new()
            {
                Username = "",
                Password = "",
                Characters = new List<string>(),
                ServerSelection = "SWG Legacy Test Server (Sudo)",
                ApiUrl = "http://login.darknaught.com:5000",
                ManifestFilePath = "manifest/required.json",
                ManifestFileUrl = "http://login.darknaught.com:8080/files/",
                BackupManifestFileUrl = "http://login.darknaught.com:8080/files/",
                SWGLoginHost = "login.darknaught.com",
                SWGLoginPort = 44453,
                GameLocation = "",
                ServerName = "SWGLegacy",
                AutoLogin = false,
                Verified = false,
                Fps = 60,
                Ram = 2048,
                MaxZoom = 5,
                Admin = false,
                DebugExamine = false,
                Reshade = false,
                HDTextures = false,
                AdditionalSettings = additionalSettings,
                TreMods = new List<TreModProperties>()
            };

            config.Servers = new Dictionary<int, AccountProperties>() { { 0, accountProperties } };

            JsonSerializerOptions options = new();
            options.WriteIndented = true;

            using StreamWriter sw = new("config.json");

            await sw.WriteAsync(JsonSerializer.Serialize(config, options));
        }

        public async static Task SetConfig(ConfigFile config)
        {
            JsonSerializerOptions options = new();
            options.WriteIndented = true;

            using StreamWriter sw = new("config.json");

            await sw.WriteAsync(JsonSerializer.Serialize(config, options));
        }

        public async static Task<ConfigFile?> GetConfig()
        {
            using StreamReader sr = new("config.json", true);

            string? data = await sr.ReadToEndAsync();

            if (data is not null)
            {
                return await Task.Run(() => JsonSerializer.Deserialize<ConfigFile>(data));
            }

            return null;
        }

        public static async Task SaveCredentialsAsync(ConfigFile config)
        {
            await Task.Run(() =>
            {
                CipherHandler cipher = new();
                config.Servers![config.ActiveServer].Username = CipherHandler.Encode(cipher.Transform(config.Servers![config.ActiveServer].Username!.ToLower()));
                config.Servers![config.ActiveServer].Password = CipherHandler.Encode(cipher.Transform(config.Servers![config.ActiveServer].Password!));
            });

            await SetConfig(config);
        }

        public static Tuple<string, string> GetAccountCredentials(ConfigFile config)
        {
            CipherHandler cipher = new();
            string username = cipher.Transform(CipherHandler.Decode(config.Servers![config.ActiveServer].Username!));
            string password = cipher.Transform(CipherHandler.Decode(config.Servers![config.ActiveServer].Password!));

            return Tuple.Create(username, password);
        }

        public static async Task SaveCharactersAsync(List<string> characters, ConfigFile config)
        {
            if (characters is not null)
            {
                List<string> characterList;
                
                characterList = characters;

                if (!characterList.Contains("None"))
                {
                    characterList.Insert(0, "None");
                }

                config!.Servers![config.ActiveServer].Characters = characterList;

                await SetConfig(config);
            }
        }
    }

    public class AccountProperties
    {
        [JsonPropertyName("username")]
        public string? Username { get; set; }
        [JsonPropertyName("password")]
        public string? Password { get; set; }
        [JsonPropertyName("characters")]
        public List<string>? Characters { get; set; }
        [JsonPropertyName("serverSelection")]
        public string? ServerSelection { get; set; }
        [JsonPropertyName("apiUrl")]
        public string? ApiUrl { get; set; }
        [JsonPropertyName("manifestFilePath")]
        public string? ManifestFilePath { get; set; }
        [JsonPropertyName("manifestFileUrl")]
        public string? ManifestFileUrl { get; set; }
        [JsonPropertyName("backupManifestFileUrl")]
        public string? BackupManifestFileUrl { get; set; }
        [JsonPropertyName("swgLoginHost")]
        public string? SWGLoginHost { get; set; }
        [JsonPropertyName("swgLoginPort")]
        public int SWGLoginPort { get; set; }
        [JsonPropertyName("gameLocation")]
        public string? GameLocation { get; set; }
        [JsonPropertyName("serverName")]
        public string? ServerName { get; set; }
        [JsonPropertyName("autoLogin")]
        public bool AutoLogin { get; set; }
        [JsonPropertyName("verified")]
        public bool Verified { get; set; }
        [JsonPropertyName("fps")]
        public int Fps { get; set; }
        [JsonPropertyName("ram")]
        public int Ram { get; set; }
        [JsonPropertyName("maxZoom")]
        public int MaxZoom { get; set; }
        [JsonPropertyName("admin")]
        public bool Admin { get; set; }
        [JsonPropertyName("debugExamine")]
        public bool DebugExamine { get; set; }
        [JsonPropertyName("reshade")]
        public bool Reshade { get; set; }
        [JsonPropertyName("hdTextures")]
        public bool HDTextures { get; set; }
        [JsonPropertyName("additionalSettings")]
        public List<AdditionalSettingProperties>? AdditionalSettings { get; set; }
        [JsonPropertyName("treMods")]
        public List<TreModProperties>? TreMods { get; set; }
    }

    public class AdditionalSettingProperties
    {
        [JsonPropertyName("category")]
        public string? Category { get; set; }
        [JsonPropertyName("key")]
        public string? Key { get; set; }
        [JsonPropertyName("value")]
        public string? Value { get; set; }
    }

    public class TreModProperties
    {
        [JsonPropertyName("modName")]
        public string? ModName { get; set; }
        [JsonPropertyName("fileList")]
        public List<string>? FileList { get; set; }
    }
}
