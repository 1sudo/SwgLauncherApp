using System.Windows;
using CommunityToolkit.Mvvm.Input;

namespace LauncherApp.ViewModels;

internal class MainWindowViewModel : MainWindowViewModelProperties
{
    public IRelayCommand MinimizeButton { get; set; }
    public IRelayCommand CloseButton { get; set; }
    public IRelayCommand UpdatesButton { get; set; }
    public IRelayCommand LogoutButton { get; set; }

    public MainWindowViewModel()
    {
        MainWindowLogoutButton = Visibility.Collapsed;
        MainWindowUsernameTextBlockVisibility = Visibility.Collapsed;
        MinimizeButton = new RelayCommand(() => Application.Current.MainWindow!.Close());
        CloseButton = new RelayCommand(() => Application.Current.MainWindow!.Close());
        UpdatesButton = new RelayCommand(() => ScreenContainerViewModel.EnableScreen(Screen.Updates));
        LogoutButton = new RelayCommand(OnLogout);
        AccountScreenViewModel.SetUsername += OnLoginSetUsername;
    }

    private void OnLoginSetUsername(string username)
    {
        MainWindowUsernameTextBlock = username.ToUpper();
        MainWindowUsernameTextBlockVisibility = Visibility.Visible;
        MainWindowLogoutButton = Visibility.Visible;
    }

    private void OnLogout()
    {
        MainWindowUsernameTextBlockVisibility = Visibility.Collapsed;
        MainWindowLogoutButton = Visibility.Collapsed;
        ScreenContainerViewModel.EnableScreen(Screen.AccountLogin);
    }
}
