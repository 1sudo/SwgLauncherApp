using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Threading.Tasks;

namespace LauncherManagement
{
    public class JsonAccountHandler
    {
        public static Action<string> OnJsonReadError;

        public async Task SaveCredentials(string username, string password)
        {
            string configLocation = Path.Join(Directory.GetCurrentDirectory(), "account.json");

            JObject json;

            if (ValidationHandler.ValidateJson("account.json"))
            {
                json = new JObject();
                try
                {
                    json = JObject.Parse(File.ReadAllText(configLocation));
                }
                catch
                {
                    OnJsonReadError?.Invoke("Error reading account.json! Please report this to staff!");
                }

                foreach (JProperty property in json.Properties())
                {
                    switch (property.Name)
                    {
                        case "Username": property.Value = username; break;
                        case "Password": property.Value = password; break;
                    }
                }

                await File.WriteAllTextAsync(configLocation, json.ToString());
            }
            else
            {
                await File.WriteAllTextAsync(configLocation, 
                    JsonConvert.SerializeObject(new AccountProperties()
                    {
                        Username = username,
                        Password = password
                    }));
            }
        }

        public AccountProperties GetAccountCredentials()
        {
            string configLocation = Path.Join(Directory.GetCurrentDirectory(), "account.json");
            AccountProperties accountProperties = new AccountProperties();

            if (ValidationHandler.ValidateJson("account.json"))
            {
                string file = File.ReadAllText(configLocation);
                return JsonConvert.DeserializeObject<AccountProperties>(file);
            }

            return null;
        }
    }
}
