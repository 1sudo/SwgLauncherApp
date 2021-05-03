using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Threading.Tasks;

namespace LauncherManagement
{
    public class ConfigJsonHandler
    {
        public object MessageBox { get; private set; }
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

        public async Task ConfigureLocationsAsync(string serverPath, bool configValidated, string gamePath)
        {
            string configLocation = Path.Join(Directory.GetCurrentDirectory(), "config.json");

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
                        case "SWGLocation": property.Value = gamePath; break;
                        case "ServerLocation": property.Value = serverPath; break;
                        case "AutoLogin": property.Value = true; break;
                    }
                }
            }
            else
            {
                json = JObject.Parse(@"
                    {'SWGLocation': '" + gamePath + "'," +
                    "'ServerLocation': '" + serverPath + "'," +
                    "'AutoLogin': false}"
                );
            }

            await File.WriteAllTextAsync(configLocation, json.ToString());
            Directory.CreateDirectory($"{ serverPath }");
        }
    }
}
