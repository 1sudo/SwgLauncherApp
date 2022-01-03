using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LauncherManagement
{
    public static class ApiHandler
    {
        public static async Task<GameLoginResponseProperties?> AccountLoginAsync(string url, string username, string password)
        {
            var client = new HttpClient();
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri($"{url}/account/login/{username}"),
                Headers =
                {
                    { "Accept", "application/json" },
                },
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "Password", password },
                })
            };

            try
            {
                using HttpResponseMessage response = await client.SendAsync(request);
                
                response.EnsureSuccessStatusCode();

                var body = await response.Content.ReadAsStringAsync();

                JObject? json = null;

#pragma warning disable CS8604 // Possible null reference argument.
                json = JObject.Parse((string?)JToken.Parse(body ?? ""));
#pragma warning restore CS8604 // Possible null reference argument.

                GameLoginResponseProperties? loginProperties = JsonConvert.DeserializeObject<GameLoginResponseProperties>(json!.ToString());

                return loginProperties;
            }
            catch (Exception e)
            {
                await LogHandler.Log(LogType.ERROR, "| AccountLoginAsync | " + e.Message.ToString());

                return new GameLoginResponseProperties
                {
                    Result = "ServerDown",
                    Username = ""
                };
            }
        }

        public static async Task<GameAccountCreationResponseProperties?> AccountCreationAsync(
            string url, GameAccountCreationProperties accountProperties, CaptchaProperties captchaProperties)
        {
            HttpClient client = new();
            HttpRequestMessage request = new()
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri($"{url}/account/create/{accountProperties.Username}"),
                Headers =
                {
                    { "Accept", "application/json" },
                },
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "Password", accountProperties.Password ?? "" },
                    { "PasswordConfirmation", accountProperties.PasswordConfirmation ?? "" },
                    { "Email", accountProperties.Email ?? "" },
                    { "Discord", accountProperties.Discord ?? "" },
                    { "SubscribeToNewsletter", accountProperties.SubscribeToNewsletter.ToString() },
                    { "CaptchaValue1", captchaProperties.Value1.ToString() },
                    { "CaptchaValue2", captchaProperties.Value2.ToString() },
                    { "CaptchaAnswer", captchaProperties.Answer.ToString() }
                })
            };

            try
            {
                using HttpResponseMessage response = await client.SendAsync(request);
                
                response.EnsureSuccessStatusCode();
                string body = await response.Content.ReadAsStringAsync();

                JToken? token = JToken.Parse(body);

                JObject json = JObject.Parse((string?)token ?? "");

                GameAccountCreationResponseProperties? accountCreationProperties = JsonConvert.DeserializeObject<GameAccountCreationResponseProperties>(json.ToString());

                return accountCreationProperties;
            }
            catch (Exception e)
            {
                await LogHandler.Log(LogType.ERROR, "| AccountCreationAsync | " + e.Message.ToString());

                return new GameAccountCreationResponseProperties
                {
                    Result = "ServerDown",
                };
            }
        }
    }
}
