using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using LauncherManagement;
using Newtonsoft.Json.Linq;

namespace LauncherApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string _serverName = "SWGLegacy";
        string apiUrl = "http://127.0.0.1:5000";
        bool _gamePathValidated;
        string _currentFile;
        double _currentFileStatus;
        double _totalFileStatus;
        string _gamePath;
        string _serverPath;
        AudioHandler _audioHandler;
        AppHandler _appHandler;

        public MainWindow()
        {
            AudioHandler audioHandler = new AudioHandler();
            _audioHandler = audioHandler;

            AppHandler appHandler = new AppHandler();
            _appHandler = appHandler;

            InitializeComponent();
            CheckLoggedIn();

            DownloadHandler.OnCurrentFileDownloading += ShowFileBeingDownloaded;
            FileDownloader.OnDownloadProgressUpdated += DownloadProgressUpdated;
            DownloadHandler.OnDownloadCompleted += OnDownloadCompleted;
            FileDownloader.OnServerError += CaughtServerError;
            DownloadHandler.OnFullScanFileCheck += OnFullScanFileCheck;
        }

        void CheckLoggedIn()
        {
            Login login = new Login(apiUrl);
            login.ShowDialog();
            PreLaunchChecks();
        }

        public async void PreLaunchChecks()
        {
            bool locationSelected = false;
            bool configValidated = false;
            bool gameValidated = false;
            bool serverPathValidated = false;

            // Ensure config exists
            if (!File.Exists(Path.Join(Directory.GetCurrentDirectory(), "config.json")))
            {
                locationSelected = true;
                gameValidated = ValidateGamePath(SelectSWGLocation());
            }
            // If it exists, validate it
            else
            {
                configValidated = GameSetupHandler.ValidateConfig();

                // If config is validated and keys exist, get the swg location
                if (configValidated)
                {
                    JObject json = new JObject();
                    try
                    {
                        json = JObject.Parse(File.ReadAllText(Path.Join(Directory.GetCurrentDirectory(), "config.json")));
                    }
                    catch
                    {
                        MessageBox.Show("Error getting JSON data, please report this to staff!", "JSON Error");
                    }

                    JToken location;

                    if (json.TryGetValue("SWGLocation", out location))
                    {
                        if (locationSelected)
                        {
                            // Validate the game path
                            gameValidated = ValidateGamePath(location.ToString(), true, true);
                        }
                        // If not validated and location hasn't been selected yet, give the user that option
                        else
                        {
                            gameValidated = ValidateGamePath(location.ToString(), false, true);
                        }
                    }
                }
                // If not validated, select swg location and validate game path
                else
                {
                    gameValidated = ValidateGamePath(SelectSWGLocation());
                }
            }

            if (configValidated && gameValidated)
            {
                serverPathValidated = ValidateServerPath();

                if (!serverPathValidated)
                {
                    Setup setupForm = new Setup(_gamePath, configValidated, _serverName);
                    setupForm.ShowDialog();
                }
            }

            if (configValidated && gameValidated && serverPathValidated)
            {
                await GameSetupHandler.CheckFilesAsync(_serverPath);
            }
        }

        bool ValidateServerPath()
        {
            string path = GameSetupHandler.GetServerPath();

            if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
            {
                _serverPath = path;
                return true;
            }
            return false;
        }

        bool ValidateGamePath(string location, bool locationProvided = true, bool configValidated = false)
        {
            _gamePathValidated = GameSetupHandler.ValidateGamePath(location);

            if (_gamePathValidated && configValidated)
            {
                _gamePath = location;
                return true;
            }

            if (_gamePathValidated)
            {
                Setup setupForm = new Setup(_gamePath, configValidated, _serverName);
                setupForm.ShowDialog();
                return true;
            }
            else
            {
                // If the user hasn't had a chance to provide a SWG location, give it to them
                // and re-run this method
                if (!locationProvided)
                {
                    ValidateGamePath(SelectSWGLocation());
                }
                else
                {
                    MessageBox.Show("Invalid SWG installation location, please ensure the path is correct, then try again.", "Invalid SWG Location");
                    this.Close();
                }
            }

            return false;
        }

        string SelectSWGLocation()
        {
            MessageBox.Show("Please select your SWG installation location.", "Select SWG Location");
            using var dialog = new System.Windows.Forms.FolderBrowserDialog();
            System.Windows.Forms.DialogResult result = dialog.ShowDialog();

            if (result.ToString().Trim() == "Cancel")
            {
                this.Close();
            }
            else if (result.ToString().Trim() == "OK")
            {
                _gamePath = dialog.SelectedPath.Replace("\\", "/");
                return dialog.SelectedPath.Replace("\\", "/");
            }

            return "";
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
            _audioHandler.PlayClickSound();
            this.WindowState = WindowState.Minimized;
        }

        void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            _audioHandler.PlayClickSound();
            this.Close();
        }

        void DiscordButton_Click(object sender, RoutedEventArgs e)
        {
            _audioHandler.PlayClickSound();
        }

        void ResourcesButton_Click(object sender, RoutedEventArgs e)
        {
            _audioHandler.PlayClickSound();
        }

        void MantisButton_Click(object sender, RoutedEventArgs e)
        {
            _audioHandler.PlayClickSound();
        }

        void SkillplannerButton_Click(object sender, RoutedEventArgs e)
        {
            _audioHandler.PlayClickSound();
        }

        void VoteButton_Click(object sender, RoutedEventArgs e)
        {
            _audioHandler.PlayClickSound();
        }

        void DonateButton_Click(object sender, RoutedEventArgs e)
        {
            _audioHandler.PlayClickSound();
        }

        void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            _audioHandler.PlayClickSound();
            _appHandler.StartGame(_serverPath);
        }

        void PlayHoverSound(object sender, RoutedEventArgs e)
        {
            _audioHandler.PlayHoverSound();
        }

        void ModsButton_Click(object sender, RoutedEventArgs e)
        {
            _audioHandler.PlayClickSound();
        }

        void ConfigButton_Click(object sender, RoutedEventArgs e)
        {
            _audioHandler.PlayClickSound();
            _appHandler.StartGameConfig(_serverPath);
        }

        async void FullScanButton_Click(object sender, RoutedEventArgs e)
        {
            _audioHandler.PlayClickSound();

            ProgressGrid.Visibility = Visibility.Visible;
            statusBar.Visibility = Visibility.Collapsed;
            PlayButton.IsEnabled = false;
            FullScanButton.IsEnabled = false;
            ModsButton.IsEnabled = false;
            ConfigButton.IsEnabled = false;
            await GameSetupHandler.CheckFilesAsync(_serverPath, true);
        }

        void DownloadProgressUpdated(long bytesReceived, long totalBytesToReceive, int progressPercentage)
        {
            this.Dispatcher.Invoke(() =>
            {
                if (_currentFileStatus == 0)
                {
                    _currentFileStatus = 0.01;
                }

                if (_totalFileStatus == 0)
                {
                    _totalFileStatus = 0.01;
                }

                double status = (_currentFileStatus / _totalFileStatus) * 100;

                ProgressGrid.Visibility = Visibility.Visible;
                ProgressGrid2.Visibility = Visibility.Visible;
                statusBar.Visibility = Visibility.Hidden;
                DownloadProgress.Value = progressPercentage;
                DownloadProgress2.Value = status;
                
                DownloadProgressText2.Text = $"Currently downloading file { _currentFileStatus } / { _totalFileStatus }";
                DownloadProgressText.Text = $"{ _currentFile } - " +
                    $"{ UnitConversion.ToSize(bytesReceived, UnitConversion.SizeUnits.MB) }MB / " +
                    $"{ UnitConversion.ToSize(totalBytesToReceive, UnitConversion.SizeUnits.MB) }MB";
            });
        }

        void OnFullScanFileCheck(string message, double current, double total)
        {
            this.Dispatcher.Invoke(() =>
            {
                if (current == 0)
                {
                    current = 0.01;
                }

                if (total == 0)
                {
                    total = 0.01;
                }

                double status = (current / total) * 100;
                double filesLeft = total - current;

                DownloadProgress2.Value = status;
                DownloadProgressText2.Text = $"Files left to check: { filesLeft }";
                DownloadProgressText.Text = message;
            });
        }

        void ShowFileBeingDownloaded(string transferType, string file, double current, double total)
        {
            this.Dispatcher.Invoke(() =>
            {
                _currentFileStatus = current;
                _totalFileStatus = total;
                if (transferType == "download")
                {
                    _currentFile = $"Downloading { file }";
                }
                else
                {
                    _currentFile = $"Copying { file }";
                    DownloadProgressText2.Text = _currentFile;
                    DownloadProgress2.Value = (current / total) * 100;
                    DownloadProgressText.Text = "File copy in progress...";
                }
            });
        }

        void CaughtServerError(string error)
        {
            MessageBox.Show(error, "Cannot Connect To Server!", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        void OnDownloadCompleted()
        {
            ProgressGrid.Visibility = Visibility.Collapsed;
            ProgressGrid2.Visibility = Visibility.Collapsed;
            statusBar.Visibility = Visibility.Visible;
            PlayButton.IsEnabled = true;
            FullScanButton.IsEnabled = true;
            ModsButton.IsEnabled = true;
            ConfigButton.IsEnabled = true;
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) &&
                Keyboard.IsKeyDown(Key.LeftShift) &&
                Keyboard.IsKeyDown(Key.OemTilde) &&
                Keyboard.IsKeyDown(Key.F12))
            {
                using var dialog = new System.Windows.Forms.FolderBrowserDialog();
                System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                string generateFromFolder = "";

                if (result.ToString().Trim() == "Cancel")
                {
                    this.Close();
                }
                else if (result.ToString().Trim() == "OK")
                {
                    generateFromFolder = dialog.SelectedPath.Replace("\\", "/");
                }

                ManifestGenerator.GenerateManifestAsync(generateFromFolder);
            }
        }
    }
}
