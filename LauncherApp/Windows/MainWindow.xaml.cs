using System.IO;
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
        List<Grid>? _screens;
        string? _currentFile;
        double _currentFileStatus;
        double _totalFileStatus;
        static bool _postLoad = false;
        string? _previousInstallationDirectory;
        int _activeServer;
        ConfigFile? _launcherSettings;
        readonly CaptchaProperties _captchaProperties = CaptchaHandler.QuestionAndAnswer();
        readonly NotifyIcon _notifyIcon;
        #endregion

        #region Constructor
        public MainWindow()
        {
            InitializeComponent();

            _notifyIcon = new();
            _notifyIcon.Icon = new Icon(@"legacy.ico");
            _notifyIcon.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(NotifyIcon_MouseDoubleClick!);

            ContextMenuStrip menuStrip = new();

            ToolStripButton exitButton = new() { Text = "Exit" };
            exitButton.Click += (s, e) => Environment.Exit(0);

            ToolStripButton aboutButton = new() { Text = "About" };

            menuStrip.Items.Add(aboutButton);
            menuStrip.Items.Add(exitButton);

            _notifyIcon.ContextMenuStrip = menuStrip;

            DownloadHandler.OnCurrentFileDownloading += ShowFileBeingDownloaded;
            DownloadHandler.OnDownloadProgressUpdated += DownloadProgressUpdated;
            DownloadHandler.OnDownloadCompleted += OnDownloadCompleted;
            DownloadHandler.OnServerError += CaughtServerError;
            DownloadHandler.OnFullScanFileCheck += OnFullScanFileCheck;
            DownloadHandler.OnInstallCheckFailed += BaseGameVerificationFailed;

            _previousInstallationDirectory = OptionsInstallDirectoryTextbox.Text;
        }
        #endregion

        #region WindowManagement
        async void Window_Initialized(object sender, EventArgs e)
        {
            ProgressGrid.Visibility = Visibility.Collapsed;
            PlayButton.Content = "Updating";
            PlayButton.IsEnabled = false;
            webView.DefaultBackgroundColor = Color.Transparent;

            // Try / catch to regenerate config if it's bad
            try
            {
                await ConfigFile.GenerateNewConfig();

                _launcherSettings = await ConfigFile.GetConfig();
                _activeServer = _launcherSettings!.ActiveServer;

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

                NotLoggedInDisableControls();

                bool isGameConfigValidated = ValidateGameConfig();

                if (isGameConfigValidated)
                {
                    bool isVerified = _launcherSettings.Servers![_activeServer].Verified;

                    if (isVerified)
                    {
                        UpdateScreen((int)Screens.LOGIN_GRID);
                        bool? isLoggedIn = await CheckAutoLoginAsync();

                        if ((bool)isLoggedIn)
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

                PopulateControls();

            }
            catch
            {
                await ConfigFile.GenerateNewConfig(true);
                System.Windows.Forms.Application.Restart();
                Environment.Exit(0);
            }
        }

        void PopulateControls(bool skipLoginServersBox = false)
        {
            DevAPIurl.Text = _launcherSettings!.Servers![_activeServer].ApiUrl;
            DevManifestURL.Text = _launcherSettings!.Servers![_activeServer].ManifestFileUrl;
            DevBackupManifestURL.Text = _launcherSettings!.Servers![_activeServer].BackupManifestFileUrl;
            DevManifestFilePath.Text = _launcherSettings!.Servers![_activeServer].ManifestFilePath;
            DevSWGhostname.Text = _launcherSettings!.Servers![_activeServer].SWGLoginHost;
            DevSWGport.Text = _launcherSettings!.Servers![_activeServer].SWGLoginPort.ToString();
            
            if (_launcherSettings!.Servers![_activeServer].Admin)
            {
                DevAdminCheckbox.IsChecked = true;
            }

            if (_launcherSettings!.Servers![_activeServer].DebugExamine)
            {
                DevDebugCheckbox.IsChecked = true;
            }

            if (_launcherSettings!.Servers![_activeServer].Reshade)
            {
                ModsReshadeCheckbox.IsChecked = true;
            }

            if (_launcherSettings!.Servers![_activeServer].HDTextures)
            {
                ModsHdTextureCheckbox.IsChecked = true;
            }

            OptionsInstallDirectoryTextbox.Text = _launcherSettings!.Servers![_activeServer].GameLocation;

            if (!skipLoginServersBox)
            {
                foreach (KeyValuePair<int, AccountProperties> item in _launcherSettings.Servers)
                {
                    OptionsLoginServerBox.Items.Add(item.Value.ServerSelection);
                }

                if (_launcherSettings!.Servers![_activeServer].LastSelectedCharacter > 0)
                {
                    CharacterNameComboBox.SelectedIndex = _launcherSettings!.Servers![_activeServer].LastSelectedCharacter;
                }
            }

            OptionsLoginServerBox.SelectedIndex = _activeServer;
        }

        void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        async void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            // Dev - Skip verification
            if (Keyboard.IsKeyDown(Key.LeftCtrl) &&
                Keyboard.IsKeyDown(Key.LeftAlt) &&
                Keyboard.IsKeyDown(Key.F1))
            {
                InstallDirectoryNextButton.IsEnabled = true;

                _launcherSettings!.Servers![_activeServer].Verified = true;
                await ConfigFile.SetConfig(_launcherSettings);

                UpdateScreen((int)Screens.LOGIN_GRID);
            }

            // Generate manifest file
            if (Keyboard.IsKeyDown(Key.LeftCtrl) &&
                Keyboard.IsKeyDown(Key.LeftShift) &&
                Keyboard.IsKeyDown(Key.F12))
            {
                using var dialog = new FolderBrowserDialog();
                DialogResult result = dialog.ShowDialog();
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

        private void UsernameTextbox_KeyDown(object sender, RoutedEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.Enter))
            {
                LoginButton_Click(sender, e);
            }
        }

        private void PasswordTextbox_KeyDown(object sender, RoutedEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.Enter))
            {
                LoginButton_Click(sender, e);
            }
        }

        async void CharacterNameComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _launcherSettings!.Servers![_activeServer].LastSelectedCharacter = CharacterNameComboBox.SelectedIndex;

            await ConfigFile.SetConfig(_launcherSettings);
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
            if (_screens is not null)
            {
                foreach (Grid screen in _screens)
                {
                    screen.Visibility = Visibility.Collapsed;
                }
            }
        }

        void EnableScreens(int[] screens)
        {
            if (_screens is not null)
            {
                foreach (int i in screens)
                {
                    _screens[i].Visibility = Visibility.Visible;
                }
            }
        }
        #endregion

        #region Buttons

        #region SidebarButtons
        void ResourcesButton_Click(object sender, RoutedEventArgs e)
        {
            AppHandler.OpenDefaultBrowser("http://galaxyharvester.net/ghHome.py");
        }

        void MantisButton_Click(object sender, RoutedEventArgs e)
        {
            AppHandler.OpenDefaultBrowser("https://mantis.swgsremu.com/login_page.php");
        }

        void SkillplannerButton_Click(object sender, RoutedEventArgs e)
        {
            // OpenCefBrowser("https://swgsremu.com/skillplanner/#/");
        }

        void VoteButton_Click(object sender, RoutedEventArgs e)
        {
            AppHandler.OpenDefaultBrowser("https://topg.org/swg-private-servers/");
        }

        void DonateButton_Click(object sender, RoutedEventArgs e)
        {
            AppHandler.OpenDefaultBrowser("http://donate.swglegacy.com");
        }

        async void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            PlayButton.IsEnabled = false;
            // FullScanButton.IsEnabled = false;
            SettingsButton.IsEnabled = false;

            (string username, string password) = ConfigFile.GetAccountCredentials(_launcherSettings!);

            try
            {
                var selectedCharacter = CharacterNameComboBox.SelectedValue.ToString();

                if (selectedCharacter != "None" && selectedCharacter is not null)
                {
                    await AppHandler.StartGameAsync(_launcherSettings, password, username ?? "", selectedCharacter, true);
                }
                else
                {
                    await AppHandler.StartGameAsync(_launcherSettings, password, username ?? "");
                }
            }
            catch
            {
                await AppHandler.StartGameAsync(_launcherSettings, password, username ?? "");
            }

            PlayButton.IsEnabled = true;
            //FullScanButton.IsEnabled = true;
            SettingsButton.IsEnabled = true;
        }

        async void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            await FileHandler.GenerateMissingFiles(_launcherSettings);

            int fps = _launcherSettings!.Servers![_activeServer].Fps;
            int ram = _launcherSettings!.Servers![_activeServer].Ram;
            int maxZoom = _launcherSettings!.Servers![_activeServer].MaxZoom;

            string screenHeight = "";
            string screenWidth = "";
            string refreshRate = "";
            string vertexShaderVersion = "";
            string pixelShaderVersion = "";

            List<AdditionalSettingProperties> properties = await FileHandler.ParseOptionsCfg(_launcherSettings);

            foreach (AdditionalSettingProperties property in properties)
            {
                switch (property.Key)
                {
                    case "\tscreenWidth":                       screenWidth                                 = property.Value ?? ""; break;
                    case "\tscreenHeight":                      screenHeight                                = property.Value ?? ""; break;
                    case "\tfullscreenRefreshRate":             refreshRate                                 = property.Value ?? ""; break;
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
                        vertexShaderVersion = property.Value ?? "";

                    if (property.Key == "\tmaxPixelShaderVersion")
                        pixelShaderVersion = property.Value ?? "";
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
                case 512: MemoryBox.SelectedIndex = 3; break;
                case 1024: MemoryBox.SelectedIndex = 2; break;
                case 2048: MemoryBox.SelectedIndex = 1; break;
                case 4096: MemoryBox.SelectedIndex = 0; break;
            }

            switch (fps)
            {
                case 30: FpsBox.SelectedIndex = 3; break;
                case 60: FpsBox.SelectedIndex = 2; break;
                case 144: FpsBox.SelectedIndex = 1; break;
                case 240: FpsBox.SelectedIndex = 0; break;
            }

            switch (maxZoom)
            {
                case 1: ZoomBox.SelectedIndex = 0; break;
                case 3: ZoomBox.SelectedIndex = 1; break;
                case 5: ZoomBox.SelectedIndex = 2; break;
                case 7: ZoomBox.SelectedIndex = 3; break;
                case 10: ZoomBox.SelectedIndex = 4; break;
            }

            UpdateScreen((int)Screens.SETTINGS_GRID);
        }

        void OptionsButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateScreen((int)Screens.OPTIONS_MODS_GRID);
        }

        void DeveloperButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateScreen((int)Screens.DEVELOPER_GRID);
        }
        #endregion

        #region SetupButtons
        void RulesAndRegulationsNextButton_Click(object sender, RoutedEventArgs e)
        {
            if (RulesAndRegulationsCheckbox.IsChecked is not null && (bool)RulesAndRegulationsCheckbox.IsChecked)
            {
                UpdateScreen((int)Screens.INSTALL_DIR_GRID);
            }
            else
            {
                // MessageBox.Show("Please accept the rules and regulations before proceeding.");
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
            if (GameValidationCheckbox.IsChecked is not null && (bool)GameValidationCheckbox.IsChecked)
            {
                if (string.IsNullOrEmpty(GameValidationTextBox.Text))
                {
                    // MessageBox.Show("Please, first select your base SWG installation location!",
                        //"Original Game Files Location Error!", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                }
                else
                {
                    bool isBaseGameValidated = await DownloadHandler.CheckBaseInstallation(GameValidationTextBox.Text);

                    if (isBaseGameValidated)
                    {
                        DownloadHandler.BaseGameLocation = GameValidationTextBox.Text;
                        _launcherSettings!.Servers![_activeServer].Verified = true;
                        await ConfigFile.SetConfig(_launcherSettings);
                        UpdateScreen((int)Screens.LOGIN_GRID);
                    }
                    else
                    {
                        // MessageBox.Show("The path you have chosen does not contain a valid copy of Star Wars Galaxies!" +
                            //"Please select a valid path and try again.", "Invalid Base Game Path!", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                    }
                }
            }
            else
            {
                // MessageBox.Show("You must first verify that you own a legitimate copy of Star Wars Galaxies!",
                    //"Did you forget to check the box? Please verify and try again.", MessageBoxButton.OK, MessageBoxImage.Asterisk);
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
                _launcherSettings!.Servers![_activeServer].GameLocation = $"C:/{_launcherSettings!.Servers![_activeServer].ServerName}";

                await ConfigFile.SetConfig(_launcherSettings);

                await FileHandler.GenerateMissingFiles(_launcherSettings);
                UpdateScreen((int)Screens.GAME_VALIDATION_GRID);
            }
            else
            {
                if (string.IsNullOrEmpty(AdvancedSetupTextbox.Text))
                {
                    //MessageBox.Show("Please, first select a location for SWG Legacy to be installed!",
                        //"SWG Legacy File Location Error!", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                }
                else
                {
                    string location = AdvancedSetupTextbox.Text.Replace("\\", "/");
                    _launcherSettings!.Servers![_activeServer].GameLocation = location;
                    await ConfigFile.SetConfig(_launcherSettings);
                    await FileHandler.GenerateMissingFiles(_launcherSettings);
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
            string? apiUrl = "";

            if (_launcherSettings is not null)
            {
                apiUrl = _launcherSettings.Servers![_activeServer].ApiUrl;
            }

            GameLoginResponseProperties? loginProperties = new();

            if (apiUrl is not null)
            {
                if (_launcherSettings!.Servers![_activeServer].AutoLogin)
                {
                    (string username, string password) = ConfigFile.GetAccountCredentials(_launcherSettings!);

                    loginProperties = await ApiHandler.AccountLoginAsync(apiUrl, username, password);
                }
                else
                {
                    loginProperties = await ApiHandler.AccountLoginAsync(apiUrl, UsernameTextbox.Text.ToLower(), PasswordTextbox.Password.ToString());

                    if ((bool)AutoLoginCheckbox.IsChecked! && loginProperties!.Result == "Success")
                    {
                        _launcherSettings.Servers[_activeServer].AutoLogin = true;
                        await ConfigFile.SetConfig(_launcherSettings);

                        _launcherSettings.Servers[_activeServer].Username = UsernameTextbox.Text.ToLower();
                        _launcherSettings.Servers[_activeServer].Password = PasswordTextbox.Password.ToString();
                        await ConfigFile.SaveCredentialsAsync(_launcherSettings);
                    }
                }

                if (loginProperties!.Result == "Success")
                {
                    ResultText.Text = "";
                    ResultText.Visibility = Visibility.Collapsed;
                    CreateUsernameTextbox.Text = "";
                    CreateEmailTextbox.Text = "";
                    CreatePasswordTextbox.Password = "";
                    CreateConfirmPasswordTextbox.Password = "";
                    CreateDiscordTextbox.Text = "";
                    CreateSecurityQuestionTextbox.Text = "";
                    NewsletterCheckbox.IsChecked = false;
                    accountCreationResponseTextbox.Visibility = Visibility.Collapsed;
                    await ConfigFile.SaveCharactersAsync(loginProperties!.Characters!, _launcherSettings);
                }
            }

            if (loginProperties is null)
                return;

            switch (loginProperties.Result)
            {
                case "Success": 
                    await HandleLogin();
                    break;
                case "ServerDown":
                    ResultText.Visibility = Visibility.Visible;
                    ResultText.Foreground = System.Windows.Media.Brushes.Red;
                    ResultText.Text = "API server down!"; 
                    break;
                case "InvalidCredentials":
                    ResultText.Visibility = Visibility.Visible;
                    ResultText.Foreground = System.Windows.Media.Brushes.Red;
                    ResultText.Text = "Invalid username or password!"; 
                    break;
                case "DatabaseConnectionError":
                    ResultText.Visibility = Visibility.Visible;
                    ResultText.Foreground = System.Windows.Media.Brushes.Red;
                    ResultText.Text = "Database connection error!";
                    break;
                default:
                    ResultText.Visibility = Visibility.Visible;
                    ResultText.Foreground = System.Windows.Media.Brushes.Red;
                    ResultText.Text = loginProperties.Result;
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
            _launcherSettings!.Servers![_activeServer].AutoLogin = false;
            await ConfigFile.SetConfig(_launcherSettings);

            // Clear characters from combobox
            CharacterNameComboBox.Items.Clear();

            LogoutButton.Visibility = Visibility.Collapsed;
            UsernameTextBlock.Visibility = Visibility.Collapsed;

            // Send back to login screen
            UpdateScreen((int)Screens.LOGIN_GRID);

            NotLoggedInDisableControls();
        }

        async void CreateAccountButton_Click(object sender, RoutedEventArgs e)
        {
            string? apiUrl = "";
            if (_launcherSettings is not null)
            {
                apiUrl = _launcherSettings.Servers![_activeServer].ApiUrl;
            }

            if (CreatePasswordTextbox.Password == CreateConfirmPasswordTextbox.Password)
            {
                bool answerParsed = int.TryParse(CreateSecurityQuestionTextbox.Text, out int result);

                if (answerParsed)
                {
                    if (result == _captchaProperties.Answer)
                    {
                        if (apiUrl is not null)
                        {
                            if (NewsletterCheckbox.IsChecked is null)
                            {
                                NewsletterCheckbox.IsChecked = false;
                            }

                            GameAccountCreationResponseProperties response = await ApiHandler.AccountCreationAsync(apiUrl, new GameAccountCreationProperties()
                            {
                                Username = CreateUsernameTextbox.Text,
                                Email = CreateEmailTextbox.Text,
                                Password = CreatePasswordTextbox.Password,
                                PasswordConfirmation = CreateConfirmPasswordTextbox.Password,
                                Discord = CreateDiscordTextbox.Text,
                                SubscribeToNewsletter = (bool)NewsletterCheckbox.IsChecked
                            }, _captchaProperties) ?? new GameAccountCreationResponseProperties();

                            if (response.Result!.Trim() == "Success")
                            {
                                ResultText.Visibility = Visibility.Visible;
                                ResultText.Foreground = System.Windows.Media.Brushes.Green;
                                ResultText.Text = "Account Created.";

                                UpdateScreen((int)Screens.LOGIN_GRID);

                                UsernameTextbox.Text = CreateUsernameTextbox.Text;
                                PasswordTextbox.Password = CreatePasswordTextbox.Password;

                                CreateUsernameTextbox.Text = "";
                                CreateEmailTextbox.Text = "";
                                CreatePasswordTextbox.Password = "";
                                CreateConfirmPasswordTextbox.Password = "";
                                CreateDiscordTextbox.Text = "";
                                CreateSecurityQuestionTextbox.Text = "";
                                NewsletterCheckbox.IsChecked = false;
                                accountCreationResponseTextbox.Visibility = Visibility.Collapsed;
                            }
                            else
                            {
                                accountCreationResponseTextbox.Text = response.Result!.Trim();
                                accountCreationResponseTextbox.Visibility = Visibility.Visible;
                            }
                        }
                    }
                    else
                    {
                        accountCreationResponseTextbox.Text = "Captcha is incorrect!";
                        accountCreationResponseTextbox.Visibility = Visibility.Visible;
                    }
                }
                else
                {
                    accountCreationResponseTextbox.Text = "Error parsing captcha answer! Report this to staff!";
                    accountCreationResponseTextbox.Visibility = Visibility.Visible;
                }
            }
            else
            {
                accountCreationResponseTextbox.Text = "Passwords do not match!";
                accountCreationResponseTextbox.Visibility = Visibility.Visible;
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
            ScanDisableButtons(true);
            await DownloadHandler.CheckFilesAsync(_launcherSettings!, true);
            ScanEnableButtons();
        }

        private void OptionsInstallDirectoryButton_Click(object sender, RoutedEventArgs e)
        {
            _previousInstallationDirectory = OptionsInstallDirectoryTextbox.Text;

            using var dialog = new FolderBrowserDialog();
            DialogResult result = dialog.ShowDialog();

            if (result.ToString().Trim() == "Cancel")
            {
                return;
            }
            else if (result.ToString().Trim() == "OK")
            {
                OptionsInstallDirectoryTextbox.Text = dialog.SelectedPath.Replace("\\", "/");
            }
        }

        void ScanEnableButtons()
        {
            ProgressGrid.Visibility = Visibility.Collapsed;
            PlayButton.IsEnabled = true;
            OptionsFullScanButton.IsEnabled = true;
            PlayButton.Content = "Play";
            CharacterSelectGrid.Visibility = Visibility.Visible;
            RevertDeveloperButton.IsEnabled = true;
            RevertModsButton.IsEnabled = true;
            RevertSettingsButton.IsEnabled = true;
            SubmitDeveloperButton.IsEnabled = true;
            SubmitModsButton.IsEnabled = true;
            SubmitSettingsButton.IsEnabled = true;
        }

        void ScanDisableButtons(bool fullscan = false)
        {
            if (fullscan)
            {
                ProgressGrid.Visibility = Visibility.Visible;
                CharacterSelectGrid.Visibility = Visibility.Collapsed;
            }
            
            PlayButton.IsEnabled = false;
            OptionsFullScanButton.IsEnabled = false;
            PlayButton.Content = "Updating";
            RevertDeveloperButton.IsEnabled = false;
            RevertModsButton.IsEnabled = false;
            RevertSettingsButton.IsEnabled = false;
            SubmitDeveloperButton.IsEnabled = false;
            SubmitModsButton.IsEnabled = false;
            SubmitSettingsButton.IsEnabled = false;
        }

        void SettingsDisableButtons()
        {
            PlayButton.IsEnabled = false;
            OptionsFullScanButton.IsEnabled = false;
            PlayButton.Content = "Updating";
            RevertDeveloperButton.IsEnabled = false;
            RevertModsButton.IsEnabled = false;
            RevertSettingsButton.IsEnabled = false;
            SubmitDeveloperButton.IsEnabled = false;
            SubmitModsButton.IsEnabled = false;
            SubmitSettingsButton.IsEnabled = false;
        }

        void SettingsEnableButtons()
        {
            OptionsFullScanButton.IsEnabled = true;
            PlayButton.IsEnabled = true;
            PlayButton.Content = "Play";
            RevertDeveloperButton.IsEnabled = true;
            RevertModsButton.IsEnabled = true;
            RevertSettingsButton.IsEnabled = true;
            SubmitDeveloperButton.IsEnabled = true;
            SubmitModsButton.IsEnabled = true;
            SubmitSettingsButton.IsEnabled = true;
        }

        async void SubmitSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsDisableButtons();
            List<AdditionalSettingProperties> properties = new();

            int fps = 0;
            int ram = 0;
            int maxZoom = 0;

            switch (FpsBox.SelectedIndex)
            {
                case 3: fps = 30; break;
                case 2: fps = 60; break;
                case 1: fps = 144; break;
                case 0: fps = 240; break;
            }

            switch (MemoryBox.SelectedIndex)
            {
                case 3: ram = 512; break;
                case 2: ram = 1024; break;
                case 1: ram = 2048; break;
                case 0: ram = 4096; break;
            }

            switch (ZoomBox.SelectedIndex)
            {
                case 0: maxZoom = 1; break;
                case 1: maxZoom = 3; break;
                case 2: maxZoom = 5; break;
                case 3: maxZoom = 7; break;
                case 4: maxZoom = 10; break;
            }

            _launcherSettings!.Servers![_activeServer].Fps = fps;
            _launcherSettings!.Servers![_activeServer].Ram = ram;
            _launcherSettings!.Servers![_activeServer].MaxZoom = maxZoom;

            await ConfigFile.SetConfig(_launcherSettings);

            string screenWidth = ResolutionBox.SelectedValue.ToString()!.Split("x")[0];
            string screenHeight = ResolutionBox.SelectedValue.ToString()!.Split("x")[1].Split("@")[0];
            string refreshRate = ResolutionBox.SelectedValue.ToString()!.Split("@")[1];

            properties.Add(new AdditionalSettingProperties { Category = "Direct3d9", Key = "fullscreenRefreshRate", Value = $"{refreshRate}" });

            if (ShaderBox.SelectedIndex == 1)
            {
                properties.Add(new AdditionalSettingProperties { Category = "Direct3d9", Key = "maxVertexShaderVersion", Value = "0x0200" });
                properties.Add(new AdditionalSettingProperties { Category = "Direct3d9", Key = "maxPixelShaderVersion", Value = "0x0200" });
            }

            if (ShaderBox.SelectedIndex == 2)
            {
                properties.Add(new AdditionalSettingProperties { Category = "Direct3d9", Key = "maxVertexShaderVersion", Value = "0x0101" });
                properties.Add(new AdditionalSettingProperties { Category = "Direct3d9", Key = "maxPixelShaderVersion", Value = "0x0104" });
            }

            if (ShaderBox.SelectedIndex == 3)
            {
                properties.Add(new AdditionalSettingProperties { Category = "Direct3d9", Key = "maxVertexShaderVersion", Value = "0x0101" });
                properties.Add(new AdditionalSettingProperties { Category = "Direct3d9", Key = "maxPixelShaderVersion", Value = "0x0101" });
            }

            if (ShaderBox.SelectedIndex == 4)
            {
                properties.Add(new AdditionalSettingProperties { Category = "Direct3d9", Key = "maxVertexShaderVersion", Value = "0" });
                properties.Add(new AdditionalSettingProperties { Category = "Direct3d9", Key = "maxPixelShaderVersion", Value = "0" });
            }

            if (DisableVsyncCheckbox.IsChecked is not null && (bool)DisableVsyncCheckbox.IsChecked)
                properties.Add(new AdditionalSettingProperties { Category = "Direct3d9", Key = "allowTearing", Value = "1" });

            properties.Add(new AdditionalSettingProperties { Category = "ClientGraphics", Key = "screenWidth", Value = $"{screenWidth}" });
            properties.Add(new AdditionalSettingProperties { Category = "ClientGraphics", Key = "screenHeight", Value = $"{screenHeight}" });
            if (UseSafeRendererCheckbox.IsChecked is not null && (bool)UseSafeRendererCheckbox.IsChecked)
                properties.Add(new AdditionalSettingProperties { Category = "ClientGraphics", Key = "useSafeRenderer", Value = "1" });
            if (BorderlessWindowCheckbox.IsChecked is not null && (bool)BorderlessWindowCheckbox.IsChecked)
                properties.Add(new AdditionalSettingProperties { Category = "ClientGraphics", Key = "borderlessWindow", Value = "1" });
            if (WindowModeCheckbox.IsChecked is not null && (bool)WindowModeCheckbox.IsChecked)
                properties.Add(new AdditionalSettingProperties { Category = "ClientGraphics", Key = "windowed", Value = "1" });
            if (LowDetailTexturesCheckbox.IsChecked is not null && (bool)LowDetailTexturesCheckbox.IsChecked)
                properties.Add(new AdditionalSettingProperties { Category = "ClientGraphics", Key = "discardHighestMipMapLevels", Value = "1" });
            if (LowDetailNormalsCheckbox.IsChecked is not null && (bool)LowDetailNormalsCheckbox.IsChecked)
                properties.Add(new AdditionalSettingProperties { Category = "ClientGraphics", Key = "discardHighestNormalMipMapLevels", Value = "1" });
            if (DisableBumpMappingCheckbox.IsChecked is not null && (bool)DisableBumpMappingCheckbox.IsChecked)
                properties.Add(new AdditionalSettingProperties { Category = "ClientGraphics", Key = "disableOptionTag", Value = "DOT3" });
            if (DisableMultiPassRenderingCheckbox.IsChecked is not null && (bool)DisableMultiPassRenderingCheckbox.IsChecked)
                properties.Add(new AdditionalSettingProperties { Category = "ClientGraphics", Key = "disableOptionTag", Value = "HIQL" });
            if (DisableHardwareMouseCheckbox.IsChecked is not null && (bool)DisableHardwareMouseCheckbox.IsChecked)
                properties.Add(new AdditionalSettingProperties { Category = "ClientGraphics", Key = "useHardwareMouseCursor", Value = "0" });
            if (SkipIntroCheckbox.IsChecked is not null && (bool)SkipIntroCheckbox.IsChecked)
                properties.Add(new AdditionalSettingProperties { Category = "ClientGame", Key = "skipIntro", Value = "1" });
            if (DisableWorldPreloadingCheckbox.IsChecked is not null && (bool)DisableWorldPreloadingCheckbox.IsChecked)
                properties.Add(new AdditionalSettingProperties { Category = "ClientGame", Key = "preloadWorldSnapshot", Value = "0" });
            if (DisableFastMouseCheckbox.IsChecked is not null && (bool)DisableFastMouseCheckbox.IsChecked)
                properties.Add(new AdditionalSettingProperties { Category = "ClientUserInterface", Key = "alwaysSetMouseCursor", Value = "1" });
            if (DisableAudioCheckbox.IsChecked is not null && (bool)DisableAudioCheckbox.IsChecked)
                properties.Add(new AdditionalSettingProperties { Category = "ClientAudio", Key = "disableMiles", Value = "1" });
            if (DisableLODManagerCheckbox.IsChecked is not null && (bool)DisableLODManagerCheckbox.IsChecked)
                properties.Add(new AdditionalSettingProperties { Category = "ClientSkeletalAnimation", Key = "lodManagerEnable", Value = "0" });
            if (DisableTextureBakingCheckbox.IsChecked is not null && (bool)DisableTextureBakingCheckbox.IsChecked)
                properties.Add(new AdditionalSettingProperties { Category = "ClientTextureRenderer", Key = "disableTextureBaking", Value = "1" });
            if (DisableFileCachingCheckbox.IsChecked is not null && (bool)DisableFileCachingCheckbox.IsChecked)
                properties.Add(new AdditionalSettingProperties { Category = "SharedUtility", Key = "disableFileCaching", Value = "1" });
            if (DisableAsyncLoaderCheckbox.IsChecked is not null && (bool)DisableAsyncLoaderCheckbox.IsChecked)
                properties.Add(new AdditionalSettingProperties { Category = "SharedFile", Key = "enableAsynchronousLoader", Value = "0" });
            if (LowDetailCharactersCheckbox.IsChecked is not null && (bool)LowDetailCharactersCheckbox.IsChecked)
                properties.Add(new AdditionalSettingProperties { Category = "ClientSkeletalAnimation", Key = "skipL0", Value = "1" });
            if (LowDetailMeshesCheckbox.IsChecked is not null && (bool)LowDetailMeshesCheckbox.IsChecked)
                properties.Add(new AdditionalSettingProperties { Category = "ClientObject/DetailAppearanceTemplate", Key = "skipL0", Value = "1" });

            await FileHandler.SaveOptionsCfg(_launcherSettings, properties);

            SettingsEnableButtons();
        }

        async void SubmitDeveloperButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsDisableButtons();

            if (DevAdminCheckbox.IsChecked is not null && (bool)DevAdminCheckbox.IsChecked)
            {
                _launcherSettings!.Servers![_activeServer].Admin = true;
                await ConfigFile.SetConfig(_launcherSettings);
            }
            else
            {
                _launcherSettings!.Servers![_activeServer].Admin = false;
                await ConfigFile.SetConfig(_launcherSettings);
            }
            
            if (DevDebugCheckbox.IsChecked is not null && (bool)DevDebugCheckbox.IsChecked)
            {
                _launcherSettings!.Servers![_activeServer].DebugExamine = true;
                await ConfigFile.SetConfig(_launcherSettings);
            }
            else
            {
                _launcherSettings!.Servers![_activeServer].DebugExamine = false;
                await ConfigFile.SetConfig(_launcherSettings);
            }

            _launcherSettings.Servers[_activeServer].ApiUrl = DevAPIurl.Text;
            _launcherSettings.Servers[_activeServer].ManifestFileUrl = DevManifestURL.Text;
            _launcherSettings.Servers[_activeServer].BackupManifestFileUrl = DevBackupManifestURL.Text;
            _launcherSettings.Servers[_activeServer].ManifestFilePath = DevManifestFilePath.Text;
            _launcherSettings.Servers[_activeServer].SWGLoginHost = DevSWGhostname.Text;
            try 
            {
                _launcherSettings.Servers[_activeServer].SWGLoginPort = int.Parse(DevSWGport.Text);
            } 
            catch
            {

            }

            await ConfigFile.SetConfig(_launcherSettings);

            await FileHandler.SaveDeveloperOptionsCfg(_launcherSettings);

            SettingsEnableButtons();
        }

        async void SubmitModsButton_Click(object sender, RoutedEventArgs e)
        {
            if (_postLoad)
            {
                SettingsDisableButtons();

                string gameLocation = _launcherSettings!.Servers![_activeServer].GameLocation!;

                if (OptionsInstallDirectoryTextbox.Text.Trim() != gameLocation.Trim())
                {
                    _launcherSettings!.Servers![_activeServer].GameLocation = OptionsInstallDirectoryTextbox.Text.Trim();
                    await ConfigFile.SetConfig(_launcherSettings);

                    if (_previousInstallationDirectory is not null)
                    {
                        await CheckGameFiles(true, _previousInstallationDirectory);
                    }
                }

                if ((ModsReshadeCheckbox.IsChecked is not null && ModsHdTextureCheckbox.IsChecked is not null) && ((bool)ModsReshadeCheckbox.IsChecked || (bool)ModsHdTextureCheckbox.IsChecked))
                {
                    CharacterSelectGrid.Visibility = Visibility.Collapsed;

                    if ((bool)ModsReshadeCheckbox.IsChecked)
                    {
                        await DownloadHandler.CheckFilesAsync(_launcherSettings, false, "reshade");

                        _launcherSettings.Servers![_activeServer].Reshade = true;
                        await ConfigFile.SetConfig(_launcherSettings);
                    }

                    CharacterSelectGrid.Visibility = Visibility.Collapsed;

                    if ((bool)ModsHdTextureCheckbox.IsChecked)
                    {
                        await DownloadHandler.CheckFilesAsync(_launcherSettings, false, "hdtextures", true);

                        _launcherSettings.Servers![_activeServer].HDTextures = true;
                        await ConfigFile.SetConfig(_launcherSettings);
                    }
                    
                    CharacterSelectGrid.Visibility = Visibility.Visible;
                }

                if (ModsReshadeCheckbox.IsChecked is not null && !(bool)ModsReshadeCheckbox.IsChecked)
                {
                    _launcherSettings.Servers![_activeServer].Reshade = false;
                    await ConfigFile.SetConfig(_launcherSettings);
                    await DownloadHandler.DeleteNonTreMod("reshade");
                }

                if (ModsHdTextureCheckbox.IsChecked is not null && !(bool)ModsHdTextureCheckbox.IsChecked)
                {
                    _launcherSettings.Servers![_activeServer].HDTextures = false;
                    await ConfigFile.SetConfig(_launcherSettings);

                    foreach (TreModProperties property in _launcherSettings.Servers[_activeServer].TreMods!)
                    {
                        if (property.ModName == "hdtextures")
                        {
                            _launcherSettings.Servers[_activeServer].TreMods!.Remove(property);
                        }
                    }
                }

                ProgressGrid.Visibility = Visibility.Collapsed;

                if (_activeServer != OptionsLoginServerBox.SelectedIndex + 1)
                {
                    _launcherSettings.ActiveServer = OptionsLoginServerBox.SelectedIndex;
                    await ConfigFile.SetConfig(_launcherSettings);

                    _launcherSettings = await ConfigFile.GetConfig();

                    LogoutButton_Click(this, new RoutedEventArgs());
                }

                SettingsEnableButtons();
            }
        }

        #endregion

        #region TopBarButtons
        void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        void PatchNotesButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateScreen((int)Screens.UPDATES_GRID);
        }

        void WebsiteButton_Click(object sender, RoutedEventArgs e)
        {
            AppHandler.OpenDefaultBrowser("https://swglegacy.com");
        }

        void ForumsButton_Click(object sender, RoutedEventArgs e)
        {
            AppHandler.OpenDefaultBrowser("https://forums.swglegacy.com");
        }

        void WikiButton_Click(object sender, RoutedEventArgs e)
        {
            AppHandler.OpenDefaultBrowser("https://wiki.swglegacy.com");
        }

        void FacebookButton_Click(object sender, RoutedEventArgs e)
        {
            AppHandler.OpenDefaultBrowser("https://facebook.com/swglegacy");
        }

        void DiscordButton_Click(object sender, RoutedEventArgs e)
        {
            AppHandler.OpenDefaultBrowser("https://discord.gg/#");
        }
        #endregion

        #endregion

        #region Validation
        async Task CheckGameFiles(bool isDirChange = false, string previousDir = "")
        {
            AppHandler.WriteMissingConfigs(_launcherSettings!.Servers![_activeServer].GameLocation!);
            ScanDisableButtons();

            if (isDirChange)
            {
                await DownloadHandler.CheckFilesAsync(_launcherSettings, false, "", false, true, previousDir);
            }
            else
            {
                await DownloadHandler.CheckFilesAsync(_launcherSettings);
            }

            ScanEnableButtons();
        }

        async Task<bool> CheckAutoLoginAsync()
        {
            if (_launcherSettings!.Servers![_activeServer].AutoLogin)
            {
                if (!string.IsNullOrEmpty(_launcherSettings!.Servers![_activeServer].Username) && 
                    !string.IsNullOrEmpty(_launcherSettings.Servers![_activeServer].Password))
                {
                    _launcherSettings!.Servers![_activeServer].AutoLogin = true;
                    await ConfigFile.SetConfig(_launcherSettings);
                    LoginButton_Click(this, new RoutedEventArgs());
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
            string path = _launcherSettings!.Servers![_activeServer].GameLocation!;

            if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
            {
                return true;
            }   

            return false;
        }

        static string SelectSWGLocation()
        {
            using var dialog = new FolderBrowserDialog();
            DialogResult result = dialog.ShowDialog();

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

                CharacterSelectGrid.Visibility = Visibility.Collapsed;

                double status = (_currentFileStatus / _totalFileStatus) * 100;

                ProgressGrid.Visibility = Visibility.Visible;
                DownloadProgress.Value = status;
                DownloadProgress2.Value = progressPercentage;

                DownloadProgressText2.Text = $"{ _currentFile }";
                DownloadProgressTextRight2.Text = $"{progressPercentage / 10}%";
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
            //MessageBox.Show(error, "Cannot Connect To Server!", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        void BaseGameVerificationFailed(string message)
        {
            //MessageBox.Show("An error occurred accessing the folder you have specified! " +
                //"Please check the permissions of the folder and try again.", "Base Game Check Failed!",
                //MessageBoxButton.OK, MessageBoxImage.Error);
        }
        #endregion

        #region Misc

        async Task HandleLogin()
        {
            (string username, string _) = ConfigFile.GetAccountCredentials(_launcherSettings!);


            
            LogoutButton.Visibility = Visibility.Visible;
            UsernameTextBlock.Visibility = Visibility.Visible;
            UsernameTextBlock.Text = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(username!.ToLower());

            UpdateScreen((int)Screens.UPDATES_GRID);

            GetCharactersAsync();
            PopulateControls(true);
            LoggedInEnableControls();

            try
            {
                await CheckGameFiles();
            }
            catch { }
        }

        void LoggedInEnableControls()
        {
            PatchNotesButton.IsEnabled = true;
        }

        void NotLoggedInDisableControls()
        {
            PatchNotesButton.IsEnabled = false;
        }

        void GetCharactersAsync()
        {
            if (_launcherSettings!.Servers![_activeServer].Characters is not null)
            {
                foreach (string character in _launcherSettings.Servers[_activeServer].Characters!)
                {
                    CharacterNameComboBox.Items.Add(character);
                }
            }
        }

        #endregion

        void ResolutionBox_Initialized(object sender, EventArgs e)
        {
            foreach (string resolution in DisplayResolutions.GetResolutions())
            {
                ResolutionBox.Items.Add(resolution);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            this.WindowState = WindowState.Minimized;
            this.ShowInTaskbar = true;
            this.Hide();
        }

        void NotifyIcon_MouseDoubleClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            this.Show();
            this.WindowState = WindowState.Normal;
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (this.WindowState == WindowState.Minimized)
            {
                this.ShowInTaskbar = false;
                _notifyIcon.BalloonTipTitle = "Minimize Sucessful";
                _notifyIcon.BalloonTipText = "Minimized the app ";
                _notifyIcon.ShowBalloonTip(400);
                _notifyIcon.Visible = true;
            }
            else if (this.WindowState == WindowState.Normal)
            {
                _notifyIcon.Visible = false;
                this.ShowInTaskbar = true;
            }
        }
    }
}
