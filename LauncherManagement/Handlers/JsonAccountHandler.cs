using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using System;
using System.IO;
using System.Threading.Tasks;

namespace LauncherManagement
{
    public class JsonAccountHandler
    {
        public static Action<string> OnJsonReadError;
        public bool ValidateAccountConfig()
        {
            JObject json = new JObject();

            string schemaJson = @"{
                'Username': 'location',
                'Password': 'location'
            }";

            JSchema schema = JSchema.Parse(schemaJson);

            try
            {
                json = JObject.Parse(File.ReadAllText(Path.Join(Directory.GetCurrentDirectory(), "account.json")));
            }
            catch
            {
                return false;
            }

            bool validSchema = json.IsValid(schema);

            int keysContained = 0;

            if (json.ContainsKey("Username"))
            {
                keysContained++;
            }

            if (json.ContainsKey("Password"))
            {
                keysContained++;
            }

            if (validSchema && keysContained == 2)
            {
                return true;
            }

            return false;
        }

        public async Task SaveCredentials(string username, string password)
        {
            string configLocation = Path.Join(Directory.GetCurrentDirectory(), "account.json");

            JObject json;

            if (ValidateAccountConfig())
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
            }
            else
            {
                json = JObject.Parse(@"
                    {'Username': '" + username + "'," +
                    "'Password': '" + password + "'}"
                );
            }

            await File.WriteAllTextAsync(configLocation, json.ToString());
        }

        public AccountProperties GetAccountCredentials()
        {
            string configLocation = Path.Join(Directory.GetCurrentDirectory(), "account.json");
            AccountProperties accountProperties = new AccountProperties();

            if (ValidateAccountConfig())
            {
                using StreamReader sr = File.OpenText(configLocation);
                accountProperties = JsonConvert.DeserializeObject<AccountProperties>(sr.ReadToEnd());
                return accountProperties;
            }

            return null;
        }
    }
}
