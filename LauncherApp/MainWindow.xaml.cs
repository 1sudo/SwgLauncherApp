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
        bool GamePathValidated;
        string CurrentFile;
        string darknaughtPath;
        private WaveOutEvent outputDevice;
        private AudioFileReader audioFile;

        public MainWindow()
        {
            InitializeComponent();
            PreLaunchChecks();

            FileDownloader.onDownloadProgressUpdated += DownloadProgressUpdated;
            DownloadHandler.onCurrentFileDownloading += ShowFileBeingDownloaded;
            DownloadHandler.onDownloadCompleted += OnDownloadCompleted;
            FileDownloader.onServerError += CaughtServerError;
            DownloadHandler.onFullScanFileCheck += OnFullScanFileCheck;
        }

        private async void PreLaunchChecks()
        {
            int locationSelected = 0;
            bool configValidated = false;
            bool gameValidated = false;
            bool darknaughtPathValidated = false;

            // Ensure config exists
            if (!File.Exists(Path.Join(Directory.GetCurrentDirectory(), "config.json")))
            {
                locationSelected++;
                gameValidated = ValidateGamePath(SelectSWGLocation());
            }
            // If it exists, validate it
            else
            {
                configValidated = GameSetupHandler.ValidateConfig();
            }

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
                    if (locationSelected > 0)
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

            if (configValidated && gameValidated)
            {
                darknaughtPathValidated = ValidateDarknaughtPath();
            }
                
            if (configValidated && gameValidated && darknaughtPathValidated)
            {
                await GameSetupHandler.CheckFiles(darknaughtPath);
            }
        }

        private bool ValidateDarknaughtPath()
        {
            string path = GameSetupHandler.GetDarknaughtPath();
            
            if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
            {
                darknaughtPath = path;
                return true;
            }
            return false;
        }

        private bool ValidateGamePath(string location, bool locationProvided = true, bool configValidated = false)
        {
            Trace.WriteLine(location);
            GamePathValidated = GameSetupHandler.ValidateGamePath(location);

            if (GamePathValidated && configValidated)
            {
                return true;
            }

            if (GamePathValidated)
            {
                Setup setupForm = new Setup();
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

        private string SelectSWGLocation()
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
                return dialog.SelectedPath;
            }

            return "";
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        private void minimizeButton_Click(object sender, RoutedEventArgs e)
        {
            PlayClickSound();
            this.WindowState = WindowState.Minimized;
        }

        private void closeButton_Click(object sender, RoutedEventArgs e)
        {
            PlayClickSound();
            this.Close();
        }

        private void discordButton_Click(object sender, RoutedEventArgs e)
        {
            PlayClickSound();
        }

        private void resourcesButton_Click(object sender, RoutedEventArgs e)
        {
            PlayClickSound();
        }

        private void mantisButton_Click(object sender, RoutedEventArgs e)
        {
            PlayClickSound();
        }

        private void skillplannerButton_Click(object sender, RoutedEventArgs e)
        {
            PlayClickSound();
        }

        private void voteButton_Click(object sender, RoutedEventArgs e)
        {
            PlayClickSound();
        }

        private void donateButton_Click(object sender, RoutedEventArgs e)
        {
            PlayClickSound();
        }

        private void playButton_Click(object sender, RoutedEventArgs e)
        {
            PlayClickSound();
        }

        private void modsButton_Click(object sender, RoutedEventArgs e)
        {
            PlayClickSound();
        }

        private void configButton_Click(object sender, RoutedEventArgs e)
        {
            PlayClickSound();
        }

        private async void fullScanButton_Click(object sender, RoutedEventArgs e)
        {
            PlayClickSound();
            await GameSetupHandler.CheckFiles(darknaughtPath, true);
        }

        private void LogManifestData(string data)
        {
            Debug.WriteLine(data);
        }

        private void DownloadProgressUpdated(long bytesReceived, long totalBytesToReceive, int progressPercentage)
        {
            this.Dispatcher.Invoke(() =>
            {
                ProgressGrid.Visibility = Visibility.Visible;
                statusBar.Visibility = Visibility.Hidden;
                DownloadProgress.Value = progressPercentage;
                DownloadProgressText.Text = $"{ CurrentFile } - " + 
                    $"{ UnitConversion.ToSize(bytesReceived, UnitConversion.SizeUnits.MB) }MB / " +
                    $"{ UnitConversion.ToSize(totalBytesToReceive, UnitConversion.SizeUnits.MB) }MB";
            });
        }

        private void OnFullScanFileCheck(string message)
        {
            this.Dispatcher.Invoke(() =>
            {
                DownloadProgressText.Text = message;
            });
        }

        private void CaughtServerError(string error)
        {
            MessageBox.Show(error, "Cannot Connect To Server!", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void OnDownloadCompleted()
        {
            ProgressGrid.Visibility = Visibility.Collapsed;
            statusBar.Visibility = Visibility.Visible;
            playButton.IsEnabled = true;
            fullScanButton.IsEnabled = true;
            modsButton.IsEnabled = true;
            configButton.IsEnabled = true;
        }

        private void ShowFileBeingDownloaded(string file)
        {
            this.Dispatcher.Invoke(() =>
            {
                CurrentFile = $"Downloading { file }";
            });
        }

        private void PlayHoverSound(object sender, MouseEventArgs e)
        {
            if (outputDevice == null)
            {
                outputDevice = new WaveOutEvent();
                outputDevice.PlaybackStopped += OnPlaybackStopped;
            }
            if (audioFile == null)
            {
                audioFile = new AudioFileReader(Path.Join(Directory.GetCurrentDirectory(), "audio/select.wav"));
                outputDevice.Init(audioFile);
            }
            outputDevice.Volume = 0.35f;
            outputDevice.Play();
        }

        private void PlayClickSound()
        {
            if (outputDevice == null)
            {
                outputDevice = new WaveOutEvent();
                outputDevice.PlaybackStopped += OnPlaybackStopped;
            }
            if (audioFile == null)
            {
                audioFile = new AudioFileReader(Path.Join(Directory.GetCurrentDirectory(), "audio/click.wav"));
                outputDevice.Init(audioFile);
            }
            outputDevice.Volume = 0.35f;
            outputDevice.Play();
        }

        private void OnPlaybackStopped(object sender, StoppedEventArgs args)
        {
            outputDevice.Dispose();
            outputDevice = null;
            audioFile.Dispose();
            audioFile = null;
        }
    }
}
