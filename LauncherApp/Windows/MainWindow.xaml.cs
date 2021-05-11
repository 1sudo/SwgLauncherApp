using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
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
        bool _gamePathValidated;
        string _currentFile;
        double _currentFileStatus;
        double _totalFileStatus;
        string _gamePath;
        string _serverPath;
        readonly AudioHandler _audioHandler;
        readonly AppHandler _appHandler;
        LoginProperties _loginProperties = new LoginProperties();
        string _gamePassword;
        bool _configValidated;

        Action OnProceedToLogin;

        public MainWindow()
        {
            ServerProperties.ServerName = "SWGLegacy";
            ServerProperties.ApiUrl = "http://localhost:5000";
            ServerProperties.ManifestFileUrl = "http://localhost/files/";
            ServerProperties.BackupManifestFileUrl = "http://localhost:8080/files/";
            ServerProperties.ManifestFilePath = "manifest/required.json";

            AudioHandler audioHandler = new AudioHandler();
            _audioHandler = audioHandler;

            AppHandler appHandler = new AppHandler();
            _appHandler = appHandler;

            InitializeComponent();

            DownloadHandler.OnCurrentFileDownloading += ShowFileBeingDownloaded;
            FileDownloader.OnDownloadProgressUpdated += DownloadProgressUpdated;
            DownloadHandler.OnDownloadCompleted += OnDownloadCompleted;
            FileDownloader.OnServerError += CaughtServerError;
            DownloadHandler.OnFullScanFileCheck += OnFullScanFileCheck;
            OnProceedToLogin += ProceedToLogin;
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

        bool ValidateGamePath(string location, bool locationProvided = true, bool configValidated = false, bool looped = false)
        {
            _gamePathValidated = GameSetupHandler.ValidateGamePath(location);

            if (_gamePathValidated && configValidated)
            {
                if (looped)
                {
                    SetupGrid.Visibility = Visibility.Visible;
                }
                _gamePath = location;
                return true;
            }

            if (_gamePathValidated)
            {
                return true;
            }
            else
            {
                // If the user hasn't had a chance to provide a SWG location, give it to them
                // and re-run this method
                if (!locationProvided)
                {
                    return ValidateGamePath(SelectSWGLocation(), true, configValidated, true);
                }
                else
                {
                    MessageBox.Show("Invalid SWG installation location, please ensure the path is correct, then try again.", "Invalid SWG Location");
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

        async void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            _audioHandler.PlayClickSound();
            PlayButton.IsEnabled = false;
            // FullScanButton.IsEnabled = false;
            SettingsButton.IsEnabled = false;

            var selectedValue = CharacterNameComboBox.SelectedValue.ToString();

            GameOptionsProperties gameOptions = new GameOptionsProperties()
            {
                Fps = 144,
                Ram = 4096,
                MaxZoom = 10
            };
            
            if (selectedValue != "System.Windows.Controls.ComboBoxItem: None")
            {
                await _appHandler.StartGameAsync(gameOptions, _serverPath, _gamePassword, _loginProperties.Username, selectedValue, true);
                JsonCharacterHandler characterHandler = new JsonCharacterHandler();
                characterHandler.SaveCharacter(selectedValue);
            }
            else
            {
                await _appHandler.StartGameAsync(gameOptions, _serverPath, _gamePassword, _loginProperties.Username);
            }

            PlayButton.IsEnabled = true;
            //FullScanButton.IsEnabled = true;
            SettingsButton.IsEnabled = true;
        }

        void PlayHoverSound(object sender, RoutedEventArgs e)
        {
            _audioHandler.PlayHoverSound();
        }

        void ModsButton_Click(object sender, RoutedEventArgs e)
        {
            _audioHandler.PlayClickSound();
        }

        void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            _audioHandler.PlayClickSound();
            // _appHandler.StartGameConfig(_serverPath);

            // Settings settings = new Settings(this);
            // settings.Show();
        }

        async void FullScanButton_Click(object sender, RoutedEventArgs e)
        {
            _audioHandler.PlayClickSound();

            ProgressGrid.Visibility = Visibility.Visible;
            PlayButton.IsEnabled = false;
            // FullScanButton.IsEnabled = false;
            SettingsButton.IsEnabled = false;

            await GameSetupHandler.CheckFilesAsync(_serverPath, true);
        }

        void DownloadProgressUpdated(long bytesReceived, long totalBytesToReceive, int progressPercentage)
        {
            this.Dispatcher.Invoke(() =>
            {
                if (_currentFileStatus == 0)
                {
                    _currentFileStatus = 1;
                }

                if (_totalFileStatus == 0)
                {
                    _totalFileStatus = 1;
                }

                double status = (_currentFileStatus / _totalFileStatus) * 100;

                ProgressGrid.Visibility = Visibility.Visible;
                DownloadProgress.Value = status;
                DownloadProgress2.Value = progressPercentage;

                DownloadProgressText2.Text = $"{ _currentFile }";
                DownloadProgressTextRight2.Text = $"{progressPercentage}%";
                DownloadProgressTextRight.Text = $"({ _currentFileStatus }/{ _totalFileStatus })";
                DownloadProgressText.Text = $"Downloading Files";
                // + $"{ UnitConversion.ToSize(bytesReceived, UnitConversion.SizeUnits.MB) }MB / " +
                // $"{ UnitConversion.ToSize(totalBytesToReceive, UnitConversion.SizeUnits.MB) }MB";
            });
        }

        void OnFullScanFileCheck(string message, double current, double total)
        {
            this.Dispatcher.Invoke(() =>
            {
                if (current == 0)
                {
                    current = 1;
                }

                if (total == 0)
                {
                    total = 1;
                }

                double status = (current / total) * 100;
                double filesLeft = total - current;

                DownloadProgress2.Value = status;
                DownloadProgressText2.Text = $"Checking Files";
                DownloadProgressTextRight2.Text = $"({ filesLeft }/{ total })";
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
                    _currentFile = $"{ file }";
                }
                else
                {
                    _currentFile = $"{ file }";
                    DownloadProgressText2.Text = _currentFile;
                    DownloadProgress2.Value = (current / total) * 100;
                    DownloadProgressText.Text = "Copying Files";
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
            CharacterSelectGrid.Visibility = Visibility.Visible;
            PlayButton.IsEnabled = true;
            // FullScanButton.IsEnabled = true;
            SettingsButton.IsEnabled = true;
        }

        private async void Window_KeyDown(object sender, KeyEventArgs e)
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

                await ManifestGenerator.GenerateManifestAsync(generateFromFolder);
            }
        }

        void Window_Activated(object sender, EventArgs e)
        {
            PreLaunchChecks();
        }

        async void ProceedToLogin()
        {
            PrimaryGrid.Visibility = Visibility.Collapsed;
            SetupGrid.Visibility = Visibility.Collapsed;
            LoginGrid.Visibility = Visibility.Visible;

            bool loggedIn = await CheckAutoLogin();

            if (loggedIn)
            {
                await PreparePrimaryGrid();
            }
        }

        async Task PreparePrimaryGrid()
        {
            GetCharacters();
            LoginGrid.Visibility = Visibility.Collapsed;
            PrimaryGrid.Visibility = Visibility.Visible;
            ProgressGrid.Visibility = Visibility.Visible;
            await GameSetupHandler.CheckFilesAsync(_serverPath);
        }

        async void PreLaunchChecks()
        {
            CharacterSelectGrid.Visibility = Visibility.Collapsed;
            bool locationSelected = false;
            bool configValidated = false;
            bool gameValidated = false;
            bool serverPathValidated = false;

            // Ensure config exists
            if (!File.Exists(Path.Join(Directory.GetCurrentDirectory(), "config.json")))
            {
                SetupGrid.Visibility = Visibility.Visible;
                locationSelected = true;
                gameValidated = ValidateGamePath(SelectSWGLocation());
            }
            // If it exists, validate it
            else
            {
                configValidated = GameSetupHandler.ValidateJsonFile("config.json");

                _configValidated = configValidated;

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
                    SetupGrid.Visibility = Visibility.Visible;
                    gameValidated = ValidateGamePath(SelectSWGLocation());
                }
            }

            if (configValidated && gameValidated)
            {
                serverPathValidated = ValidateServerPath();

                if (!serverPathValidated)
                {
                    SetupGrid.Visibility = Visibility.Visible;
                }
            }

            if (configValidated && gameValidated && serverPathValidated)
            {
                JsonConfigHandler config = new JsonConfigHandler();
                string serverLocation = config.GetServerLocation();
                await config.ConfigureLocationsAsync(serverLocation, _configValidated, _gamePath);
                SetupGrid.Visibility = Visibility.Collapsed;
                PrimaryGrid.Visibility = Visibility.Visible;
                OnProceedToLogin?.Invoke();
            }
        }

        void GetCharacters()
        {
            if (_loginProperties.Characters != null)
            {
                foreach (string character in _loginProperties.Characters)
                {
                    CharacterNameComboBox.Items.Add(character);
                }
            }

            string file = Path.Join(Directory.GetCurrentDirectory(), "character.json");

            if (File.Exists(file))
            {
                JsonCharacterHandler characterHandler = new JsonCharacterHandler();

                characterHandler.GetLastSavedCharacter();

                for (int i = 0; i < CharacterNameComboBox.Items.Count; i++)
                {
                    if (characterHandler.GetLastSavedCharacter() == CharacterNameComboBox.Items[i].ToString())
                    {
                        CharacterNameComboBox.SelectedIndex = i;
                    }
                }
            }
        }

        async Task<bool> CheckAutoLogin()
        {
            JsonAccountHandler accountHandler = new JsonAccountHandler();
            AccountProperties account = accountHandler.GetAccountCredentials();
            JsonConfigHandler configHandler = new JsonConfigHandler();

            if (GameSetupHandler.ValidateJsonFile("account.json"))
            {
                if (configHandler.CheckAutoLoginEnabled())
                {
                    if (account != null)
                    {
                        if (account.Username != "" && account.Password != "")
                        {
                            ApiHandler apiHandler = new ApiHandler();

                            LoginProperties loginProperties = await apiHandler.AccountLoginAsync(ServerProperties.ApiUrl, account.Username, account.Password);

                            _loginProperties = loginProperties;
                            _gamePassword = account.Password;

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

            LoginProperties loginProperties = await apiHandler.AccountLoginAsync(ServerProperties.ApiUrl, UsernameTextbox.Text, PasswordTextbox.Password.ToString());

            _loginProperties = loginProperties;
            _gamePassword = PasswordTextbox.Password.ToString();

            if ((bool)AutoLoginCheckbox.IsChecked && loginProperties.Result == "Success")
            {
                JsonConfigHandler config = new JsonConfigHandler();
                await config.EnableAutoLoginAsync();
                await accountHandler.SaveCredentials(UsernameTextbox.Text, PasswordTextbox.Password.ToString());
            }

            switch (loginProperties.Result)
            {
                case "Success": await PreparePrimaryGrid(); break;
                case "ServerDown": ResultText.Text = "API server down!"; break;
                case "InvalidCredentials": ResultText.Text = "Invalid username or password!"; break;
            }
        }

        private async void EasySetupButton_Click(object sender, RoutedEventArgs e)
        {
            JsonConfigHandler config = new JsonConfigHandler();
            await config.ConfigureLocationsAsync($"C:/{ServerProperties.ServerName}", _configValidated, _gamePath);
            PreLaunchChecks();
        }

        private void AdvancedSetupButton_Click(object sender, RoutedEventArgs e)
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

        private void SelectDirectoryButton_Click(object sender, RoutedEventArgs e)
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

        private async void OkayDirectoryButton_Click(object sender, RoutedEventArgs e)
        {
            JsonConfigHandler config = new JsonConfigHandler();
            await config.ConfigureLocationsAsync(SelectDirectoryTextbox.Text, _configValidated, _gamePath);
            PreLaunchChecks();
        }
    }
}
