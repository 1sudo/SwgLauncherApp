using System.Windows;
using System.Windows.Input;
using LauncherManagement;
using System.Threading.Tasks;
using System;

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
            JsonConfigHandler.OnJsonReadError += OnJsonReadError;
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

        async Task<bool> CheckAutoLogin()
        {
            JsonAccountHandler accountHandler = new JsonAccountHandler();
            AccountProperties account = accountHandler.GetAccountCredentials();
            JsonConfigHandler configHandler = new JsonConfigHandler();

            if (accountHandler.ValidateAccountConfig())
            {
                if (configHandler.CheckAutoLoginEnabled())
                {
                    if (account != null)
                    {
                        if (account.Username != "" && account.Password != "")
                        {
                            ApiHandler apiHandler = new ApiHandler();

                            LoginProperties loginProperties = await apiHandler.AccountLoginAsync(_url, account.Username, account.Password);

                            switch (loginProperties.Result)
                            {
                                case "Success": return true;
                                case "ServerDown": ResultText.Text = "API server down!"; break;
                                case "InvalidCredentials": ResultText.Text = "Invalid username or password!"; break;
                            }
                        }
                    }
                }
            }

            return false;
        }

        async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            ApiHandler apiHandler = new ApiHandler();
            JsonAccountHandler accountHandler = new JsonAccountHandler();

            LoginProperties loginProperties = await apiHandler.AccountLoginAsync(_url, UsernameTextbox.Text, PasswordTextbox.Password.ToString());

            if ((bool)AutoLoginCheckbox.IsChecked && loginProperties.Result == "Success")
            {
                JsonConfigHandler config = new JsonConfigHandler();
                await config.EnableAutoLoginAsync();
                await accountHandler.SaveCredentials(UsernameTextbox.Text, PasswordTextbox.Password.ToString());
            }

            switch (loginProperties.Result)
            {
                case "Success": this.Close(); break;
                case "ServerDown": ResultText.Text = "API server down!"; break;
                case "InvalidCredentials": ResultText.Text = "Invalid username or password!"; break;
            }
        }

        private async void Window_Activated(object sender, EventArgs e)
        {
            var loggedIn = await CheckAutoLogin();

            if (loggedIn)
            {
                this.Close();
            }
        }
    }
}
