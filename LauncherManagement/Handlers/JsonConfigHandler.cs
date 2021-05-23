using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Threading.Tasks;

namespace LauncherManagement
{
    public class JsonConfigHandler
    {
        public static Action<string> OnJsonReadError;

        public async Task EnableAutoLoginAsync()
        {
            string configLocation = Path.Join(Directory.GetCurrentDirectory(), "config.json");

            JObject json = new JObject();
            try
            {
                json = JObject.Parse(File.ReadAllText(configLocation));
            }
            catch
            {
                OnJsonReadError?.Invoke("Error reading config.json! Please report this to staff!");
            }

            ConfigProperties configProperties = JsonConvert.DeserializeObject<ConfigProperties>(json.ToString());

            configProperties.AutoLogin = true;

            string newJson = JsonConvert.SerializeObject(configProperties, Formatting.Indented);

            await File.WriteAllTextAsync(configLocation, newJson.ToString());
        }

        public bool CheckAutoLoginEnabled()
        {
            string configLocation = Path.Join(Directory.GetCurrentDirectory(), "config.json");

            JObject json = new JObject();
            try
            {
                json = JObject.Parse(File.ReadAllText(configLocation));
            }
            catch
            {
                OnJsonReadError?.Invoke("Error reading config.json! Please report this to staff!");
            }

            ConfigProperties configProperties = JsonConvert.DeserializeObject<ConfigProperties>(json.ToString());

            return configProperties.AutoLogin;
        }

        public async Task ConfigureLocationsAsync(string gamePath)
        {
            string configLocation = Path.Join(Directory.GetCurrentDirectory(), "config.json");

            bool configValidated = GameSetupHandler.ValidateJsonFile(configLocation);

            JObject json;

            if (configValidated)
            {
                json = new JObject();
                try
                {
                    json = JObject.Parse(File.ReadAllText(configLocation));
                }
                catch
                {
                    OnJsonReadError?.Invoke("Error reading config.json! Please report this to staff!");
                }

                foreach (JProperty property in json.Properties())
                {
                    switch (property.Name)
                    {
                        case "GameLocation": property.Value = gamePath; break;
                    }
                }

                await File.WriteAllTextAsync(configLocation, json.ToString());
            }
            else
            {
                await File.WriteAllTextAsync(configLocation, JsonConvert.SerializeObject(new ConfigProperties()
                {
                    GameLocation = gamePath,
                    AutoLogin = false,
                    Verified = false
                }, Formatting.Indented));
            }

            Directory.CreateDirectory($"{ gamePath }");
        }

        public string GetServerLocation()
        {
            JObject json = new JObject();
            try
            {
                json = JObject.Parse(File.ReadAllText(Path.Join(Directory.GetCurrentDirectory(), "config.json")));
            }
            catch
            { }

            JToken location;

            if (json.TryGetValue("GameLocation", out location))
            {
                return location.ToString();
            }

            return "";
        }

        public async Task<bool> SetVerified()
        {
            string configFile = Path.Join(Directory.GetCurrentDirectory(), "config.json");
            bool configValidated = ValidationHandler.ValidateJson("config.json");

            if (configValidated)
            {
                string json = File.ReadAllText(configFile);

                ConfigProperties config = JsonConvert.DeserializeObject<ConfigProperties>(json);

                config.Verified = true;

                string newJson = JsonConvert.SerializeObject(config, Formatting.Indented);

                await File.WriteAllTextAsync(configFile, newJson);

                return true;
            }

            return false;
        }

        public bool GetVerified()
        {
            bool configValidated = ValidationHandler.ValidateJson("config.json");

            if (configValidated)
            {
                string json = File.ReadAllText(Path.Join(Directory.GetCurrentDirectory(), "config.json"));

                ConfigProperties config = JsonConvert.DeserializeObject<ConfigProperties>(json);

                return config.Verified;
            }

            return false;
        }
    }
}
