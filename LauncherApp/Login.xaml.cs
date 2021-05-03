using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Windows;
using System.Windows.Input;
using LauncherManagement;

namespace LauncherApp
{
    /// <summary>
    /// Interaction logic for Login.xaml
    /// </summary>
    public partial class Login : Window
    {
        string _url;

        public Login(string url)
        {
            InitializeComponent();
            _url = url;
            ConfigJsonHandler.OnJsonReadError += OnJsonReadError;
        }

        void OnJsonReadError(string error)
        {
            MessageBox.Show(error, "JSON Error!");
        }

        void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            ApiHandler apiHandler = new ApiHandler();

            LoginProperties loginProperties = await apiHandler.AccountLogin(_url, UsernameTextbox.Text, PasswordTextbox.Password.ToString());

            switch (loginProperties.Result)
            {
                case "Success": this.Close(); break;
                case "ServerDown": ResultText.Text = "API server down!"; break;
                case "InvalidCredentials": ResultText.Text = "Invalid username or password!"; break;
            }

            if ((bool)AutoLoginCheckbox.IsChecked)
            {
                ConfigJsonHandler config = new ConfigJsonHandler();
                await config.EnableAutoLoginAsync();
            }
        }
    }
}
