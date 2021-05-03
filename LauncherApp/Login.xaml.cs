using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace LauncherApp
{
    /// <summary>
    /// Interaction logic for Login.xaml
    /// </summary>
    public partial class Login : Window
    {
        public static Action OnLoggedIn;
        MainWindow _mainWindow;
        string _url;

        public Login(MainWindow mainWindow, string url)
        {
            InitializeComponent();
            _mainWindow = mainWindow;
            _url = url;
        }

        void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        async Task<LoginProperties> AccountLogin(string url, string username, string password)
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

        async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            LoginProperties loginProperties = await AccountLogin(_url, UsernameTextbox.Text, PasswordTextbox.Password.ToString());

            if (loginProperties.Result == "Success")
            {
                OnLoggedIn?.Invoke();
                _mainWindow.Show();
                this.Close();
            }
            else if (loginProperties.Result == "ServerDown")
            {
                ResultText.Text = "API server down!";
            }
            else if (loginProperties.Result == "InvalidCredentials")
            {
                ResultText.Text = "Invalid username or password!";
            }
        }
    }
}
