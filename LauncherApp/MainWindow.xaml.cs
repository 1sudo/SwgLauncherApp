using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using LauncherManagement;
using NAudio.Wave;
using Newtonsoft.Json.Linq;

namespace LauncherApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string _serverName = "Legacy";
        bool _gamePathValidated;
        string _currentFile;
        string _gamePath;
        string _serverPath;
        WaveOutEvent _outputDevice;
        AudioFileReader _audioFile;

        public MainWindow()
        {
            InitializeComponent();
            PreLaunchChecks();

            FileDownloader.OnDownloadProgressUpdated += DownloadProgressUpdated;
            DownloadHandler.OnCurrentFileDownloading += ShowFileBeingDownloaded;
            DownloadHandler.OnDownloadCompleted += OnDownloadCompleted;
            FileDownloader.OnServerError += CaughtServerError;
            DownloadHandler.OnFullScanFileCheck += OnFullScanFileCheck;
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
                    Setup setupForm = new Setup(_gamePath, configValidated, _serverName, this);
                    setupForm.Show();
                    this.Hide();
                }
            }

            if (configValidated && gameValidated && serverPathValidated)
            {
                await GameSetupHandler.CheckFiles(_serverPath);
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
                Setup setupForm = new Setup(_gamePath, configValidated, _serverName, this);
                setupForm.Show();
                this.Hide();
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
            PlayClickSound();
            this.WindowState = WindowState.Minimized;
        }

        void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            PlayClickSound();
            this.Close();
        }

        void DiscordButton_Click(object sender, RoutedEventArgs e)
        {
            PlayClickSound();
        }

        void ResourcesButton_Click(object sender, RoutedEventArgs e)
        {
            PlayClickSound();
        }

        void MantisButton_Click(object sender, RoutedEventArgs e)
        {
            PlayClickSound();
        }

        void SkillplannerButton_Click(object sender, RoutedEventArgs e)
        {
            PlayClickSound();
        }

        void VoteButton_Click(object sender, RoutedEventArgs e)
        {
            PlayClickSound();
        }

        void DonateButton_Click(object sender, RoutedEventArgs e)
        {
            PlayClickSound();
        }

        void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            PlayClickSound();

            var startInfo = new ProcessStartInfo();

            startInfo.EnvironmentVariables["SWGCLIENT_MEMORY_SIZE_MB"] = "4096";
            startInfo.UseShellExecute = false;
            startInfo.WorkingDirectory = _serverPath;
            startInfo.FileName = Path.Join(_serverPath, "SWGEmu.exe");

            Process.Start(startInfo);
        }

        void ModsButton_Click(object sender, RoutedEventArgs e)
        {
            PlayClickSound();
        }

        void ConfigButton_Click(object sender, RoutedEventArgs e)
        {
            PlayClickSound();

            var startInfo = new ProcessStartInfo();

            startInfo.UseShellExecute = true;
            startInfo.WorkingDirectory = _serverPath;
            startInfo.FileName = Path.Join(_serverPath, "SWGEmu_Setup.exe");

            Process.Start(startInfo);
        }

        async void FullScanButton_Click(object sender, RoutedEventArgs e)
        {
            PlayClickSound();

            ProgressGrid.Visibility = Visibility.Visible;
            statusBar.Visibility = Visibility.Collapsed;
            PlayButton.IsEnabled = false;
            FullScanButton.IsEnabled = false;
            ModsButton.IsEnabled = false;
            ConfigButton.IsEnabled = false;
            await GameSetupHandler.CheckFiles(_serverPath, true);
        }

        void LogManifestData(string data)
        {
            Debug.WriteLine(data);
        }

        void DownloadProgressUpdated(long bytesReceived, long totalBytesToReceive, int progressPercentage)
        {
            this.Dispatcher.Invoke(() =>
            {
                ProgressGrid.Visibility = Visibility.Visible;
                statusBar.Visibility = Visibility.Hidden;
                DownloadProgress.Value = progressPercentage;
                DownloadProgressText.Text = $"{ _currentFile } - " +
                    $"{ UnitConversion.ToSize(bytesReceived, UnitConversion.SizeUnits.MB) }MB / " +
                    $"{ UnitConversion.ToSize(totalBytesToReceive, UnitConversion.SizeUnits.MB) }MB";
            });
        }

        void OnFullScanFileCheck(string message)
        {
            this.Dispatcher.Invoke(() =>
            {
                DownloadProgressText.Text = message;
            });
        }

        void CaughtServerError(string error)
        {
            MessageBox.Show(error, "Cannot Connect To Server!", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        void OnDownloadCompleted()
        {
            ProgressGrid.Visibility = Visibility.Collapsed;
            statusBar.Visibility = Visibility.Visible;
            PlayButton.IsEnabled = true;
            FullScanButton.IsEnabled = true;
            ModsButton.IsEnabled = true;
            ConfigButton.IsEnabled = true;
        }

        void ShowFileBeingDownloaded(string file)
        {
            this.Dispatcher.Invoke(() =>
            {
                _currentFile = $"Downloading { file }";
            });
        }

        void PlayHoverSound(object sender, MouseEventArgs e)
        {
            if (_outputDevice == null)
            {
                _outputDevice = new WaveOutEvent();
                _outputDevice.PlaybackStopped += OnPlaybackStopped;
            }
            if (_audioFile == null)
            {
                _audioFile = new AudioFileReader(Path.Join(Directory.GetCurrentDirectory(), "audio/select.wav"));
                _outputDevice.Init(_audioFile);
            }
            _outputDevice.Volume = 0.35f;
            _outputDevice.Play();
        }

        void PlayClickSound()
        {
            if (_outputDevice == null)
            {
                _outputDevice = new WaveOutEvent();
                _outputDevice.PlaybackStopped += OnPlaybackStopped;
            }
            if (_audioFile == null)
            {
                _audioFile = new AudioFileReader(Path.Join(Directory.GetCurrentDirectory(), "audio/click.wav"));
                _outputDevice.Init(_audioFile);
            }
            _outputDevice.Volume = 0.35f;
            _outputDevice.Play();
        }

        void OnPlaybackStopped(object sender, StoppedEventArgs args)
        {
            _outputDevice.Dispose();
            _outputDevice = null;
            _audioFile.Dispose();
            _audioFile = null;
        }
    }
}
