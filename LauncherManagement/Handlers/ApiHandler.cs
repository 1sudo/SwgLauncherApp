using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;

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
                    { "Accept", "application/text" },
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

                return await response.Content.ReadFromJsonAsync<GameLoginResponseProperties>();
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
                    { "Accept", "application/text" },
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

                return await response.Content.ReadFromJsonAsync<GameAccountCreationResponseProperties>();
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

        public static async Task<ServerStatus> RetrieveStatus(ConfigFile? config)
        {
            using HttpClient cl = new();

            return JsonSerializer.Deserialize<ServerStatus>(await cl.GetStringAsync(new Uri(config!.Servers![config.ActiveServer].StatusUrl!)))!;
        }
    }
}
