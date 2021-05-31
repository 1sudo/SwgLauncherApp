using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        ACCOUNT_GRID,
        LOGIN_GRID,
        CREATE_ACCOUNT_GRID,
        PRIMARY_GRID,
        UPDATES_GRID,
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
        List<Grid>                          _screens;
        string                              _currentFile;
        double                              _currentFileStatus;
        double                              _totalFileStatus;
        string                              _gamePath;
        string                              _gamePassword;
        static bool                         _postLoad = false;
        Dictionary<string, string>          _launcherSettings;
        GameLoginResponseProperties         _loginProperties = new();
        readonly LauncherConfigHandler      _configHandler = new();
        readonly AccountsHandler            _accountHandler = new();
        readonly ActiveServerHandler        _activeServerHandler = new();
        readonly CharacterHandler           _characterHandler = new();
        readonly SettingsHandler            _settingsHandler = new();
        readonly AdditionalSettingsHandler  _additionalSettingsHandler = new();
        readonly AudioHandler               _audioHandler = new();
        readonly FileHandler                _fileHandler = new();
        readonly CaptchaProperties          _captchaProperties = CaptchaController.QuestionAndAnswer();
        #endregion

        #region Constructor
        public MainWindow()
        {
            InitializeComponent();

            DownloadHandler.OnCurrentFileDownloading += ShowFileBeingDownloaded;
            DownloadHandler.OnDownloadProgressUpdated += DownloadProgressUpdated;
            DownloadHandler.OnDownloadCompleted += OnDownloadCompleted;
            DownloadHandler.OnServerError += CaughtServerError;
            DownloadHandler.OnFullScanFileCheck += OnFullScanFileCheck;
            DownloadHandler.OnInstallCheckFailed += BaseGameVerificationFailed;
        }
        #endregion

        #region WindowManagement
        async void Window_Initialized(object sender, EventArgs e)
        {
            await ConfigureDatabase();
            await PopulateControls();

            _launcherSettings = await _configHandler.GetLauncherSettings();

            _screens = new List<Grid>()
            {
                SetupGrid,
                RulesAndRegulationsGrid,
                InstallDirectoryGrid,
                GameValidationGrid,
                AccountGrid,
                LoginGrid,
                CreateAccountGrid,
                PrimaryGrid,
                UpdatesGrid,
                SettingsGrid,
                OptionsAndModsGrid,
                DeveloperGrid
            };

            bool isGameConfigValidated = await ValidateGameConfig();

            if (isGameConfigValidated)
            {
                bool isVerified = await _settingsHandler.GetVerifiedAsync();

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

                _postLoad = true;
            }
        }

        async Task PopulateControls()
        {
            Dictionary<string, string> config = await _configHandler.GetLauncherSettings();
            config.TryGetValue("ApiUrl", out string apiUrl);
            config.TryGetValue("ManifestFilePath", out string manifestFilePath);
            config.TryGetValue("ManifestFileUrl", out string manifestFileUrl);
            config.TryGetValue("SWGLoginHost", out string swgLoginHost);
            config.TryGetValue("SWGLoginPort", out string swgLoginPort);

            DevAPIurl.Text = apiUrl;
            DevManifestURL.Text = manifestFileUrl;
            DevManifestFilePath.Text = manifestFilePath;
            DevSWGhostname.Text = swgLoginHost;
            DevSWGport.Text = swgLoginPort;

            Dictionary<string, string> settings = await _settingsHandler.GetGameOptionsControls();

            settings.TryGetValue("GameLocation", out string gameLocation);

            OptionsInstallDirectoryTextbox.Text = gameLocation;

            foreach (string type in await _configHandler.GetServerTypes())
            {
                OptionsLoginServerBox.Items.Add(type);
            }

            // Subtract 1 since List starts at 0
            OptionsLoginServerBox.SelectedIndex = ServerSelection.ActiveServer - 1;
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
            // Dev - Skip verification
            if (Keyboard.IsKeyDown(Key.LeftCtrl) &&
                Keyboard.IsKeyDown(Key.LeftAlt) &&
                Keyboard.IsKeyDown(Key.F1))
            {
                InstallDirectoryNextButton.IsEnabled = true;
                await _settingsHandler.SetVerifiedAsync();
                UpdateScreen((int)Screens.LOGIN_GRID);
            }

            // Generate manifest file
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
                    Close();
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

        async void CharacterNameComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedCharacter = "";

            if (CharacterNameComboBox.SelectedValue != null)
            {
                selectedCharacter = CharacterNameComboBox.SelectedValue.ToString();
            }

            await _characterHandler.SaveCharacterAsync(selectedCharacter);
        }

        void CreateSecurityQuestionTextblock_Initialized(object sender, EventArgs e)
        {
            CreateSecurityQuestionTextblock.Text = $"{_captchaProperties.Value1} + {_captchaProperties.Value2}";
        }

        void CreateUsernameTextbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            AccountCreationWindowProperties.UsernameTextBox = CreateUsernameTextbox.Text;
        }

        void CreateEmailTextbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            AccountCreationWindowProperties.EmailTextBox = CreateEmailTextbox.Text;
            CheckAccountCreationButton();
        }

        void CreatePasswordTextbox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            AccountCreationWindowProperties.PasswordTextBox = CreatePasswordTextbox.Password;
            CheckAccountCreationButton();
        }

        void CreateConfirmPasswordTextbox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            AccountCreationWindowProperties.PasswordConfirmationTextBox = CreateConfirmPasswordTextbox.Password;
            CheckAccountCreationButton();
        }

        void CreateSecurityQuestionTextbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            AccountCreationWindowProperties.CaptchaQuestionTextBox = CreateSecurityQuestionTextbox.Text;
            CheckAccountCreationButton();
        }

        void CheckAccountCreationButton()
        {
            if (!string.IsNullOrEmpty(AccountCreationWindowProperties.UsernameTextBox) &&
                !string.IsNullOrEmpty(AccountCreationWindowProperties.EmailTextBox) &&
                !string.IsNullOrEmpty(AccountCreationWindowProperties.PasswordTextBox) &&
                !string.IsNullOrEmpty(AccountCreationWindowProperties.PasswordConfirmationTextBox) &&
                !string.IsNullOrEmpty(AccountCreationWindowProperties.CaptchaQuestionTextBox))
            {
                CreateAccountButton.IsEnabled = true;
            }
            else
            {
                CreateAccountButton.IsEnabled = false;
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
                case (int)Screens.LOGIN_GRID: EnableScreens(new int[] { 4, 5 }); break;
                case (int)Screens.CREATE_ACCOUNT_GRID: EnableScreens(new int[] { 4, 6 }); break;
                case (int)Screens.PRIMARY_GRID: EnableScreens(new int[] { 7 }); break;
                case (int)Screens.UPDATES_GRID: EnableScreens(new int[] { 7, 8 }); break;
                case (int)Screens.SETTINGS_GRID: EnableScreens(new int[] { 7, 9 }); break;
                case (int)Screens.OPTIONS_MODS_GRID: EnableScreens(new int[] { 7, 10 }); break;
                case (int)Screens.DEVELOPER_GRID: EnableScreens(new int[] { 7, 11 }); break;
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

            // Get values from database and set to static properties
            await _settingsHandler.GetGameOptions();

            var selectedCharacter = CharacterNameComboBox.SelectedValue.ToString();

            if (selectedCharacter != "None")
            {
                await AppHandler.StartGameAsync(_gamePath, _gamePassword, _loginProperties.Username, selectedCharacter, true);
            }
            else
            {
                await AppHandler.StartGameAsync(_gamePath, _gamePassword, _loginProperties.Username);
            }

            PlayButton.IsEnabled = true;
            //FullScanButton.IsEnabled = true;
            SettingsButton.IsEnabled = true;
        }

        async void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            _audioHandler.PlayClickSound();
            await _fileHandler.GenerateMissingFiles();
            Dictionary<string, string> settings = await _settingsHandler.GetGameOptionsControls();

            settings.TryGetValue("Fps", out string fps);
            settings.TryGetValue("Ram", out string ram);
            settings.TryGetValue("MaxZoom", out string maxZoom);

            string screenHeight = "";
            string screenWidth = "";
            string refreshRate = "";
            string vertexShaderVersion = "";
            string pixelShaderVersion = "";

            List<GameSettingsProperty> properties = await _fileHandler.ParseOptionsCfg();

            foreach (GameSettingsProperty property in properties)
            {
                switch (property.Key)
                {
                    case "\tscreenWidth":                       screenWidth                                 = property.Value; break;
                    case "\tscreenHeight":                      screenHeight                                = property.Value; break;
                    case "\tfullscreenRefreshRate":             refreshRate                                 = property.Value; break;
                    case "\tuseSafeRenderer":                   UseSafeRendererCheckbox.IsChecked           = true; break;
                    case "\tborderlessWindow":                  BorderlessWindowCheckbox.IsChecked          = true; break;
                    case "\twindowed":                          WindowModeCheckbox.IsChecked                = true; break;
                    case "\tdiscardHighestMipMapLevels":        LowDetailTexturesCheckbox.IsChecked         = true; break;
                    case "\tdiscardHighestNormalMipMapLevels":  LowDetailNormalsCheckbox.IsChecked          = true; break;
                    case "\tallowTearing":                      DisableVsyncCheckbox.IsChecked              = true; break;
                    case "\talwaysSetMouseCursor":              DisableFastMouseCheckbox.IsChecked          = true; break;
                    case "\tskipIntro":                         SkipIntroCheckbox.IsChecked                 = true; break;
                    case "\tdisableMiles":                      DisableAudioCheckbox.IsChecked              = true; break;
                    case "\tpreloadWorldSnapshot":              DisableWorldPreloadingCheckbox.IsChecked    = true; break;
                    case "\tlodManagerEnable":                  DisableLODManagerCheckbox.IsChecked         = true; break;
                    case "\tdisableTextureBaking":              DisableTextureBakingCheckbox.IsChecked      = true; break;
                    case "\tdisableFileCaching":                DisableFileCachingCheckbox.IsChecked        = true; break;
                    case "\tenableAsynchronousLoader":          DisableAsyncLoaderCheckbox.IsChecked        = true; break;
                    case "\tskipL0":
                        if (property.Category == "ClientSkeletalAnimation")
                            LowDetailCharactersCheckbox.IsChecked = true;
                        else
                            LowDetailMeshesCheckbox.IsChecked = true;
                        break;
                    case "\tdisableOptionTag":
                        if (property.Value == "DOT3")
                            DisableBumpMappingCheckbox.IsChecked = true;
                        else
                            DisableMultiPassRenderingCheckbox.IsChecked = true;
                        break;
                    case "\tuseHardwareMouseCursor":
                        if (property.Value == "0")
                            DisableHardwareMouseCheckbox.IsChecked = true;
                        else
                            DisableHardwareMouseCheckbox.IsChecked = false; 
                        break;
                }

                if (property.Category == "Direct3d9")
                {
                    if (string.IsNullOrEmpty(property.Key) &&
                        string.IsNullOrEmpty(property.Value))
                    {
                        ShaderBox.SelectedIndex = 0;
                    }

                    if (property.Key == "\tmaxVertexShaderVersion")
                        vertexShaderVersion = property.Value;

                    if (property.Key == "\tmaxPixelShaderVersion")
                        pixelShaderVersion = property.Value;
                }
            }

            if (vertexShaderVersion == "0x0200" && pixelShaderVersion == "0x0200")
                ShaderBox.SelectedIndex = 1;
            if (vertexShaderVersion == "0x0101" && pixelShaderVersion == "0x0104")
                ShaderBox.SelectedIndex = 2;
            if (vertexShaderVersion == "0x0101" && pixelShaderVersion == "0x0101")
                ShaderBox.SelectedIndex = 3;
            if (vertexShaderVersion == "0" && pixelShaderVersion == "0")
                ShaderBox.SelectedIndex = 4;

            ResolutionBox.SelectedValue = $"{screenWidth}x{screenHeight}@{refreshRate}";

            switch (ram)
            {
                case "512": MemoryBox.SelectedIndex = 3; break;
                case "1024": MemoryBox.SelectedIndex = 2; break;
                case "2048": MemoryBox.SelectedIndex = 1; break;
                case "4096": MemoryBox.SelectedIndex = 0; break;
            }

            switch (fps)
            {
                case "30": FpsBox.SelectedIndex = 3; break;
                case "60": FpsBox.SelectedIndex = 2; break;
                case "144": FpsBox.SelectedIndex = 1; break;
                case "240": FpsBox.SelectedIndex = 0; break;
            }

            switch (maxZoom)
            {
                case "1": ZoomBox.SelectedIndex = 0; break;
                case "3": ZoomBox.SelectedIndex = 1; break;
                case "5": ZoomBox.SelectedIndex = 2; break;
                case "7": ZoomBox.SelectedIndex = 3; break;
                case "10": ZoomBox.SelectedIndex = 4; break;
            }

            UpdateScreen((int)Screens.SETTINGS_GRID);
        }

        void OptionsButton_Click(object sender, RoutedEventArgs e)
        {
            _audioHandler.PlayClickSound();
            UpdateScreen((int)Screens.OPTIONS_MODS_GRID);
        }

        void DeveloperButton_Click(object sender, RoutedEventArgs e)
        {
            _audioHandler.PlayClickSound();
            UpdateScreen((int)Screens.DEVELOPER_GRID);
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
                    bool isBaseGameValidated = DownloadHandler.CheckBaseInstallation(GameValidationTextBox.Text);

                    if (isBaseGameValidated)
                    {
                        DownloadHandler.baseGameLocation = GameValidationTextBox.Text;
                        await _settingsHandler.SetVerifiedAsync();
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
                await _settingsHandler.ConfigureLocationsAsync($"C:/{await _settingsHandler.GetServerNameAsync()}");
                await _fileHandler.GenerateMissingFiles();
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
                    await _settingsHandler.ConfigureLocationsAsync(location);
                    await _fileHandler.GenerateMissingFiles();
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
            _launcherSettings.TryGetValue("ApiUrl", out string apiUrl);
            GameLoginResponseProperties loginProperties = await ApiHandler.AccountLoginAsync(apiUrl, UsernameTextbox.Text.ToLower(), PasswordTextbox.Password.ToString());

            _loginProperties = loginProperties;
            _gamePassword = PasswordTextbox.Password.ToString();

            if ((bool)AutoLoginCheckbox.IsChecked && loginProperties.Result == "Success")
            {
                await _settingsHandler.ToggleAutoLoginAsync(true);
                await _accountHandler.SaveCredentialsAsync(UsernameTextbox.Text.ToLower(), PasswordTextbox.Password.ToString());
            }
            else
            {
                await _settingsHandler.ToggleAutoLoginAsync(false);
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

        void CreateAccount_Click(object sender, RoutedEventArgs e)
        {
            UpdateScreen((int)Screens.CREATE_ACCOUNT_GRID);
        }

        async void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            // Turn off autologin
            await _settingsHandler.ToggleAutoLoginAsync(false);

            // Clear characters from combobox
            CharacterNameComboBox.Items.Clear();

            LogoutButton.Visibility = Visibility.Collapsed;
            UsernameTextBlock.Visibility = Visibility.Collapsed;

            // Send back to login screen
            UpdateScreen((int)Screens.LOGIN_GRID);
        }

        async void CreateAccountButton_Click(object sender, RoutedEventArgs e)
        {
            _launcherSettings.TryGetValue("ApiUrl", out string apiUrl);

            if (CreatePasswordTextbox.Password == CreateConfirmPasswordTextbox.Password)
            {
                bool answerParsed = int.TryParse(CreateSecurityQuestionTextbox.Text, out int result);

                if (answerParsed)
                {
                    if (result == _captchaProperties.Answer)
                    {
                        GameAccountCreationResponseProperties creation = await ApiHandler.AccountCreationAsync(apiUrl, new GameAccountCreationProperties()
                        {
                            Username = CreateUsernameTextbox.Text,
                            Email = CreateEmailTextbox.Text,
                            Password = CreatePasswordTextbox.Password,
                            PasswordConfirmation = CreateConfirmPasswordTextbox.Password,
                            Discord = CreateDiscordTextbox.Text,
                            SubscribeToNewsletter = (bool)NewsletterCheckbox.IsChecked
                        }, _captchaProperties);

                        MessageBox.Show(creation.Result);
                    }
                    else
                    {
                        MessageBox.Show("Captcha is incorrect!");
                    }
                }
                else
                {
                    MessageBox.Show("Error parsing captcha answer! Report this to staff!");
                }
            }
            else
            {
                MessageBox.Show("Passwords do not match!");
            }
        }

        void CreateAccountCancelButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateScreen((int)Screens.LOGIN_GRID);
        }
        #endregion        

        #region SettingsButtons

        async void FullScanButton_Click(object sender, RoutedEventArgs e)
        {
            _audioHandler.PlayClickSound();

            ScanDisableButtons();
            await DownloadHandler.CheckFilesAsync(_gamePath, true);
            ScanEnableButtons();
        }

        void ScanEnableButtons()
        {
            ProgressGrid.Visibility = Visibility.Collapsed;
            PlayButton.IsEnabled = true;
            OptionsRerunSetupButton.IsEnabled = true;
            OptionsFullScanButton.IsEnabled = true;
            OptionsInstallDirectoryButton.IsEnabled = true;
            PlayButton.IsEnabled = true;
            PlayButton.Content = "Play";
            CharacterSelectGrid.Visibility = Visibility.Visible;
            OptionsLoginServerBox.IsEnabled = true;
        }

        void ScanDisableButtons()
        {
            ProgressGrid.Visibility = Visibility.Visible;
            PlayButton.IsEnabled = false;
            OptionsRerunSetupButton.IsEnabled = false;
            OptionsFullScanButton.IsEnabled = false;
            OptionsInstallDirectoryButton.IsEnabled = false;
            CharacterSelectGrid.Visibility = Visibility.Collapsed;
            PlayButton.Content = "Updating";
            OptionsLoginServerBox.IsEnabled = false;
        }

        async void SubmitSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            

            



            /* foreach (GameSettingsProperty prop in properties)
            {
                Trace.WriteLine($"Category: {prop.Category}");
                Trace.WriteLine($"Key: {prop.Key}");
                Trace.WriteLine($"Value: {prop.Value}");
            }*/
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
            _audioHandler.PlayClickSound();
            UpdateScreen((int)Screens.PRIMARY_GRID);
        }

        void WebsiteButton_Click(object sender, RoutedEventArgs e)
        {
            _audioHandler.PlayClickSound();
        }

        void ForumsButton_Click(object sender, RoutedEventArgs e)
        {
            _audioHandler.PlayClickSound();
        }

        void WikiButton_Click(object sender, RoutedEventArgs e)
        {
            _audioHandler.PlayClickSound();
        }

        void FacebookButton_Click(object sender, RoutedEventArgs e)
        {
            _audioHandler.PlayClickSound();
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
            ScanDisableButtons();
            await DownloadHandler.CheckFilesAsync(await _settingsHandler.GetGameLocationAsync());
            ScanEnableButtons();
        }

        async Task<bool> CheckAutoLoginAsync()
        {
            Dictionary<string, string> accounts = await _accountHandler.GetAccountCredentialsAsync();

            accounts.TryGetValue("Username", out string username);
            accounts.TryGetValue("Password", out string password);

            if (await _settingsHandler.CheckAutoLoginEnabledAsync())
            {
                if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
                {
                    _launcherSettings.TryGetValue("ApiUrl", out string apiUrl);
                    GameLoginResponseProperties loginProperties = await ApiHandler.AccountLoginAsync(apiUrl, username, password);

                    _loginProperties = loginProperties;
                    _gamePassword = password;

                    switch (loginProperties.Result)
                    {
                        case "Success": return true;
                        case "ServerDown": ResultText.Text = "API server down!"; break;
                        case "InvalidCredentials": ResultText.Text = "Invalid username or password!"; break;
                    }
                }
            }

            return false;
        }

        async Task<bool> ValidateGameConfig()
        {
            bool isGameValidated = await IsGameValidated();

            if (!isGameValidated)
            {
                UpdateScreen((int)Screens.RULES_GRID);
                return false;
            }

            return true;
        }

        async Task<bool> IsGameValidated()
        {
            string path = await _settingsHandler.GetGameLocationAsync();

            if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
            {
                _gamePath = path;
                return true;
            }   

            return false;
        }

        static string SelectSWGLocation()
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

        async Task HandleLogin()
        {
            Dictionary<string, string> accounts = await _accountHandler.GetAccountCredentialsAsync();

            accounts.TryGetValue("Username", out string username);

            LogoutButton.Visibility = Visibility.Visible;
            UsernameTextBlock.Visibility = Visibility.Visible;
            UsernameTextBlock.Text =
                System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(
                    username.ToLower()
                );

            UpdateScreen((int)Screens.PRIMARY_GRID);

            await GetCharactersAsync();

            try
            {
                await CheckGameFiles();
            }
            catch { }
        }

        async Task GetCharactersAsync()
        {
            string lastCharacter = await _characterHandler.GetLastSavedCharacterAsync();

            CharacterNameComboBox.Items.Add(lastCharacter);

            bool noneExists = false;
            foreach (string character in _loginProperties.Characters)
            {
                if (character != lastCharacter)
                {
                    CharacterNameComboBox.Items.Add(character);
                }

                if (character == "None" || lastCharacter == "None")
                {
                    noneExists = true;
                }
            }

            if (!noneExists)
            {
                CharacterNameComboBox.Items.Add("None");
            }
        }

        void PlayHoverSound(object sender, MouseEventArgs e)
        {
            _audioHandler.PlayHoverSound();
        }

        async Task ConfigureDatabase()
        {
            DatabaseHandler db = new();
            await db.CreateTables();

            await _configHandler.InsertDefaultRows();
            await _activeServerHandler.InsertDefaultRow();
            await _settingsHandler.InsertDefaultRow();
            await _additionalSettingsHandler.InsertDefaultRows();
            await _activeServerHandler.GetActiveServer();
        }

        #endregion

        private void ResolutionBox_Initialized(object sender, EventArgs e)
        {
            foreach (string resolution in DisplayResolutions.GetResolutions())
            {
                ResolutionBox.Items.Add(resolution);
            }
        }

        async void OptionsLoginServerBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_postLoad)
            {
                await _activeServerHandler.SetActiveServer(OptionsLoginServerBox.SelectedIndex + 1);
                LogoutButton_Click(this, new RoutedEventArgs());
            }
        }

    }
}
