using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using LauncherApp.Models.Handlers;

namespace LauncherApp.Models.Properties;

public class ConfigFile
{
    static readonly string _configFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LauncherApp/config.json");

    [JsonPropertyName("activeServer")]
    public int ActiveServer { get; set; }

    [JsonPropertyName("servers")]
    public Dictionary<int, AccountProperties>? Servers { get; set; }

    public static async Task GenerateNewConfig(bool deleteCurrent = false)
    {
        if (deleteCurrent)
        {
            File.Delete(_configFile);
        }

        if (!Directory.Exists(Path.GetDirectoryName(_configFile)))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_configFile)!);
        }

        if (File.Exists(_configFile))
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
            LastSelectedCharacter = 0,
            ServerSelection = "SWG Legacy Test Server (Sudo)",
            ApiUrl = "http://192.168.1.102:5000",
            ManifestFilePath = "manifest/required.json",
            ManifestFileUrl = "http://login.darknaught.com:8080/files/",
            BackupManifestFileUrl = "http://login.darknaught.com:8080/files/",
            StatusUrl = "http://login.darknaught.com:8080/status/status.json",
            SWGLoginHost = "login.darknaught.com",
            SWGLoginPort = 44453,
            GameLocation = "C:/SWGLegacy",
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

        await using StreamWriter sw = new(_configFile);

        try
        {
            await sw.WriteAsync(JsonSerializer.Serialize(config, options));
        }
        catch (IOException e)
        {
            Trace.WriteLine("Unable to write config file: " + e.Message);
        }
    }

    public static void SetConfig(ConfigFile config)
    {
        if (!Directory.Exists(Path.GetDirectoryName(_configFile)))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_configFile)!);
        }

        JsonSerializerOptions options = new();
        options.WriteIndented = true;

        using StreamWriter sw = new(_configFile);

        sw.Write(JsonSerializer.Serialize(config, options));
    }

    public static ConfigFile? GetConfig()
    {
        using StreamReader sr = new(_configFile, true);

        var data = sr.ReadToEnd();

        if (data is not null)
        {
            return JsonSerializer.Deserialize<ConfigFile>(data);
        }

        return null;
    }

    public static void SaveCredentials(ConfigFile config)
    {
        CipherHandler cipher = new();
        config.Servers![config.ActiveServer].Username = CipherHandler.Encode(cipher.Transform(config.Servers![config.ActiveServer].Username!.ToLower()));
        config.Servers![config.ActiveServer].Password = CipherHandler.Encode(cipher.Transform(config.Servers![config.ActiveServer].Password!));

        SetConfig(config);
    }

    public static Tuple<string, string> GetAccountCredentials(ConfigFile config)
    {
        CipherHandler cipher = new();
        var username = cipher.Transform(CipherHandler.Decode(config.Servers![config.ActiveServer].Username!));
        var password = cipher.Transform(CipherHandler.Decode(config.Servers![config.ActiveServer].Password!));

        return Tuple.Create(username, password);
    }

    public static void SaveCharacters(List<string> characters, ConfigFile config)
    {
        if (characters is not null)
        {
            List<string> characterList = characters;

            if (!characterList.Contains("None"))
            {
                characterList.Insert(0, "None");
            }

            config!.Servers![config.ActiveServer].Characters = characterList;

            SetConfig(config);
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

    [JsonPropertyName("lastSelectedCharacter")]
    public int LastSelectedCharacter { get; set; }

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

    [JsonPropertyName("statusUrl")]
    public string? StatusUrl { get; set; }

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
