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
        UpdatesButtonIsEnabled = false;
        MainWindowLogoutButton = Visibility.Collapsed;
        MainWindowUsernameTextBlockVisibility = Visibility.Collapsed;
        MinimizeButton = new RelayCommand(() => Application.Current.MainWindow!.Close());
        CloseButton = new RelayCommand(() => Application.Current.MainWindow!.Close());
        UpdatesButton = new RelayCommand(() => ScreenContainerViewModel.EnableScreen(Screen.Updates));
        LogoutButton = new RelayCommand(OnLogout);
        AccountScreenViewModel.SetUsername += OnLogin;
    }

    private void OnLogin(string username)
    {
        UpdatesButtonIsEnabled = true;
        MainWindowUsernameTextBlock = username.ToUpper();
        MainWindowUsernameTextBlockVisibility = Visibility.Visible;
        MainWindowLogoutButton = Visibility.Visible;
    }

    private void OnLogout()
    {
        UpdatesButtonIsEnabled = false;
        MainWindowUsernameTextBlockVisibility = Visibility.Collapsed;
        MainWindowLogoutButton = Visibility.Collapsed;
        ScreenContainerViewModel.EnableScreen(Screen.AccountLogin);
    }
}
