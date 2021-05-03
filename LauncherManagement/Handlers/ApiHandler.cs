using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace LauncherManagement
{
    public class ApiHandler
    {
        public async Task<LoginProperties> AccountLoginAsync(string url, string username, string password)
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
                using (var response = await client.SendAsync(request))
                {
                    response.EnsureSuccessStatusCode();
                    var body = await response.Content.ReadAsStringAsync();

                    JToken token = JToken.Parse(body);
                    JObject json = JObject.Parse((string)token);

                    LoginProperties loginProperties = JsonConvert.DeserializeObject<LoginProperties>(json.ToString());

                    return loginProperties;
                }
            }
            catch
            {
                return new LoginProperties
                {
                    Result = "ServerDown",
                    Username = ""
                };
            }
        }
    }
}
