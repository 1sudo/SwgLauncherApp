using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LauncherManagement;

namespace LauncherApp
{
    #region Enums
    enum Screens
    {
        SETUP_GRID,
        RULES_GRID,
        INSTALL_DIR_GRID,
        GAME_VALIDATION_GRID,
        LOGIN_GRID,
        PRIMARY_GRID,
        SETTINGS_GRID,
        OPTIONS_MODS_GRID,
        DEVELOPER_GRID
    }
    #endregion

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Vars
        List<Grid> _screens;
        string _currentFile;
        double _currentFileStatus;
        double _totalFileStatus;
        string _gamePath;
        readonly AudioHandler _audioHandler;
        readonly AppHandler _appHandler;
        LoginProperties _loginProperties = new LoginProperties();
        string _gamePassword;
        #endregion

        #region Constructor
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
            ValidationHandler.OnInstallCheckFailed += BaseGameVerificationFailed;
        }
        #endregion

        #region WindowManagement
        async void Window_Initialized(object sender, EventArgs e)
        {
            _screens = new List<Grid>()
            {
                SetupGrid,
                RulesAndRegulationsGrid,
                InstallDirectoryGrid,
                GameValidationGrid,
                LoginGrid,
                PrimaryGrid,
                SettingsGrid,
                OptionsAndModsGrid,
                DeveloperGrid
            };

            bool isGameConfigValidated = ValidateGameConfig();

            if (isGameConfigValidated)
            {
                JsonConfigHandler config = new JsonConfigHandler();

                bool isVerified = config.GetVerified();

                if (isVerified)
                {
                    UpdateScreen((int)Screens.LOGIN_GRID);
                    bool isLoggedIn = await CheckAutoLoginAsync();

                    if (isLoggedIn)
                    {
                        await HandleLogin();
                    }
                    else
                    {
                        UpdateScreen((int)Screens.LOGIN_GRID);
                    }
                }
                else
                {
                    UpdateScreen((int)Screens.GAME_VALIDATION_GRID);
                }
            }
        }

        void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        async void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) &&
                Keyboard.IsKeyDown(Key.LeftAlt) &&
                Keyboard.IsKeyDown(Key.Space))
            {
                InstallDirectoryNextButton.IsEnabled = true;
                JsonConfigHandler configHandler = new JsonConfigHandler();
                await configHandler.SetVerified();
                UpdateScreen((int)Screens.LOGIN_GRID);
            }

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

                try
                {
                    await ManifestGenerator.GenerateManifestAsync(generateFromFolder);
                }
                catch { }
            }
        }

        private void UsernameTextbox_KeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.Enter))
            {
                LoginButton_Click(sender, e);
            }
        }

        private void PasswordTextbox_KeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.Enter))
            {
                LoginButton_Click(sender, e);
            }
        }

        #region Checkboxes
        private void GameValidationCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            GameValidationDirectorySection.Visibility = Visibility.Visible;
        }

        private void GameValidationCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
            GameValidationDirectorySection.Visibility = Visibility.Collapsed;
        }

        private void RulesAndRegulationsCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            RulesAndRegulationsNextButton.IsEnabled = true;
        }

        private void RulesAndRegulationsCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
            RulesAndRegulationsNextButton.IsEnabled = false;
        }
        #endregion

        #region Textboxes
        private void GameValidationTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(GameValidationTextBox.Text))
            {
                GameValidationNextButton.IsEnabled = false;
            }
            else
            {
                GameValidationNextButton.IsEnabled = true;
            }
        }

        private void AdvancedSetupTextbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!string.IsNullOrEmpty(AdvancedSetupTextbox.Text))
            {
                InstallDirectoryNextButton.IsEnabled = true;
            }
        }
        #endregion

        #endregion

        #region ScreenManagement
        void UpdateScreen(int id)
        {
            CollapseAllScreens();

            switch (id)
            {
                case (int)Screens.RULES_GRID: EnableScreens(new int[] { 0, 1 }); break;
                case (int)Screens.INSTALL_DIR_GRID: EnableScreens(new int[] { 0, 2 }); break;
                case (int)Screens.GAME_VALIDATION_GRID: EnableScreens(new int[] { 0, 3 }); break;
                case (int)Screens.LOGIN_GRID: EnableScreens(new int[] { 4 }); break;
                case (int)Screens.PRIMARY_GRID: EnableScreens(new int[] { 5 }); break;
                case (int)Screens.SETTINGS_GRID: EnableScreens(new int[] { 5, 6 }); break;
                case (int)Screens.OPTIONS_MODS_GRID: EnableScreens(new int[] { 5, 7 }); break;
                case (int)Screens.DEVELOPER_GRID: EnableScreens(new int[] { 5, 8 }); break;
            }
        }

        void CollapseAllScreens()
        {
            foreach (Grid screen in _screens)
            {
                screen.Visibility = Visibility.Collapsed;
            }
        }

        void EnableScreens(int[] screens)
        {
            foreach (int i in screens)
            {
                _screens[i].Visibility = Visibility.Visible;
            }
        }
        #endregion

        #region Buttons

        #region SidebarButtons
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
                await _appHandler.StartGameAsync(gameOptions, _gamePath, _gamePassword, _loginProperties.Username, selectedValue, true);
                JsonCharacterHandler characterHandler = new JsonCharacterHandler();
                characterHandler.SaveCharacter(selectedValue);
            }
            else
            {
                await _appHandler.StartGameAsync(gameOptions, _gamePath, _gamePassword, _loginProperties.Username);
            }

            PlayButton.IsEnabled = true;
            //FullScanButton.IsEnabled = true;
            SettingsButton.IsEnabled = true;
        }

        void ModsButton_Click(object sender, RoutedEventArgs e)
        {
            _audioHandler.PlayClickSound();
        }

        void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            _audioHandler.PlayClickSound();
            // _appHandler.StartGameConfig(_gamePath);

            // Settings settings = new Settings(this);
            // settings.Show();
        }

        void OptionsButton_Click(object sender, RoutedEventArgs e)
        {

        }

        void DeveloperButton_Click(object sender, RoutedEventArgs e)
        {

        }
        #endregion

        #region SetupButtons
        void RulesAndRegulationsNextButton_Click(object sender, RoutedEventArgs e)
        {
            if ((bool)RulesAndRegulationsCheckbox.IsChecked)
            {
                UpdateScreen((int)Screens.INSTALL_DIR_GRID);
            }
            else
            {
                MessageBox.Show("Please accept the rules and regulations before proceeding.");
            }
        }

        void RulesAndRegulationsCancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        void EasySetupButton_Click(object sender, RoutedEventArgs e)
        {
            InstallDirectoryNextButton.IsEnabled = true;
            EasySetupEllipse.Visibility = Visibility.Visible;
            EasySetupSection.Visibility = Visibility.Visible;
            AdvancedSetupEllipse.Visibility = Visibility.Collapsed;
            AdvancedSetupSection.Visibility = Visibility.Collapsed;
        }

        void AdvancedSetupButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(AdvancedSetupTextbox.Text))
            {
                InstallDirectoryNextButton.IsEnabled = false;
            }
            AdvancedSetupSection.Visibility = Visibility.Visible;
            AdvancedSetupEllipse.Visibility = Visibility.Visible;
            EasySetupEllipse.Visibility = Visibility.Collapsed;
            EasySetupSection.Visibility = Visibility.Collapsed;
        }

        void AdvancedSetupBrowseButton_Click(object sender, RoutedEventArgs e)
        {
            AdvancedSetupTextbox.Text = SelectSWGLocation();
        }

        async void GameValidationNextButton_Click(object sender, RoutedEventArgs e)
        {
            if ((bool)GameValidationCheckbox.IsChecked)
            {
                if (string.IsNullOrEmpty(GameValidationTextBox.Text))
                {
                    MessageBox.Show("Please, first select your base SWG installation location!",
                        "Original Game Files Location Error!", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                }
                else
                {
                    bool isBaseGameValidated = GameSetupHandler.ValidateBaseGame(GameValidationTextBox.Text);

                    if (isBaseGameValidated)
                    {
                        JsonConfigHandler configHandler = new JsonConfigHandler();
                        await configHandler.SetVerified();
                        UpdateScreen((int)Screens.LOGIN_GRID);
                    }
                    else
                    {
                        MessageBox.Show("The path you have chosen does not contain a valid copy of Star Wars Galaxies!" +
                            "Please select a valid path and try again.", "Invalid Base Game Path!", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                    }
                }
            }
            else
            {
                MessageBox.Show("You must first verify that you own a legitimate copy of Star Wars Galaxies!",
                    "Did you forget to check the box? Please verify and try again.", MessageBoxButton.OK, MessageBoxImage.Asterisk);
            }
        }

        void GameValidationBackButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateScreen((int)Screens.INSTALL_DIR_GRID);
        }

        void GameValidationCancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        void GameValidationBrowseButton_Click(object sender, RoutedEventArgs e)
        {
            GameValidationTextBox.Text = SelectSWGLocation();
        }

        void InstallDirectoryBackButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateScreen((int)Screens.RULES_GRID);
        }

        async void InstallDirectoryNextButton_Click(object sender, RoutedEventArgs e)
        {
            if (EasySetupEllipse.IsVisible)
            {
                JsonConfigHandler config = new JsonConfigHandler();
                await config.ConfigureLocationsAsync($"C:/{ServerProperties.ServerName}");
                UpdateScreen((int)Screens.GAME_VALIDATION_GRID);
            }
            else
            {
                if (string.IsNullOrEmpty(AdvancedSetupTextbox.Text))
                {
                    MessageBox.Show("Please, first select a location for SWG Legacy to be installed!",
                        "SWG Legacy File Location Error!", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                }
                else
                {
                    string location = AdvancedSetupTextbox.Text.Replace("\\", "/");
                    JsonConfigHandler config = new JsonConfigHandler();
                    await config.ConfigureLocationsAsync(location);
                    UpdateScreen((int)Screens.GAME_VALIDATION_GRID);
                }
            }
        }

        void InstallDirectoryCancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        #endregion

        #region LoginButtons
        async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            ApiHandler apiHandler = new ApiHandler();
            JsonAccountHandler accountHandler = new JsonAccountHandler();

            LoginProperties loginProperties = await apiHandler.AccountLoginAsync(ServerProperties.ApiUrl, UsernameTextbox.Text, PasswordTextbox.Password.ToString());

            _loginProperties = loginProperties;
            _gamePassword = PasswordTextbox.Password.ToString();

            JsonConfigHandler config = new JsonConfigHandler();

            if ((bool)AutoLoginCheckbox.IsChecked && loginProperties.Result == "Success")
            {
                await config.ToggleAutoLoginAsync(true);
                await accountHandler.SaveCredentials(UsernameTextbox.Text, PasswordTextbox.Password.ToString());
            }
            else
            {
                await config.ToggleAutoLoginAsync(false);
            }

            switch (loginProperties.Result)
            {
                case "Success": 
                    await HandleLogin();
                    break;
                case "ServerDown": 
                    ResultText.Text = "API server down!"; 
                    break;
                case "InvalidCredentials": 
                    ResultText.Text = "Invalid username or password!"; 
                    break;
                case "DatabaseConnectionError":
                    ResultText.Text = "Database connection error!";
                    break;
            }
        }
        #endregion        

        #region SettingsButtons

        async void FullScanButton_Click(object sender, RoutedEventArgs e)
        {
            _audioHandler.PlayClickSound();

            ProgressGrid.Visibility = Visibility.Visible;
            PlayButton.IsEnabled = false;
            // FullScanButton.IsEnabled = false;
            SettingsButton.IsEnabled = false;

            await GameSetupHandler.CheckFilesAsync(_gamePath, true);
        }

        void SubmitSettingsButton_Click(object sender, RoutedEventArgs e)
        {

        }

        void SubmitDeveloperButton_Click(object sender, RoutedEventArgs e)
        {

        }

        void SubmitModsButton_Click(object sender, RoutedEventArgs e)
        {

        }

        #endregion

        #region TopBarButtons
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
        void PatchNotesButton_Click(object sender, RoutedEventArgs e)
        {

        }

        void WebsiteButton_Click(object sender, RoutedEventArgs e)
        {

        }

        void ForumsButton_Click(object sender, RoutedEventArgs e)
        {

        }

        void WikiButton_Click(object sender, RoutedEventArgs e)
        {

        }

        void FacebookButton_Click(object sender, RoutedEventArgs e)
        {

        }

        void DiscordButton_Click(object sender, RoutedEventArgs e)
        {
            _audioHandler.PlayClickSound();
        }
        #endregion

        #endregion

        #region Validation
        async Task CheckGameFiles()
        {
            CharacterSelectGrid.Visibility = Visibility.Collapsed;
            PlayButton.IsEnabled = false;
            PlayButton.Content = "Updating";
            await GameSetupHandler.CheckFilesAsync(GameSetupHandler.GetGamePath());
            PlayButton.IsEnabled = true;
            PlayButton.Content = "Play";
            CharacterSelectGrid.Visibility = Visibility.Visible;
        }

        async Task<bool> CheckAutoLoginAsync()
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

        bool ValidateGameConfig()
        {
            bool isGameValidated = IsGameValidated();

            if (!isGameValidated)
            {
                UpdateScreen((int)Screens.RULES_GRID);
                return false;
            }

            return true;
        }

        bool IsGameValidated()
        {
            bool configValidated = GameSetupHandler.ValidateJsonFile("config.json");

            if (configValidated)
            {
                string path = GameSetupHandler.GetGamePath();

                if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
                {
                    _gamePath = path;
                    return true;
                }
            }

            return false;
        }

        string SelectSWGLocation()
        {
            using var dialog = new System.Windows.Forms.FolderBrowserDialog();
            System.Windows.Forms.DialogResult result = dialog.ShowDialog();

            if (result.ToString().Trim() == "Cancel")
            {
                return "";
            }
            else if (result.ToString().Trim() == "OK")
            {
                return dialog.SelectedPath.Replace("\\", "/");
            }

            return "";
        }
        #endregion

        #region DelegateNotifications
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

        void OnDownloadCompleted()
        {
            ProgressGrid.Visibility = Visibility.Collapsed;
            CharacterSelectGrid.Visibility = Visibility.Visible;
            PlayButton.IsEnabled = true;
            // FullScanButton.IsEnabled = true;
            SettingsButton.IsEnabled = true;
        }

        void CaughtServerError(string error)
        {
            MessageBox.Show(error, "Cannot Connect To Server!", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        void BaseGameVerificationFailed(string message)
        {
            MessageBox.Show("An error occurred accessing the folder you have specified! " +
                "Please check the permissions of the folder and try again.", "Base Game Check Failed!",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        #endregion

        #region Misc
        void PlayHoverSound(object sender, RoutedEventArgs e)
        {
            _audioHandler.PlayHoverSound();
        }

        async Task HandleLogin()
        {
            JsonAccountHandler accountHandler = new JsonAccountHandler();
            AccountProperties account = accountHandler.GetAccountCredentials();

            LogoutButton.Visibility = Visibility.Visible;
            UsernameTextBlock.Visibility = Visibility.Visible;
            UsernameTextBlock.Text =
                System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(
                    account.Username.ToLower()
                );

            UpdateScreen((int)Screens.PRIMARY_GRID);
            GetCharacters();

            try
            {
                await CheckGameFiles();
            }
            catch { }
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
        #endregion
    }
}
