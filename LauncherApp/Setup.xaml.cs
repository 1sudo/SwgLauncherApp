using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace LauncherApp
{
    /// <summary>
    /// Interaction logic for Setup.xaml
    /// </summary>
    public partial class Setup : Window
    {
        string _gamePath;
        string _serverPath;
        bool _configValidated;
        string _serverName;
        MainWindow _mainWindow;

        public Setup(string gamePath, bool configValidated, string serverName, MainWindow mainWindow)
        {
            InitializeComponent();
            _gamePath = gamePath;
            _configValidated = configValidated;
            _serverName = serverName;
            _mainWindow = mainWindow;
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
            await ConfigureLocations($"C:/{_serverName}");
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

        async Task ConfigureLocations(string serverPath)
        {
            string configLocation = Path.Join(Directory.GetCurrentDirectory(), "config.json");
            
            JObject json;

            if (_configValidated)
            {
                json = new JObject();
                try
                {
                    json = JObject.Parse(File.ReadAllText(configLocation));
                }
                catch
                {
                    MessageBox.Show("Error getting JSON data, please report this to staff!", "JSON Error");
                }

                foreach (JProperty property in json.Properties())
                {
                    if (property.Name == "SWGLocation")
                    {
                        property.Value = _gamePath;
                    }
                    if (property.Name == "ServerLocation")
                    {
                        property.Value = serverPath;
                    }
                }
            }
            else
            {
                json = JObject.Parse(@"{'SWGLocation': '" + _gamePath + "','ServerLocation': '" + serverPath + "'}");
            }

            await File.WriteAllTextAsync(configLocation, json.ToString());
            Directory.CreateDirectory($"{ serverPath }");

            _mainWindow.Show();
            _mainWindow.PreLaunchChecks();
            this.Close();
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
            await ConfigureLocations(SelectDirectoryTextbox.Text);
        }
    }
}
