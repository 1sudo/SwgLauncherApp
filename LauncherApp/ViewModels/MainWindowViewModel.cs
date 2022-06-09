using System.Windows;
using Microsoft.Toolkit.Mvvm.Input;

namespace LauncherApp.ViewModels
{
    internal class MainWindowViewModel
    {
        public IRelayCommand MinimizeButton { get; set; }
        public IRelayCommand CloseButton { get; set; }
        public IRelayCommand UpdatesButton { get; set; }

        public MainWindowViewModel()
        {
            MinimizeButton = new RelayCommand(() => Application.Current.MainWindow!.Close());
            CloseButton = new RelayCommand(() => Application.Current.MainWindow!.Close());
            UpdatesButton = new RelayCommand(() => ScreenContainerViewModel.EnableScreen(Screen.Updates));
        }
    }
}
