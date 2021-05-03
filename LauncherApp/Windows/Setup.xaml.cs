using System;
using System.Windows;
using System.Windows.Input;
using LauncherManagement;

namespace LauncherApp
{
    /// <summary>
    /// Interaction logic for Setup.xaml
    /// </summary>
    public partial class Setup : Window
    {
        string _gamePath;
        bool _configValidated;
        string _serverName;

        public Setup(string gamePath, bool configValidated, string serverName)
        {
            InitializeComponent();
            _gamePath = gamePath;
            _configValidated = configValidated;
            _serverName = serverName;
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

        void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Environment.Exit(0);
        }

        async void EasySetupButton_Click(object sender, RoutedEventArgs e)
        {
            ConfigJsonHandler config = new ConfigJsonHandler();
            await config.ConfigureLocationsAsync($"C:/{_serverName}", _configValidated, _gamePath);
            this.Close();
        }

        void AdvancedSetupButton_Click(object sender, RoutedEventArgs e)
        {
            EasySetupButton.Visibility = Visibility.Collapsed;
            EasySetupText.Visibility = Visibility.Collapsed;
            EasySetupDescription.Visibility = Visibility.Collapsed;
            EasySetupCaption.Visibility = Visibility.Collapsed;
            AdvancedSetupButton.Visibility = Visibility.Collapsed;
            AdvancedSetupText.Visibility = Visibility.Collapsed;
            AdvancedSetupDescription.Visibility = Visibility.Collapsed;
            AdvancedSetupCaption.Visibility = Visibility.Collapsed;
            SelectDirectoryButton.Visibility = Visibility.Visible;
            SelectDirectoryDescription.Visibility = Visibility.Visible;
            SelectDirectoryText.Visibility = Visibility.Visible;
            SelectDirectoryTextbox.Visibility = Visibility.Visible;
            OkayDirectoryButton.Visibility = Visibility.Visible;
        }

        void SelectDirectoryButton_Click(object sender, RoutedEventArgs e)
        {
            using var dialog = new System.Windows.Forms.FolderBrowserDialog();
            System.Windows.Forms.DialogResult result = dialog.ShowDialog();

            if (result.ToString().Trim() == "Cancel")
            {
                this.Close();
            }
            else if (result.ToString().Trim() == "OK")
            {
                SelectDirectoryTextbox.Text = dialog.SelectedPath.Replace("\\", "/");
            }
        }

        async void OkayDirectoryButton_Click(object sender, RoutedEventArgs e)
        {
            ConfigJsonHandler config = new ConfigJsonHandler();
            await config.ConfigureLocationsAsync(SelectDirectoryTextbox.Text, _configValidated, _gamePath);
            this.Close();
        }
    }
}
