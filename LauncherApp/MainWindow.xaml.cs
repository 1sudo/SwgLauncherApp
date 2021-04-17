using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using LauncherManagement;

namespace LauncherApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        bool GamePathValidated;
        string reason;
        string currentFile;

        public MainWindow()
        {
            InitializeComponent();
            PreLaunchChecks();

            FileDownloader.onDownloadProgressUpdated += DownloadProgressUpdated;
            DownloadHandler.onCurrentFileDownloading += ShowFileBeingDownloaded;
            DownloadHandler.onDownloadCompleted += OnDownloadCompleted;
            FileDownloader.onServerError += CaughtServerError;
        }

        private void PreLaunchChecks()
        {
            (GamePathValidated, reason) = GameSetupHandler.ValidateGamePath("C:/SWGEmu");

            if (!GamePathValidated)
            {
                MessageBox.Show(reason);
            }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        private async void button1_Click(object sender, RoutedEventArgs e)
        {
            await GameSetupHandler.CheckFiles();
        }

        private void modsButton_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void configButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void fullScanButton_Click(object sender, RoutedEventArgs e)
        {

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
                DownloadProgressText.Text = $"{ currentFile } - " + 
                    $"{ UnitConversion.ToSize(bytesReceived, UnitConversion.SizeUnits.MB) }MB / " +
                    $"{ UnitConversion.ToSize(totalBytesToReceive, UnitConversion.SizeUnits.MB) }MB";
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
        }

        private void ShowFileBeingDownloaded(string file)
        {
            this.Dispatcher.Invoke(() =>
            {
                currentFile = $"Downloading { file }";
            });
        }
    }
}
